namespace Services;

public interface IAiAssistantService
{
    /// <summary>True when a Gemini API key is configured in appsettings.
    /// The UI uses this to show a friendly "not configured" message instead of failing.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Ask the assistant a question.
    /// <paramref name="studentContext"/> is a plain-text snapshot of THIS student's own data,
    /// built by the caller and sent as system context.
    /// <paramref name="history"/> is the conversation so far, oldest first — without it the
    /// model cannot answer a follow-up like "and the other class?".
    /// </summary>
    Task<AiAssistantResult> AskAsync(string question, string studentContext, IEnumerable<ChatTurn>? history = null);
}

/// <summary>One exchange already in the conversation.</summary>
public class ChatTurn
{
    /// <summary>True when the student wrote it, false when the assistant did.</summary>
    public bool IsUser { get; set; }

    public string Text { get; set; } = "";
}

public class AiAssistantResult
{
    public bool Success { get; set; }
    public string? Answer { get; set; }
    public string? Error { get; set; }
}
