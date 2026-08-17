using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace PmAssistant.Services;

public interface IGoogleSheetsService
{
    Task<List<List<object>>> ReadRowsAsync(string sheetName);
    Task AppendRowsAsync(string sheetName, params object[] values);
    Task UpdateRangeAsync(string sheetName, string range, object value);
    Task<string> GetOrCreateSheetAsync(string sheetName);
    Task<List<string>> ListSheetsAsync();
}

public class GoogleSheetsSettings
{
    public string SheetId { get; set; } = "";
    public string DailyLogsSheetName { get; set; } = "DailyLogs";
    public string TodosSheetName { get; set; } = "Todos";
    public string AuditLogSheetName { get; set; } = "AuditLog";
    public string ClientsSheetName { get; set; } = "Clients";
    public string EmailQueueSheetName { get; set; } = "EmailQueue";
    public string ServiceAccountJsonPath { get; set; } = "./keys/service-account.json";
}

public class GoogleSheetsMetadata
{
    public string SheetId { get; set; } = "";
    public string Title { get; set; } = "";
    public int Index { get; set; }
}

public class GoogleSheetsService : IGoogleSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly GoogleSheetsSettings _settings;
    private readonly string _resolvedKeyPath;

    public GoogleSheetsService(IOptions<GoogleSheetsSettings> settings)
    {
        _settings = settings.Value;

        // Resolve the service account key path: try configured path first, then look in parent directory
        var configuredPath = _settings.ServiceAccountJsonPath;
        _resolvedKeyPath = ConfigurePath(configuredPath) ?? Path.GetFullPath(configuredPath);

        var credential = GoogleCredential.FromFile(_resolvedKeyPath)
            .CreateScoped(new[] { "https://www.googleapis.com/auth/spreadsheets" });

        Console.WriteLine($"[GoogleSheets] Using service account key: {_resolvedKeyPath}");

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Secretary AI Assistant",
            GZipEnabled = true
        });
    }

    public async Task<List<List<object>>> ReadRowsAsync(string sheetName)
    {
        var range = $"{sheetName}!A:Z";
        var response = await _sheetsService.Spreadsheets.Values.Get(_settings.SheetId, range).ExecuteAsync();
        var values = response.Values ?? new List<IList<object>>();
        return values.Select(row => row.Cast<object>().ToList()).ToList();
    }

    public async Task AppendRowsAsync(string sheetName, params object[] values)
    {
        // Ancorato ad A1: con "A:Z" il table detection di Google può agganciare
        // l'append all'ultima colonna dei dati esistenti invece che alla colonna A
        var range = $"{sheetName}!A1";
        var valueRange = new ValueRange
        {
            Values = new List<IList<object>> { values.ToList() }
        };
        var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _settings.SheetId, range);
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        appendRequest.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
        await appendRequest.ExecuteAsync();
    }

    public async Task UpdateRangeAsync(string sheetName, string range, object value)
    {
        var valueRange = new ValueRange { Values = new List<IList<object>> { ((List<object>)value).ToList() } };
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _settings.SheetId, $"{sheetName}!{range}");
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await updateRequest.ExecuteAsync();
    }

    public async Task<string> GetOrCreateSheetAsync(string sheetName)
    {
        var spreadsheet = await _sheetsService.Spreadsheets.Get(_settings.SheetId).ExecuteAsync();
        var existingSheets = spreadsheet.Sheets ?? new List<Sheet>();
        if (existingSheets.Any(s => s.Properties?.Title == sheetName))
            return sheetName;

        var addSheet = new AddSheetRequest { Properties = new SheetProperties { Title = sheetName } };
        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest { Requests = [new Request { AddSheet = addSheet }] };
        await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, _settings.SheetId).ExecuteAsync();
        return sheetName;
    }

    public async Task<List<string>> ListSheetsAsync()
    {
        var spreadsheet = await _sheetsService.Spreadsheets.Get(_settings.SheetId).ExecuteAsync();
        var sheets = spreadsheet.Sheets ?? new List<Sheet>();
        return sheets.Select(s => s.Properties?.Title ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList();
    }

    /// <summary>
    /// Resolves the service account key path by checking multiple locations.
    /// Tries: configured path → grandparent of CWD → parent of assembly → current directory.
    /// </summary>
    private static string? ConfigurePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;

        // Try the configured path as-is (absolute or relative to CWD)
        if (File.Exists(relativePath))
            return Path.GetFullPath(relativePath);

        // Try relative to current working directory at different levels
        var cwd = Environment.CurrentDirectory;
        
        // Grandparent of CWD (handles case where app runs from a subdirectory like pm-assistant/pm-assistant)
        var cwdParent = Directory.GetParent(cwd)?.FullName;
        if (cwdParent != null)
        {
            var fromGrandparent = Path.Combine(cwdParent, relativePath);
            var resolvedGrandparent = Path.GetFullPath(fromGrandparent);
            if (File.Exists(resolvedGrandparent))
                return resolvedGrandparent;

            // Great-grandparent of CWD (handles deeper nesting)
            var cwdGreatGrandparent = Directory.GetParent(cwdParent)?.FullName;
            if (cwdGreatGrandparent != null)
            {
                var fromGreatGrandparent = Path.Combine(cwdGreatGrandparent, relativePath);
                var resolvedGreatGrandparent = Path.GetFullPath(fromGreatGrandparent);
                if (File.Exists(resolvedGreatGrandparent))
                    return resolvedGreatGrandparent;
            }
        }

        // Try relative to the assembly directory (handles case where app runs from subdirectory)
        var assemblyDir = AppContext.BaseDirectory;
        var fromAssemblyDir = Path.Combine(assemblyDir, "..", "..", "..", relativePath);
        var resolvedFromAssembly = Path.GetFullPath(fromAssemblyDir);
        if (File.Exists(resolvedFromAssembly))
            return resolvedFromAssembly;

        // Try directly relative to assembly directory (in case keys/ is next to bin/)
        var fromAssemblyDirFlat = Path.Combine(assemblyDir, relativePath);
        if (File.Exists(fromAssemblyDirFlat))
            return Path.GetFullPath(fromAssemblyDirFlat);

        return null;
    }
}
