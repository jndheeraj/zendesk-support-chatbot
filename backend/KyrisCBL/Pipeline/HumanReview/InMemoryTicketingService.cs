using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace KyrisCBL.Pipeline.HumanReview;

/// <summary>
/// In-memory implementation of <see cref="ITicketingService"/>.
/// Suitable for development and demos. Replace with a real ticketing integration for production
/// (e.g., Zendesk API, Jira, Freshdesk).
/// </summary>
public sealed class InMemoryTicketingService : ITicketingService
{
    private readonly ConcurrentDictionary<string, SupportTicket> _tickets = new();
    private readonly ILogger<InMemoryTicketingService> _logger;

    public InMemoryTicketingService(ILogger<InMemoryTicketingService> logger)
        => _logger = logger;

    public Task CreateOrUpdateAsync(SupportTicket ticket, CancellationToken ct = default)
    {
        _tickets[ticket.TicketId] = ticket;
        _logger.LogInformation("[Ticketing] Ticket {Id} ({Status}) — {Email}",
            ticket.TicketId, ticket.Status, ticket.UserEmail ?? "anonymous");
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SupportTicket>> GetPendingAsync(CancellationToken ct = default)
    {
        var pending = _tickets.Values
            .Where(t => t.Status == TicketStatus.PendingHumanReview)
            .OrderBy(t => t.CreatedUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<SupportTicket>>(pending);
    }

    public Task ResolveAsync(string ticketId, string? resolution = null, CancellationToken ct = default)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            ticket.Status      = TicketStatus.Resolved;
            ticket.Resolution  = resolution;
            ticket.ResolvedUtc = DateTime.UtcNow;
            _logger.LogInformation("[Ticketing] Ticket {Id} resolved.", ticketId);
        }
        return Task.CompletedTask;
    }
}
