using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Services;

/// <summary>
/// AI Study Assistant backed by the Google Gemini API (generativelanguage.googleapis.com),
/// which has a free tier suitable for a student project. Follows the same config-driven,
/// HttpClient pattern as <see cref="ZaloPayService"/>: the API key, model, and endpoint come
/// from the "Gemini" section of appsettings.json. The student's own data is passed in as a
/// plain-text context string and sent as the system instruction, so the model can answer
/// questions about that student's schedule, grades, and invoices without the service touching
/// the database directly.
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly HttpClient _httpClient = new();
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpointBase;

    public AiAssistantService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var section = config.GetSection("Gemini");
        _apiKey = section["ApiKey"] ?? "";
        // Free-tier, fast model; can be pointed at another Gemini model via appsettings.
        _model = section["Model"] ?? "gemini-2.0-flash";
        _endpointBase = section["Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AiAssistantResult> AskAsync(string question, string studentContext)
    {
        if (!IsConfigured)
        {
            return new AiAssistantResult
            {
                Success = false,
                Error = "AI assistant is not configured. Add a \"Gemini\" section with an ApiKey to appsettings.json."
            };
        }

        var systemPrompt =
            "You are a helpful study assistant for a student at a language center. " +
            "Answer the student's questions using ONLY the data below about their own schedule, grades, and tuition. " +
            "If the answer is not in the data, say you don't have that information. Be concise and friendly.\n\n" +
            "=== STUDENT DATA ===\n" + studentContext;

        // Gemini generateContent request shape.
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = question } } }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var url = $"{_endpointBase.TrimEnd('/')}/{_model}:generateContent";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

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

    /// <summary>Pull the first text part out of the Gemini response
    /// (shape: { "candidates": [ { "content": { "parts": [ { "text": "..." } ] } } ] }).</summary>
    private static string ExtractText(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (root.TryGetProperty("candidates", out var candidates) &&
            candidates.ValueKind == JsonValueKind.Array &&
            candidates.GetArrayLength() > 0)
        {
            var first = candidates[0];
            if (first.TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text))
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
