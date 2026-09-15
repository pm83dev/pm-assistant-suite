using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreTracking.Api.Models;
using OreTracking.Api.Services;
using System.Data;
using System.Reflection;

namespace OreTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NL2SQLController : ControllerBase
    {
        private readonly DataContext _context;

        public NL2SQLController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("schema")]
        public IActionResult GetDatabaseSchema()
        {
            try
            {
                var model = _context.Model;
                var tables = new List<string>();

                foreach (var entityType in model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();
                    var columns = new List<string>();

                    foreach (var property in entityType.GetProperties())
                    {
                        columns.Add($"{property.Name} ({property.ClrType.Name})");
                    }

                    tables.Add($"Table: {tableName}\nColumns: {string.Join(", ", columns)}");
                }

                var schemaJson = new { tables = string.Join("\n\n", tables) };
                return Ok(schemaJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Errore nel recupero dello schema", message = ex.Message });
            }
        }

        [HttpGet("tables")]
        public IActionResult ListTables()
        {
            try
            {
                var tables = new List<string>();

                foreach (var entityType in _context.Model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();
                    // Filtra tabelle con nome null (es. tabelle private/internal)
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        tables.Add(tableName);
                    }
                }

                return Ok(tables);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Errore nel recupero dell'elenco tabelle", message = ex.Message });
            }
        }

        [HttpGet("describe/{tableName}")]
        public IActionResult DescribeTable(string tableName)
        {
            try
            {
                var model = _context.Model;
                var tableInfo = new List<(string Name, string Type)>();

                foreach (var entityType in model.GetEntityTypes())
                {
                    var entityTableName = entityType.GetTableName();

                    if (string.Equals(entityTableName, tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Usa GetDeclaredProperties() per ottenere le proprietà della tabella
                        foreach (var property in entityType.GetDeclaredProperties())
                        {
                            var clrType = property.ClrType;

                            // In .NET 8, GetTypeInfo() è deprecato. Usiamo IsValueType per distinguere struct/class
                            string typeName;
                            if (clrType.IsValueType && !clrType.IsPrimitive && clrType != typeof(decimal))
                            {
                                // Struct (es. Guid, DateTime, TimeSpan)
                                typeName = clrType.Name;
                            }
                            else if (clrType.IsEnum)
                            {
                                // Enum
                                typeName = clrType.Name;
                            }
                            else
                            {
                                // Class/Reference type (es. string, int, decimal)
                                typeName = clrType.Name;
                            }

                            // Usa il nome della colonna EF o del database se disponibile
                            var columnName = property.GetColumnName();
                            
                            tableInfo.Add((columnName, typeName));
                        }

                        return Ok(tableInfo);
                    }
                }

                return NotFound(new { error = $"Tabella '{tableName}' non trovata" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Errore nel descrivere la tabella '{tableName}'", message = ex.Message });
            }
        }

        [HttpPost("query")]
        public async Task<IActionResult> ExecuteNL2SQL([FromBody] NL2SQLRequest request, int? maxRows = null)
        {
            try
            {
                // Validazione sicurezza: solo query SELECT
                var sqlQuery = request.Query;

                // Rimuovi punto e virgola finale se presente
                sqlQuery = sqlQuery.Trim().TrimEnd(';');

                // Blocco multi-statement: rifiuta query con più istruzioni separate da ";"
                var interiorStatements = sqlQuery.Split(';')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s));

                if (interiorStatements.Count() > 1)
                {
                    return BadRequest(new 
                    { 
                        error = "Questa chiamata contiene più istruzioni SQL separate da ';'. Esegui UNA sola query SELECT per chiamata.",
                        message = "Se ti servono dati da più tabelle o database diversi, chiama run_query separatamente per ciascuna query, oppure unisci i dati in una singola query con JOIN/UNION."
                    });
                }

                // Blocco parole chiave pericolose (anche dentro subquery o CTE)
                var upperQuery = $" {sqlQuery.ToUpper()} ";
                var forbiddenKeywords = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE", "CREATE", "GRANT" };

                if (forbiddenKeywords.Any(word => upperQuery.Contains($" {word} ", StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new 
                    { 
                        error = "La query contiene un'operazione non permessa.",
                        message = "Per sicurezza posso eseguire solo query SELECT (sola lettura). Modifiche/cancellazioni non sono permesse tramite questo strumento."
                    });
                }

                // Verifica che inizi con SELECT
                if (!sqlQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new 
                    { 
                        error = "Per sicurezza posso eseguire solo query SELECT (sola lettura).",
                        message = "Modifiche/cancellazioni non sono permesse tramite questo strumento."
                    });
                }

                // Esegui la query SQL sul database usando DbConnection per supportare tabelle arbitrarie
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;
                using var reader = await command.ExecuteReaderAsync();
                
                var dataTable = new DataTable();
                dataTable.Load(reader);

                // Applica limite righe se specificato
                int limit = maxRows ?? 100;
                if (dataTable.Rows.Count > limit)
                {
                    var limitedTable = dataTable.Clone();
                    for (int i = 0; i < limit; i++)
                    {
                        limitedTable.ImportRow(dataTable.Rows[i]);
                    }
                    dataTable = limitedTable;
                }

                // Converti DataTable in lista di dizionari per la risposta JSON
                var results = new List<Dictionary<string, object?>>();
                foreach (DataRow row in dataTable.Rows)
                {
                    var rowDict = new Dictionary<string, object?>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        rowDict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                    }
                    results.Add(rowDict);
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Errore nell'elaborazione della query", message = ex.Message });
            }
        }

        private string ConvertNL2SQL(string naturalQuery)
        {
            // Normalizzazione della query
            var normalizedQuery = naturalQuery.ToLower().Trim();

            // Regole di conversione semplici per esempio
            // Controlla prima se chiede un totale/sommatoria
            if (normalizedQuery.Contains("totale") && (normalizedQuery.Contains("ore") || normalizedQuery.Contains("ore lavorate")))
            {
                return "SELECT SUM(Ore) as TotaleOre FROM OreLavorate";
            }
            else if (normalizedQuery.Contains("totale ore"))
            {
                return "SELECT SUM(Ore) as TotaleOre FROM OreLavorate";
            }
            else if (normalizedQuery.Contains("progetti"))
            {
                return "SELECT * FROM Progetti";
            }
            else if (normalizedQuery.Contains("clienti"))
            {
                return "SELECT * FROM Clienti";
            }
            else if (normalizedQuery.Contains("ore lavorate"))
            {
                return "SELECT * FROM OreLavorate";
            }
            else if (normalizedQuery.Contains("note"))
            {
                return "SELECT * FROM Note";
            }

            // Se non riconosce nessun pattern, restituisce una query generica
            return "SELECT * FROM OreLavorate";
        }
    }

    public class NL2SQLRequest
    {
        public string Query { get; set; } = string.Empty;
    }
}