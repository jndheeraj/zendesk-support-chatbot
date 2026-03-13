namespace KyrisCBL.Pipeline.HumanReview;

/// <summary>
/// Abstraction over the ticketing backend used for Human-in-the-Loop escalations.
/// Swap the implementation to connect to Zendesk, Jira, Freshdesk, or any other system.
/// </summary>
public interface ITicketingService
{
    /// <summary>Creates a new ticket or appends to an existing one.</summary>
    Task CreateOrUpdateAsync(SupportTicket ticket, CancellationToken ct = default);

    /// <summary>Returns all open tickets pending human review.</summary>
    Task<IReadOnlyList<SupportTicket>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Resolves a ticket with an optional resolution note.</summary>
    Task ResolveAsync(string ticketId, string? resolution = null, CancellationToken ct = default);
}
