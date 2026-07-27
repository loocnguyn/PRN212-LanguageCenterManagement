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
// ============================================================
//  AiAssistantService — asks Google Gemini a student's question with context.
//  CONTENTS:
//    1. Construction & config  — reads the API key from appsettings
//    2. IsConfigured           — whether an API key is present
//    3. AskAsync               — build prompt, call Gemini, parse the answer
// ============================================================
public class AiAssistantService : IAiAssistantService
{
    /// <summary>
    /// Used when appsettings has no "Gemini:SystemPrompt". Kept here only so the app
    /// still works with a config file that predates the setting — edit the JSON, not this.
    /// </summary>
    private const string DefaultSystemPrompt =
        "You are the study assistant of a student at a language center. " +
        "Answer using ONLY the student data below - it is that student's own record and is complete. " +
        "If something genuinely is not in the data, say so plainly instead of guessing. " +
        "Reply in the same language the student wrote in (Vietnamese or English). " +
        "Money is Vietnamese dong; dates are dd/MM/yyyy. Be concise, warm and specific: quote the " +
        "actual class names, dates and amounts rather than describing them in general terms.";

    private readonly HttpClient _httpClient = new();
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpointBase;
    private readonly string _systemPrompt;

    public AiAssistantService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var section = config.GetSection("Gemini");
        _apiKey = section["ApiKey"] ?? "";
        // Older model names (gemini-2.0-flash, gemini-2.5-flash) return 0 free-tier quota
        // or a 404 "no longer available to new users" on newly created API keys.
        // gemini-3.1-flash-lite is confirmed (via the Rate Limits dashboard) to have a
        // nonzero free quota, so default to that.
        _model = section["Model"] ?? "gemini-3.1-flash-lite";
        _endpointBase = section["Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models";

        // How the assistant is told to behave is a setting, not code: it gets reworded
        // far more often than anything else here, and doing so should not need a rebuild.
        var configured = section["SystemPrompt"];
        _systemPrompt = string.IsNullOrWhiteSpace(configured) ? DefaultSystemPrompt : configured;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AiAssistantResult> AskAsync(
        string question, string studentContext, IEnumerable<ChatTurn>? history = null)
    {
        if (!IsConfigured)
        {
            return new AiAssistantResult
            {
                Success = false,
                Error = "AI assistant is not configured. Add a \"Gemini\" section with an ApiKey to appsettings.json."
            };
        }

        // Instructions come from appsettings; the student's own data is appended here,
        // so a reworded prompt can never accidentally drop the context block.
        var systemPrompt = _systemPrompt + "\n\n=== STUDENT DATA ===\n" + studentContext;

        // Gemini wants the whole conversation each time — it keeps no state of its own.
        // Oldest first, then the new question last.
        var contents = new List<object>();
        if (history != null)
        {
            foreach (var turn in history)
            {
                contents.Add(new
                {
                    role = turn.IsUser ? "user" : "model",
                    parts = new[] { new { text = turn.Text } }
                });
            }
        }
        contents.Add(new { role = "user", parts = new[] { new { text = question } } });

        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents
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
