using System.Net.Http.Json;
using System.Text.Json;
using LocalCodeAgent.Models;

namespace PmAssistant.Tools;

/// <summary>
/// Tool per interagire con il servizio ore-tracking/Api (Clienti, Progetti, Ore lavorate, Note)
/// tramite le sue API REST. Il servizio resta un processo .NET separato: questi tool si limitano
/// a chiamarlo via HTTP, esattamente come fa il frontend Angular in ore-tracking/Web.
/// </summary>
public class OreTrackingTools(string baseUrl)
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string _baseUrl = baseUrl.TrimEnd('/');

    public List<ToolDefinition> Definitions =>
    [
        new() { Function = new() {
            Name = "ore_list_clienti",
            Description = "Elenca tutti i clienti registrati nel sistema di tracciamento ore.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }},
        new() { Function = new() {
            Name = "ore_create_cliente",
            Description = "Crea un nuovo cliente nel sistema di tracciamento ore.",
            Parameters = new { type = "object", properties = new {
                nome      = new { type = "string", description = "Nome del cliente" },
                email     = new { type = "string", description = "Opzionale" },
                telefono  = new { type = "string", description = "Opzionale" },
                indirizzo = new { type = "string", description = "Opzionale" }
            }, required = new[] { "nome" } }
        }},
        new() { Function = new() {
            Name = "ore_list_progetti",
            Description = "Elenca i progetti. Se cliente_id è specificato, filtra solo i progetti di quel cliente.",
            Parameters = new { type = "object", properties = new {
                cliente_id = new { type = "integer", description = "Opzionale: filtra per cliente" }
            }, required = Array.Empty<string>() }
        }},
        new() { Function = new() {
            Name = "ore_create_progetto",
            Description = "Crea un nuovo progetto associato a un cliente esistente.",
            Parameters = new { type = "object", properties = new {
                nome        = new { type = "string", description = "Nome del progetto" },
                cliente_id  = new { type = "integer", description = "Id del cliente (usa ore_list_clienti per trovarlo)" },
                descrizione = new { type = "string", description = "Opzionale" }
            }, required = new[] { "nome", "cliente_id" } }
        }},
        new() { Function = new() {
            Name = "ore_list_ore",
            Description = "Elenca le ore lavorate. Se progetto_id è specificato, filtra solo quel progetto.",
            Parameters = new { type = "object", properties = new {
                progetto_id = new { type = "integer", description = "Opzionale: filtra per progetto" }
            }, required = Array.Empty<string>() }
        }},
        new() { Function = new() {
            Name = "ore_log_ora",
            Description = "Registra ore lavorate su un progetto (data odierna, impostata dal server). Non chiede conferma: scrive subito.",
            Parameters = new { type = "object", properties = new {
                progetto_id = new { type = "integer", description = "Id del progetto (usa ore_list_progetti per trovarlo)" },
                ore         = new { type = "number", description = "Ore lavorate, tra 0.25 e 24" },
                descrizione = new { type = "string", description = "Opzionale" }
            }, required = new[] { "progetto_id", "ore" } }
        }},
        new() { Function = new() {
            Name = "ore_totale_progetto",
            Description = "Restituisce il totale ore registrate su un progetto.",
            Parameters = new { type = "object", properties = new {
                progetto_id = new { type = "integer" }
            }, required = new[] { "progetto_id" } }
        }},
        new() { Function = new() {
            Name = "ore_list_note",
            Description = "Elenca le note. Se progetto_id è specificato, filtra solo quel progetto.",
            Parameters = new { type = "object", properties = new {
                progetto_id = new { type = "integer", description = "Opzionale: filtra per progetto" }
            }, required = Array.Empty<string>() }
        }},
        new() { Function = new() {
            Name = "ore_add_nota",
            Description = "Aggiunge una nota a un progetto.",
            Parameters = new { type = "object", properties = new {
                progetto_id = new { type = "integer" },
                contenuto   = new { type = "string" },
                titolo      = new { type = "string", description = "Opzionale" }
            }, required = new[] { "progetto_id", "contenuto" } }
        }}
    ];

    public string Execute(string toolName, string argumentsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(
                string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);

            return toolName switch
            {
                "ore_list_clienti"     => Get("/api/time-tracking/clienti").GetAwaiter().GetResult(),
                "ore_create_cliente"   => CreateCliente(args).GetAwaiter().GetResult(),
                "ore_list_progetti"    => ListProgetti(args).GetAwaiter().GetResult(),
                "ore_create_progetto"  => CreateProgetto(args).GetAwaiter().GetResult(),
                "ore_list_ore"         => ListOre(args).GetAwaiter().GetResult(),
                "ore_log_ora"          => LogOra(args).GetAwaiter().GetResult(),
                "ore_totale_progetto"  => TotaleProgetto(args).GetAwaiter().GetResult(),
                "ore_list_note"        => ListNote(args).GetAwaiter().GetResult(),
                "ore_add_nota"         => AddNota(args).GetAwaiter().GetResult(),
                _ => $"Tool '{toolName}' non trovato."
            };
        }
        catch (HttpRequestException ex)
        {
            return $"ERRORE: impossibile contattare il servizio ore-tracking ({_baseUrl}). " +
                   $"È avviato? Dettagli: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"ERRORE: {ex.Message}";
        }
    }

    private async Task<string> Get(string path)
    {
        var res = await _http.GetAsync($"{_baseUrl}{path}");
        var body = await res.Content.ReadAsStringAsync();
        return res.IsSuccessStatusCode ? body : $"ERRORE HTTP {(int)res.StatusCode}: {body}";
    }

    private async Task<string> Post(string path, object payload)
    {
        var res = await _http.PostAsJsonAsync($"{_baseUrl}{path}", payload);
        var body = await res.Content.ReadAsStringAsync();
        return res.IsSuccessStatusCode ? body : $"ERRORE HTTP {(int)res.StatusCode}: {body}";
    }

    private async Task<string> CreateCliente(JsonElement args) => await Post("/api/time-tracking/clienti", new
    {
        nome = args.GetProperty("nome").GetString(),
        email = args.TryGetProperty("email", out var e) ? e.GetString() : null,
        telefono = args.TryGetProperty("telefono", out var t) ? t.GetString() : null,
        indirizzo = args.TryGetProperty("indirizzo", out var i) ? i.GetString() : null
    });

    private async Task<string> ListProgetti(JsonElement args) =>
        args.TryGetProperty("cliente_id", out var cid) && cid.ValueKind == JsonValueKind.Number
            ? await Get($"/api/time-tracking/progetti/cliente/{cid.GetInt32()}")
            : await Get("/api/time-tracking/progetti");

    private async Task<string> CreateProgetto(JsonElement args) => await Post("/api/time-tracking/progetti", new
    {
        nome = args.GetProperty("nome").GetString(),
        clienteId = args.GetProperty("cliente_id").GetInt32(),
        descrizione = args.TryGetProperty("descrizione", out var d) ? d.GetString() : null
    });

    private async Task<string> ListOre(JsonElement args) =>
        args.TryGetProperty("progetto_id", out var pid) && pid.ValueKind == JsonValueKind.Number
            ? await Get($"/api/time-tracking/ore/progetto/{pid.GetInt32()}")
            : await Get("/api/time-tracking/ore");

    private async Task<string> LogOra(JsonElement args) => await Post("/api/time-tracking/ore", new
    {
        progettoId = args.GetProperty("progetto_id").GetInt32(),
        ore = args.GetProperty("ore").GetDecimal(),
        descrizione = args.TryGetProperty("descrizione", out var d) ? d.GetString() : null
    });

    private async Task<string> TotaleProgetto(JsonElement args)
    {
        var id = args.GetProperty("progetto_id").GetInt32();
        return await Get($"/api/time-tracking/ore/progetto/{id}/total");
    }

    private async Task<string> ListNote(JsonElement args) =>
        args.TryGetProperty("progetto_id", out var pid) && pid.ValueKind == JsonValueKind.Number
            ? await Get($"/api/time-tracking/note/progetto/{pid.GetInt32()}")
            : await Get("/api/time-tracking/note");

    private async Task<string> AddNota(JsonElement args) => await Post("/api/time-tracking/note", new
    {
        progettoId = args.GetProperty("progetto_id").GetInt32(),
        contenuto = args.GetProperty("contenuto").GetString(),
        titolo = args.TryGetProperty("titolo", out var t) ? t.GetString() : null
    });
}
