using System.Text.Json;
using System.Text.RegularExpressions;
using LocalCodeAgent.Models;

namespace LocalCodeAgent.Tools;

public class WebTools
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly string? _googleApiKey;
    private readonly string? _googleCx;

    public WebTools(string? googleApiKey = null, string? googleCx = null)
    {
        _googleApiKey = googleApiKey;
        _googleCx = googleCx;
    }

    public List<ToolDefinition> Definitions =>
    [
        new() { Function = new() {
            Name = "web_fetch",
            Description = "Scarica il contenuto di un URL. Restituisce testo pulito (HTML stripped) o raw.",
            Parameters = new { type = "object",
                properties = new {
                    url       = new { type = "string",  description = "URL da scaricare" },
                    raw       = new { type = "boolean", description = "Restituisce HTML grezzo invece del testo pulito" },
                    max_chars = new { type = "integer", description = "Limite caratteri output (default 20000)" }
                },
                required = new[] { "url" } }
        }},
        new() { Function = new() {
            Name = "web_search",
            Description = "Cerca informazioni sul web via Google Custom Search API e restituisce titolo, URL e snippet.",
            Parameters = new { type = "object",
                properties = new {
                    query   = new { type = "string",  description = "Query di ricerca" },
                    results = new { type = "integer", description = "Numero di risultati (default 5)" }
                },
                required = new[] { "query" } }
        }}
    ];

    public string Execute(string toolName, string argumentsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
            return toolName switch
            {
                "web_fetch"  => WebFetch(args),
                "web_search" => WebSearch(args),
                _ => $"Tool '{toolName}' non trovato."
            };
        }
        catch (Exception ex) { return $"ERRORE: {ex.Message}"; }
    }

    private string WebFetch(JsonElement args)
    {
        var url      = args.GetProperty("url").GetString()!;
        var raw      = args.TryGetProperty("raw",       out var r) && r.GetBoolean();
        var maxChars = args.TryGetProperty("max_chars", out var m) ? m.GetInt32() : 20_000;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; LocalCodeAgent/1.0)");

        var response = _http.SendAsync(request).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var mime    = response.Content.Headers.ContentType?.MediaType ?? "";

        if (!raw && mime.Contains("html"))
            content = StripHtml(content);

        if (content.Length > maxChars)
            content = content[..maxChars] + $"\n\n[Troncato a {maxChars} caratteri]";

        return content;
    }

    private string WebSearch(JsonElement args)
    {
        if (string.IsNullOrEmpty(_googleApiKey))
            return "[web_search NON DISPONIBILE: API key di Google Custom Search non configurata in appsettings.json. Usa web_fetch con un URL specifico per scaricare contenuti da una pagina web.]";

        if (string.IsNullOrEmpty(_googleCx))
            return "[web_search NON DISPONIBILE: Search Engine ID (CX) non configurato in appsettings.json. Usa web_fetch con un URL specifico per scaricare contenuti da una pagina web.]";

        var query = args.GetProperty("query").GetString()!;
        var count = args.TryGetProperty("results", out var r) ? r.GetInt32() : 5;
        if (count > 10) count = 10; // Google API max 10 per richiesta

        var url = $"https://www.googleapis.com/customsearch/v1?key={_googleApiKey}&cx={_googleCx}&q={Uri.EscapeDataString(query)}&num={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");

        var response = _http.SendAsync(request).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc  = JsonDocument.Parse(json);

        var sb = new System.Text.StringBuilder();
        
        // Google Custom Search response format
        if (doc.RootElement.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var title   = item.TryGetProperty("title",       out var t) ? t.GetString() : "";
                var itemUrl = item.TryGetProperty("link",         out var u) ? u.GetString() : "";
                var snippet = item.TryGetProperty("snippet",      out var s) ? s.GetString() : "";
                sb.AppendLine($"{title}\n{itemUrl}\n{snippet}\n");
            }
        }

        var output = sb.ToString().Trim();
        return string.IsNullOrEmpty(output) ? "Nessun risultato trovato." : output;
    }

    private static string StripHtml(string html)
    {
        html = Regex.Replace(html,
            @"<(script|style)[^>]*>.*?</(script|style)>", "",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", " ");
        html = Regex.Replace(html, @"\s{2,}", "\n").Trim();
        return html;
    }
}
