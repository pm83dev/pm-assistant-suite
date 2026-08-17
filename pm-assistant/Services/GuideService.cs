using Microsoft.Extensions.Options;
using LibGit2Sharp;
using PmAssistant.Services;

namespace PmAssistant.Services;

public interface IGuideService
{
    Task<string> GetGuideAsync(string guideName);
    Task<(bool HasChanges, string Diff)> PreviewUpdateAsync(string guideName, string newContent);
    Task ApplyUpdateAsync(string guideName, string newContent);
    Task PublishAsync(string guideName);
    Task<List<string>> ListGuidesAsync();
}

public class GuideSettings
{
    public string BasePath { get; set; } = "./guides";
    public string GitHubRepo { get; set; } = "";
    public string StagingBranch { get; set; } = "staging";
    public string ProductionBranch { get; set; } = "main";
    public string GitUserEmail { get; set; } = "";
    public string GitUserName { get; set; } = "";
}

public class GuideService : IGuideService
{
    private readonly GuideSettings _settings;
    private readonly IAuditLogService _audit;
    private readonly ILogger<GuideService> _logger;

    public GuideService(IOptions<GuideSettings> settings, IAuditLogService audit,
        ILogger<GuideService> logger)
    {
        _settings = settings.Value;
        _audit = audit;
        _logger = logger;
    }

    public async Task<List<string>> ListGuidesAsync()
    {
        var basePath = Path.GetFullPath(_settings.BasePath);
        if (!Directory.Exists(basePath))
            return [];

        return Directory.GetFiles(basePath, "*.md")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToList();
    }

    public async Task<string> GetGuideAsync(string guideName)
    {
        var filePath = Path.Combine(_settings.BasePath, $"{guideName}.md");
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Guida non trovata: {guideName}");

        return await File.ReadAllTextAsync(filePath);
    }

    public async Task<(bool HasChanges, string Diff)> PreviewUpdateAsync(string guideName, string newContent)
    {
        var filePath = Path.Combine(_settings.BasePath, $"{guideName}.md");
        var existingContent = File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : "# " + guideName;

        if (existingContent == newContent)
            return (false, "Nessuna modifica rilevata.");

        var diff = ComputeDiff(existingContent, newContent);
        return (true, diff);
    }

    public async Task ApplyUpdateAsync(string guideName, string newContent)
    {
        var filePath = Path.Combine(_settings.BasePath, $"{guideName}.md");
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir!);

        await File.WriteAllTextAsync(filePath, newContent);

        // Git add + commit su branch staging
        var repoPath = _settings.BasePath;
        if (Directory.Exists(Path.Combine(repoPath, ".git")))
        {
            using var repo = new Repository(repoPath);

            Commands.Stage(repo, Path.GetFullPath(filePath));

            var signature = new Signature(_settings.GitUserName, _settings.GitUserEmail, DateTimeOffset.Now);
            var commitId = repo.Commit($"Update guide: {guideName}", signature, signature);

            // Push su staging
            try
            {
                var remotes = repo.Network.Remotes;
                var origin = remotes["origin"];
                if (origin != null)
                {
                    // Switch to staging branch
                    var staging = repo.Branches[_settings.StagingBranch]
                        ?? repo.CreateBranch(_settings.StagingBranch);
                    Commands.Checkout(repo, staging);

                    repo.Network.Push(origin, $"refs/heads/{_settings.StagingBranch}", new PushOptions
                    {
                        CredentialsProvider = (url, usernameFromUrl, types) =>
                            new UsernamePasswordCredentials
                            {
                                Username = Environment.GetEnvironmentVariable("GIT_USERNAME") ?? "",
                                Password = Environment.GetEnvironmentVariable("GIT_TOKEN") ?? ""
                            }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore push Git per guida {GuideName}", guideName);
            }
        }

        await _audit.LogAsync("guide_update", $"Guida '{guideName}' aggiornata in staging");
    }

    public async Task PublishAsync(string guideName)
    {
        var repoPath = _settings.BasePath;
        if (!Directory.Exists(Path.Combine(repoPath, ".git")))
            throw new InvalidOperationException("Repository Git non trovata per le guide.");

        using var repo = new Repository(repoPath);

        try
        {
            // Merge staging -> main
            var productionBranch = $"refs/heads/{_settings.ProductionBranch}";

            var staging = repo.Branches[_settings.StagingBranch]
                ?? throw new InvalidOperationException($"Branch '{_settings.StagingBranch}' non esiste.");

            var production = repo.Branches[_settings.ProductionBranch]
                ?? repo.CreateBranch(_settings.ProductionBranch);
            Commands.Checkout(repo, production);

            var signature = new Signature(_settings.GitUserName, _settings.GitUserEmail, DateTimeOffset.Now);
            repo.Merge(staging, signature, new MergeOptions { CommitOnSuccess = true });

            // Push main
            var remotes = repo.Network.Remotes;
            var origin = remotes["origin"];
            if (origin != null)
            {
                repo.Network.Push(origin, productionBranch, new PushOptions
                {
                    CredentialsProvider = (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials
                        {
                            Username = Environment.GetEnvironmentVariable("GIT_USERNAME") ?? "",
                            Password = Environment.GetEnvironmentVariable("GIT_TOKEN") ?? ""
                        }
                });
            }

            await _audit.LogAsync("guide_publish", $"Guida '{guideName}' pubblicata su main");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore publish guida {GuideName}", guideName);
            throw;
        }
    }

    private static string ComputeDiff(string oldContent, string newContent)
    {
        var oldLines = oldContent.Split('\n', StringSplitOptions.None);
        var newLines = newContent.Split('\n', StringSplitOptions.None);
        var sb = new System.Text.StringBuilder();

        var maxLen = Math.Max(oldLines.Length, newLines.Length);
        for (var i = 0; i < maxLen; i++)
        {
            var oldLine = i < oldLines.Length ? oldLines[i] : null;
            var newLine = i < newLines.Length ? newLines[i] : null;

            if (oldLine == newLine)
            {
                sb.AppendLine($" {oldLine}");
            }
            else
            {
                if (oldLine != null)
                    sb.AppendLine($"-{oldLine}");
                if (newLine != null)
                    sb.AppendLine($"+{newLine}");
            }
        }

        return sb.ToString();
    }
}
