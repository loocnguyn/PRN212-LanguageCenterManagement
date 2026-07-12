using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Services;

/// <summary>
/// AI Study Assistant backed by the Anthropic Claude Messages API.
/// Follows the same config-driven, HttpClient pattern as <see cref="ZaloPayService"/>:
/// the API key and model come from the "Anthropic" section of appsettings.json.
/// The student's own data is passed in as a plain-text context string and sent as the
/// system prompt, so the model can answer questions about that student's schedule,
/// grades, and invoices without the service touching the database directly.
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly HttpClient _httpClient = new();
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint;
    private const string ApiVersion = "2023-06-01";

    public AiAssistantService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var section = config.GetSection("Anthropic");
        _apiKey = section["ApiKey"] ?? "";
        // Default to the latest Claude model; can be pointed at a cheaper one (e.g. claude-haiku-4-5)
        // via appsettings without touching code.
        _model = section["Model"] ?? "claude-opus-4-8";
        _endpoint = section["Endpoint"] ?? "https://api.anthropic.com/v1/messages";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AiAssistantResult> AskAsync(string question, string studentContext)
    {
        if (!IsConfigured)
        {
            return new AiAssistantResult
            {
                Success = false,
                Error = "AI assistant is not configured. Add an \"Anthropic\" section with an ApiKey to appsettings.json."
            };
        }

        var systemPrompt =
            "You are a helpful study assistant for a student at a language center. " +
            "Answer the student's questions using ONLY the data below about their own schedule, grades, and tuition. " +
            "If the answer is not in the data, say you don't have that information. Be concise and friendly.\n\n" +
            "=== STUDENT DATA ===\n" + studentContext;

        var payload = new
        {
            model = _model,
            max_tokens = 1024,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = question }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", ApiVersion);

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AiAssistantResult
                {
                    Success = false,
                    Error = $"AI request failed ({(int)response.StatusCode}): {ExtractError(body)}"
                };
            }

            return new AiAssistantResult { Success = true, Answer = ExtractText(body) };
        }
        catch (Exception ex)
        {
            return new AiAssistantResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Pull the first text block out of the Messages API response
    /// (shape: { "content": [ { "type": "text", "text": "..." } ] }).</summary>
    private static string ExtractText(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "text" &&
                    block.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? "";
                }
            }
        }
        return "(No response text returned.)";
    }

    private static string ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
            {
                return msg.GetString() ?? body;
            }
        }
        catch
        {
            // fall through to raw body
        }
        return body;
    }
}
