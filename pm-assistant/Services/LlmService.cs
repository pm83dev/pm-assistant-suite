using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PmAssistant.Services;

public interface ILlmService
{
    Task<string> GenerateAsync(string prompt, string? systemPrompt = null, int? maxTokens = null);
    Task<bool> HealthCheckAsync();
}

public class LlmSettings
{
    public string BaseUrl { get; set; } = "";
    public string ModelName { get; set; } = "";
    public float Temperature { get; set; } = 0.1f;
    public int MaxTokens { get; set; } = 4096;
    public float TopP { get; set; } = 0.9f;
    public int RetryAttempts { get; set; } = 3;
}

public class LlmService : ILlmService
{
    private readonly HttpClient _http;
    private readonly LlmSettings _settings;
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public LlmService(HttpClient http, IOptions<LlmSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/v1/models");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GenerateAsync(string prompt, string? systemPrompt = null, int? maxTokens = null)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new { role = "system", content = systemPrompt });
        }

        messages.Add(new { role = "user", content = prompt });

        var request = new
        {
            model = _settings.ModelName,
            messages,
            temperature = _settings.Temperature,
            max_tokens = maxTokens ?? _settings.MaxTokens,
            top_p = _settings.TopP,
            stream = false
        };

        for (var attempt = 1; attempt <= _settings.RetryAttempts; attempt++)
        {
            try
            {
                var content = JsonContent.Create(request, options: _jsonOpts);
                var resp = await _http.PostAsync("/v1/chat/completions", content);

                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var msg = choices[0].GetProperty("message");
                        var contentProp = msg.GetProperty("content");
                        return contentProp.GetString() ?? "";
                    }
                }

                if (attempt < _settings.RetryAttempts)
                {
                    var delayMs = 1000 * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delayMs);
                }
            }
            catch
            {
                if (attempt < _settings.RetryAttempts)
                {
                    var delayMs = 1000 * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delayMs);
                }
                else
                {
                    throw;
                }
            }
        }

        return "";
    }
}
