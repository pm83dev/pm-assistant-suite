using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;

namespace PmAssistant.Services;

public interface IGraphAuthService
{
    Task<GraphServiceClient?> GetClientAsync(string accountName);
    Task<bool> IsAccountEnabled(string accountName);
}

public class GraphAccountConfig
{
    public string Name { get; set; } = "";
    public string AuthType { get; set; } = "";
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public bool Enabled { get; set; }
}

public class GraphAuthService : IGraphAuthService
{
    private readonly List<GraphAccountConfig> _accounts;
    private readonly Dictionary<string, GraphServiceClient?> _clientCache = new();

    public GraphAuthService(IOptions<List<GraphAccountConfig>> accounts)
    {
        _accounts = accounts.Value ?? [];
    }

    public async Task<bool> IsAccountEnabled(string accountName)
    {
        var account = _accounts.FirstOrDefault(a => a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase));
        return account?.Enabled == true;
    }

    public async Task<GraphServiceClient?> GetClientAsync(string accountName)
    {
        if (_clientCache.TryGetValue(accountName, out var cached))
            return cached;

        var account = _accounts.FirstOrDefault(a => a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase));
        if (account is null || !account.Enabled)
            return null;

        try
        {
            if (account.AuthType == "AppOnly" && !string.IsNullOrEmpty(account.ClientId))
            {
                var client = await GetAppOnlyClientAsync(account);
                _clientCache[accountName] = client;
                return client;
            }
            else if (account.AuthType == "Delegated")
            {
                // Delegated auth disabled per specs until explicit authorization
                _clientCache[accountName] = null;
                return null;
            }
        }
        catch
        {
            _clientCache[accountName] = null;
            return null;
        }

        return null;
    }

    private async Task<GraphServiceClient?> GetAppOnlyClientAsync(GraphAccountConfig account)
    {
        var clientId = account.ClientId ?? "";
        var tenantId = account.TenantId ?? "";
        var clientSecret = account.ClientSecret ?? "";

        var confidentialClient = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .Build();

        // Verifica subito che le credenziali siano valide
        await confidentialClient.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
            .ExecuteAsync();

        var authProvider = new BaseBearerTokenAuthenticationProvider(new MsalAccessTokenProvider(confidentialClient));
        return new GraphServiceClient(authProvider);
    }

    private sealed class MsalAccessTokenProvider : IAccessTokenProvider
    {
        private readonly IConfidentialClientApplication _app;

        public MsalAccessTokenProvider(IConfidentialClientApplication app) => _app = app;

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();

        public async Task<string> GetAuthorizationTokenAsync(Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _app.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                .ExecuteAsync(cancellationToken);
            return result.AccessToken;
        }
    }
}
