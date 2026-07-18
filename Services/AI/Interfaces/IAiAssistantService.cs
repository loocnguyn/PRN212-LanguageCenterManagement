namespace Services;

public interface IAiAssistantService
{
    /// <summary>True when a Gemini API key is configured in appsettings.
    /// The UI uses this to show a friendly "not configured" message instead of failing.</summary>
    bool IsConfigured { get; }

    /// <summary>Ask the AI assistant a question. <paramref name="studentContext"/> is a plain-text
    /// snapshot of the student's own data (schedule, grades, invoices) built by the caller and sent
    /// as system context so the model can answer about the student's situation.</summary>
    Task<AiAssistantResult> AskAsync(string question, string studentContext);
}

public class AiAssistantResult
{
    public bool Success { get; set; }
    public string? Answer { get; set; }
    public string? Error { get; set; }
}
