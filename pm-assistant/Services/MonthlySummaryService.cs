using PmAssistant.Services;

namespace PmAssistant.Services;

public interface IMonthlySummaryService
{
    Task<string> GenerateAsync(int year, int month);
}

public class MonthlySummaryService : IMonthlySummaryService
{
    private readonly IGoogleSheetsService _sheets;
    private readonly ILlmService _llm;
    private readonly ILogger<MonthlySummaryService> _logger;

    public MonthlySummaryService(IGoogleSheetsService sheets, ILlmService llm,
        ILogger<MonthlySummaryService> logger)
    {
        _sheets = sheets;
        _llm = llm;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(int year, int month)
    {
        var entries = await GetEntriesFromSheets(year, month);
        if (!entries.Any())
            return $"Nessuna registrazione per {year}-{month:D2}.";

        var totalHours = entries.Sum(e => e.Hours);
        var byClient = entries.GroupBy(e => e.Client)
            .Select(g => new { Client = g.Key, Hours = g.Sum(e => e.Hours), Days = g.Count() });

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Riepilogo Mensile - {year}-{month:D2}");
        sb.AppendLine(new string('=', 40));
        sb.AppendLine($"Totale ore: {totalHours}h\n");

        foreach (var c in byClient.OrderBy(x => x.Client))
        {
            sb.AppendLine($"  {c.Client}: {c.Hours}h su {c.Days} giorni");
        }

        // Genera testo per Fiscozen tramite LLM
        var prompt = $"""
            Genera un riepilogo testuale professionale per il cliente freelance, 
            da usare per la fatturazione su Fiscozen. Non generare PDF o XML.

            Dati:
            Mese: {year}-{month:D2}
            Totale ore: {totalHours}h
            
            Per cliente:
            {string.Join("\n", byClient.Select(c => $"  - {c.Client}: {c.Hours}h ({c.Days} giorni)"))}

            Rispondi solo con il testo del riepilogo, in italiano.
            """;

        var summaryText = await _llm.GenerateAsync(prompt, "Sei un assistente che genera riepiloghi mensili per freelance.");
        return string.IsNullOrWhiteSpace(summaryText) ? sb.ToString() : summaryText;
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
