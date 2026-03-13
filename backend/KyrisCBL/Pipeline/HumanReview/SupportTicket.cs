namespace KyrisCBL.Pipeline.HumanReview;

public enum TicketStatus { PendingHumanReview, InProgress, Resolved, Closed }

/// <summary>Represents a support ticket created when the chatbot escalates to a human agent.</summary>
public sealed class SupportTicket
{
    public string TicketId { get; init; } = string.Empty;
    public string? UserEmail { get; init; }
    public string UserMessage { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public TicketStatus Status { get; set; }
    public string? Resolution { get; set; }
    public DateTime? ResolvedUtc { get; set; }
}
