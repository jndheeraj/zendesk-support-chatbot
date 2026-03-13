using KyrisCBL.Models;

namespace KyrisCBL.Pipeline.Core;

/// <summary>
/// Shared mutable state passed through every agent in the pipeline.
/// Agents read from and write to this object to communicate with each other.
/// </summary>
public sealed class AgentContext
{
    // ── Inputs (set by the caller before running the pipeline) ──────────────

    /// <summary>Raw message from the user.</summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>User email extracted from session or request body (may be null for anonymous).</summary>
    public string? UserEmail { get; set; }

    /// <summary>Whether the current user has an active authenticated session.</summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>Rolling conversation memory (intent history, clarification attempts).</summary>
    public ChatMemoryState Memory { get; set; } = new();

    // ── Populated by IntentClassificationAgent ───────────────────────────────

    /// <summary>Classified intent label (e.g. "reset_password", "faq", "other").</summary>
    public string? DetectedIntent { get; set; }

    /// <summary>Whether the LLM requested escalation to a human agent.</summary>
    public bool EscalationRequested { get; set; }

    /// <summary>Preliminary response text produced by the classifier (used as fallback).</summary>
    public string? ClassifierResponse { get; set; }

    // ── Populated by KnowledgeRetrievalAgent ────────────────────────────────

    /// <summary>Documents retrieved from the vector store. Empty when skipped.</summary>
    public List<string> RetrievedDocuments { get; set; } = new();

    // ── Populated by ResponseGenerationAgent ────────────────────────────────

    /// <summary>The final structured LLM response.</summary>
    public GptStructuredResponse? StructuredResponse { get; set; }

    // ── Populated by WorkflowExecutionAgent ─────────────────────────────────

    /// <summary>True when a workflow action (unsubscribe, profile update, etc.) completed.</summary>
    public bool WorkflowExecuted { get; set; }

    /// <summary>Result message from the workflow execution.</summary>
    public string? WorkflowMessage { get; set; }

    // ── Human-in-the-Loop (HITL) ────────────────────────────────────────────

    /// <summary>True when the conversation requires human review / a support ticket.</summary>
    public bool RequiresHumanReview { get; set; }

    /// <summary>Support ticket ID created by the HITL layer (if any).</summary>
    public string? TicketId { get; set; }

    // ── Pipeline control ─────────────────────────────────────────────────────

    /// <summary>
    /// When set to true by any agent, subsequent agents are skipped.
    /// Use to short-circuit the pipeline when a definitive answer is already available.
    /// </summary>
    public bool IsComplete { get; set; }

    // ── Final output ─────────────────────────────────────────────────────────

    /// <summary>The reply text that will be returned to the user.</summary>
    public string FinalResponse { get; set; } = string.Empty;

    /// <summary>Whether the user's issue was fully resolved.</summary>
    public bool Solved { get; set; }
}
