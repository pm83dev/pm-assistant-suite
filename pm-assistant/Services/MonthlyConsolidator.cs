using PmAssistant.Services;

namespace PmAssistant.Services;

public interface IMonthlyConsolidator
{
    Task<string> ConsolidateAsync(int year, int month);
}

public class MonthlyConsolidator : IMonthlyConsolidator
{
    private readonly IGoogleSheetsService _sheets;
    private readonly IPdfParserService _pdfParser;
    private readonly ILogger<MonthlyConsolidator> _logger;

    public MonthlyConsolidator(IGoogleSheetsService sheets, IPdfParserService pdfParser,
        ILogger<MonthlyConsolidator> logger)
    {
        _sheets = sheets;
        _pdfParser = pdfParser;
        _logger = logger;
    }

    public async Task<string> ConsolidateAsync(int year, int month)
    {
        var entries = await GetEntriesFromSheets(year, month);
        var totalHours = entries.Sum(e => e.Hours);

        // TODO: integrate PDF client report parsing
        // var pdfText = await _pdfParser.ExtractTextAsync(pdfPath);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Riconciliazione Mensile {year}-{month:D2} ===\n");
        sb.AppendLine($"Totale ore registrate: {totalHours}h\n");

        // Raggruppa per cliente
        var byClient = entries.GroupBy(e => e.Client);
        foreach (var group in byClient)
        {
            var clientHours = group.Sum(e => e.Hours);
            sb.AppendLine($"  {group.Key}: {clientHours}h ({group.Count()} giorni)\n");
        }

        sb.AppendLine("=== Fine Riepilogo ===");
        return sb.ToString();
    }

    private async Task<List<(string Client, decimal Hours)>> GetEntriesFromSheets(int year, int month)
    {
        var rows = await _sheets.ReadRowsAsync("DailyLogs");
        var entries = new List<(string Client, decimal Hours)>();

        foreach (var row in rows.Skip(1))
        {
            if (row.Count >= 2 && DateTime.TryParse(row[1] as string, out var entryDate) &&
                entryDate.Year == year && entryDate.Month == month)
            {
                entries.Add((
                    row[2]?.ToString() ?? "",
                    decimal.TryParse(row[4]?.ToString(), out var h) ? h : 0m
                ));
            }
        }

        return entries;
    }
}
