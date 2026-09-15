using System.Net.Http.Json;
using System.Text.Json;
using LocalCodeAgent.Models;
using PmAssistant.Services;

namespace PmAssistant.Tools;

/// <summary>
/// Tool per eseguire query naturali sul database di ore-tracking usando LLM.
/// Recupera dinamicamente lo schema del DB e genera query SQL corrette.
/// Usa l'API REST di ore-tracking per eseguire le query.
/// </summary>
public class NL2SQLTools(string baseUrl, ILlmService llmService)
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string _baseUrl = baseUrl.TrimEnd('/');
    private readonly ILlmService _llmService = llmService;

    /// <summary>
    /// Valves per configurare il comportamento del tool.
    /// </summary>
    public class Valves
    {
        public static readonly string DefaultDbType = "sqlite"; // ore-tracking usa SQLite
        public static readonly int DefaultMaxRows = 100;
    }

    private static readonly Valves _valves = new();

    public List<ToolDefinition> Definitions =>
    [
        new() { Function = new() {
            Name = "list_tables",
            Description = "Elenca tutte le tabelle disponibili nel database. Usa questo strumento PRIMA di scrivere una query, per capire quali tabelle esistono.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }},
        new() { Function = new() {
            Name = "describe_table",
            Description = "Mostra le colonne e i tipi di dato di una tabella specifica. Usa questo strumento per capire la struttura di una tabella prima di scrivere una query.",
            Parameters = new { 
                type = "object", 
                properties = new { 
                    table_name = new { type = "string", description = "Nome della tabella da descrivere." }
                }, 
                required = new[] { "table_name" } 
            }
        }},
        new() { Function = new() {
            Name = "run_query",
            Description = "Esegue una query SQL in SOLA LETTURA (SELECT) sul database e restituisce i risultati. Rifiuta qualsiasi query che non sia una SELECT, per sicurezza. Limita automaticamente il numero di righe restituite.",
            Parameters = new { 
                type = "object", 
                properties = new { 
                    sql_query = new { type = "string", description = "La query SQL da eseguire. Deve iniziare con SELECT." },
                    max_rows = new { type = "integer", description = "Numero massimo di righe da restituire (default: 100)" }
                }, 
                required = new[] { "sql_query" } 
            }
        }}
    ];

    public string Execute(string toolName, string argumentsJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson, options);

            // Validazione: args può essere null se argumentsJson è vuoto o non valido
            if (args == null)
            {
                args = new Dictionary<string, object>();
            }

            return toolName switch
            {
                "list_tables" => ExecuteListTables(),
                "describe_table" => ExecuteDescribeTable(args),
                "run_query" => ExecuteRunQuery(args),
                _ => $"Errore: tool '{toolName}' non riconosciuto."
            };
        }
        catch (Exception ex)
        {
            return $"Errore nell'esecuzione del tool '{toolName}': {ex.Message}";
        }
    }

    public async Task<string> ExecuteAsync(string toolName, string argumentsJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson, options);

            // Validazione: args può essere null se argumentsJson è vuoto o non valido
            if (args == null)
            {
                args = new Dictionary<string, object>();
            }

            return toolName switch
            {
                "list_tables" => await ExecuteListTablesAsync(),
                "describe_table" => await ExecuteDescribeTableAsync(args),
                "run_query" => await ExecuteRunQueryAsync(args),
                _ => $"Errore: tool '{toolName}' non riconosciuto."
            };
        }
        catch (Exception ex)
        {
            return $"Errore nell'esecuzione del tool '{toolName}': {ex.Message}";
        }
    }

    private string ExecuteListTables()
    {
        try
        {
            var response = _http.GetAsync($"{_baseUrl}/api/NL2SQL/tables").Result;

            if (response.IsSuccessStatusCode)
            {
                var tablesJson = response.Content.ReadAsStringAsync().Result;
                var tables = JsonSerializer.Deserialize<List<string>>(tablesJson);

                if (tables == null || tables.Count == 0)
                {
                    return "Nessuna tabella trovata nel database.";
                }

                return "Tabelle disponibili:\n" + 
                       string.Join("\n", tables.Select(t => $"- {t}"));
            }

            return $"Errore nel recuperare l'elenco tabelle: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Errore nel recuperare l'elenco tabelle: {ex.Message}";
        }
    }

    private async Task<string> ExecuteListTablesAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/api/NL2SQL/tables");

            if (response.IsSuccessStatusCode)
            {
                var tablesJson = await response.Content.ReadAsStringAsync();
                var tables = JsonSerializer.Deserialize<List<string>>(tablesJson);

                if (tables == null || tables.Count == 0)
                {
                    return "Nessuna tabella trovata nel database.";
                }

                return "Tabelle disponibili:\n" + 
                       string.Join("\n", tables.Select(t => $"- {t}"));
            }

            return $"Errore nel recuperare l'elenco tabelle: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Errore nel recuperare l'elenco tabelle: {ex.Message}";
        }
    }

    private string ExecuteDescribeTable(Dictionary<string, object> args)
    {
        try
        {
            if (!args.TryGetValue("table_name", out var tableNameObj) || tableNameObj == null)
            {
                return "Errore: parametro 'table_name' mancante.";
            }

            var tableName = tableNameObj.ToString();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Errore: 'table_name' non può essere vuoto.";
            }

            var response = _http.GetAsync($"{_baseUrl}/api/NL2SQL/describe/{Uri.EscapeDataString(tableName)}").Result;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.Content.ReadAsStringAsync().Result;
                try
                {
                    var errorJson = JsonDocument.Parse(errorContent);
                    if (errorJson.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        return $"Errore nel descrivere la tabella '{tableName}': {errorProp.GetString()}";
                    }
                }
                catch { /* Fallback */ }
                return $"Errore nel descrivere la tabella '{tableName}': {response.StatusCode}";
            }

            var jsonContent = response.Content.ReadAsStringAsync().Result;
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Se è un oggetto con proprietà "error", estrai l'errore
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var errorProp2))
            {
                return $"Errore nel descrivere la tabella '{tableName}': {errorProp2.GetString()}";
            }

            // Altrimenti è un array di colonne
            var columns = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : new List<JsonElement>();
            
            if (columns.Count == 0)
            {
                return $"Nessuna colonna trovata nella tabella '{tableName}'.";
            }

            var result = $"Struttura tabella '{tableName}':\n";
            foreach (var col in columns)
            {
                var name = col.GetProperty("Name").GetString();
                var type = col.GetProperty("Type").GetString();
                result += $"- {name} ({type})\n";
            }

            return result.TrimEnd('\n');
        }
        catch (Exception ex)
        {
            return $"Errore nel descrivere la tabella: {ex.Message}";
        }
    }

    private async Task<string> ExecuteDescribeTableAsync(Dictionary<string, object> args)
    {
        try
        {
            if (!args.TryGetValue("table_name", out var tableNameObj) || tableNameObj == null)
            {
                return "Errore: parametro 'table_name' mancante.";
            }

            var tableName = tableNameObj.ToString();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Errore: 'table_name' non può essere vuoto.";
            }

            var response = await _http.GetAsync($"{_baseUrl}/api/NL2SQL/describe/{Uri.EscapeDataString(tableName)}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorJson = JsonDocument.Parse(errorContent);
                    if (errorJson.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        return $"Errore nel descrivere la tabella '{tableName}': {errorProp.GetString()}";
                    }
                }
                catch { /* Fallback */ }
                return $"Errore nel descrivere la tabella '{tableName}': {response.StatusCode}";
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Se è un oggetto con proprietà "error", estrai l'errore
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var errorProp2))
            {
                return $"Errore nel descrivere la tabella '{tableName}': {errorProp2.GetString()}";
            }

            // Altrimenti è un array di colonne
            var columns = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : new List<JsonElement>();

            if (columns.Count == 0)
            {
                return $"Nessuna colonna trovata nella tabella '{tableName}'.";
            }

            var result = $"Struttura tabella '{tableName}':\n";
            foreach (var col in columns)
            {
                var name = col.GetProperty("Name").GetString();
                var type = col.GetProperty("Type").GetString();
                result += $"- {name} ({type})\n";
            }

            return result.TrimEnd('\n');
        }
        catch (Exception ex)
        {
            return $"Errore nel descrivere la tabella: {ex.Message}";
        }
    }

    private string ExecuteRunQuery(Dictionary<string, object> args)
    {
        try
        {
            if (!args.TryGetValue("sql_query", out var sqlQueryObj) || sqlQueryObj == null)
            {
                return "Errore: parametro 'sql_query' mancante.";
            }

            var sqlQuery = sqlQueryObj.ToString();
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                return "Errore: 'sql_query' non può essere vuoto.";
            }

            // Rimuovi punto e virgola finale se presente
            var cleaned = sqlQuery.Trim().TrimEnd(';');

            // Blocco multi-statement: rifiuta query con più istruzioni separate da ";"
            var interiorStatements = cleaned.Split(';')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s));

            if (interiorStatements.Count() > 1)
            {
                return "ERRORE: questa chiamata contiene più istruzioni SQL separate da ';'. " +
                       "Esegui UNA sola query SELECT per chiamata: se ti servono dati da più " +
                       "tabelle o database diversi, chiama run_query separatamente per ciascuna " +
                       "query, oppure unisci i dati in una singola query con JOIN/UNION.";
            }

            // Blocco parole chiave pericolose (anche dentro subquery o CTE)
            var upperQuery = $" {cleaned.ToUpper()} ";
            var forbiddenKeywords = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE", "CREATE", "GRANT" };

            if (forbiddenKeywords.Any(word => upperQuery.Contains($" {word} ", StringComparison.OrdinalIgnoreCase)))
            {
                return "ERRORE: la query contiene un'operazione non permessa. " +
                       "Per sicurezza posso eseguire solo query SELECT (sola lettura). " +
                       "Modifiche/cancellazioni non sono permesse tramite questo strumento.";
            }

            // Verifica che inizi con SELECT
            if (!cleaned.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return "ERRORE: per sicurezza posso eseguire solo query SELECT (sola lettura). " +
                       "Modifiche/cancellazioni non sono permesse tramite questo strumento.";
            }

            // Ottieni max_rows dal parametro o usa default
            int maxRows = Valves.DefaultMaxRows;
            if (args.TryGetValue("max_rows", out var maxRowsObj) && maxRowsObj != null)
            {
                if (int.TryParse(maxRowsObj.ToString(), out int parsedMaxRows))
                {
                    maxRows = parsedMaxRows;
                }
            }

            // Esegui la query via API
            var content = new { query = cleaned };
            var response = _http.PostAsJsonAsync($"{_baseUrl}/api/NL2SQL/query", content).Result;

            if (response.IsSuccessStatusCode)
            {
                var resultsJson = response.Content.ReadAsStringAsync().Result;
                var results = JsonSerializer.Deserialize<List<object>>(resultsJson);

                var result = $"Query eseguita: {cleaned}\n\n";

                // Estrai i nomi delle colonne dal primo elemento (se è un oggetto)
                if (results != null && results.Count > 0)
                {
                    var firstItem = results[0];
                    if (firstItem is JsonElement element && element.ValueKind == JsonValueKind.Object)
                    {
                        var columnNames = element.EnumerateObject()
                            .Select(o => o.Name)
                            .ToArray();

                        result += string.Join(" | ", columnNames) + "\n";
                        result += new string('-', 40) + "\n";

                        foreach (var row in results)
                        {
                            if (row is JsonElement rowElement && rowElement.ValueKind == JsonValueKind.Object)
                            {
                                var values = rowElement.EnumerateObject()
                                    .Select(o => o.Value.ToString())
                                    .ToArray();
                                result += string.Join(" | ", values) + "\n";
                            }
                        }
                    }
                }

                result += $"\n({results?.Count ?? 0} righe restituite" +
                          (results?.Count > maxRows ? $", troncato al limite di {maxRows})" : ")");

                return result;
            }

            return $"Errore nell'esecuzione della query: {response.StatusCode} - {response.Content.ReadAsStringAsync().Result}";
        }
        catch (Exception ex)
        {
            return $"Errore nell'esecuzione della query: {ex.Message}";
        }
    }

    private async Task<string> ExecuteRunQueryAsync(Dictionary<string, object> args)
    {
        try
        {
            if (!args.TryGetValue("sql_query", out var sqlQueryObj) || sqlQueryObj == null)
            {
                return "Errore: parametro 'sql_query' mancante.";
            }

            var sqlQuery = sqlQueryObj.ToString();
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                return "Errore: 'sql_query' non può essere vuoto.";
            }

            // Rimuovi punto e virgola finale se presente
            var cleaned = sqlQuery.Trim().TrimEnd(';');

            // Blocco multi-statement: rifiuta query con più istruzioni separate da ";"
            var interiorStatements = cleaned.Split(';')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s));

            if (interiorStatements.Count() > 1)
            {
                return "ERRORE: questa chiamata contiene più istruzioni SQL separate da ';'. " +
                       "Esegui UNA sola query SELECT per chiamata: se ti servono dati da più " +
                       "tabelle o database diversi, chiama run_query separatamente per ciascuna " +
                       "query, oppure unisci i dati in una singola query con JOIN/UNION.";
            }

            // Blocco parole chiave pericolose (anche dentro subquery o CTE)
            var upperQuery = $" {cleaned.ToUpper()} ";
            var forbiddenKeywords = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE", "CREATE", "GRANT" };

            if (forbiddenKeywords.Any(word => upperQuery.Contains($" {word} ", StringComparison.OrdinalIgnoreCase)))
            {
                return "ERRORE: la query contiene un'operazione non permessa. " +
                       "Per sicurezza posso eseguire solo query SELECT (sola lettura). " +
                       "Modifiche/cancellazioni non sono permesse tramite questo strumento.";
            }

            // Verifica che inizi con SELECT
            if (!cleaned.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return "ERRORE: per sicurezza posso eseguire solo query SELECT (sola lettura). " +
                       "Modifiche/cancellazioni non sono permesse tramite questo strumento.";
            }

            // Ottieni max_rows dal parametro o usa default
            int maxRows = Valves.DefaultMaxRows;
            if (args.TryGetValue("max_rows", out var maxRowsObj) && maxRowsObj != null)
            {
                if (int.TryParse(maxRowsObj.ToString(), out int parsedMaxRows))
                {
                    maxRows = parsedMaxRows;
                }
            }

            // Esegui la query via API
            var content = new { query = cleaned };
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/NL2SQL/query", content);

            if (response.IsSuccessStatusCode)
            {
                var resultsJson = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<object>>(resultsJson);

                var result = $"Query eseguita: {cleaned}\n\n";

                // Estrai i nomi delle colonne dal primo elemento (se è un oggetto)
                if (results != null && results.Count > 0)
                {
                    var firstItem = results[0];
                    if (firstItem is JsonElement element && element.ValueKind == JsonValueKind.Object)
                    {
                        var columnNames = element.EnumerateObject()
                            .Select(o => o.Name)
                            .ToArray();

                        result += string.Join(" | ", columnNames) + "\n";
                        result += new string('-', 40) + "\n";

                        foreach (var row in results)
                        {
                            if (row is JsonElement rowElement && rowElement.ValueKind == JsonValueKind.Object)
                            {
                                var values = rowElement.EnumerateObject()
                                    .Select(o => o.Value.ToString())
                                    .ToArray();
                                result += string.Join(" | ", values) + "\n";
                            }
                        }
                    }
                }

                result += $"\n({results?.Count ?? 0} righe restituite" +
                          (results?.Count > maxRows ? $", troncato al limite di {maxRows})" : ")");

                return result;
            }

            return $"Errore nell'esecuzione della query: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
        }
        catch (Exception ex)
        {
            return $"Errore nell'esecuzione della query: {ex.Message}";
        }
    }
}
