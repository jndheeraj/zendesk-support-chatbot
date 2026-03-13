using KyrisCBL.Pipeline.HumanReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KyrisCBL.Controllers;

/// <summary>
/// Endpoints for human agents to view and resolve escalated support tickets.
/// Restrict access to internal/admin users in production.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize] // TODO: Replace with a role-based policy, e.g. [Authorize(Roles = "SupportAgent")]
public sealed class HumanReviewController : ControllerBase
{
    private readonly ITicketingService _ticketing;

    public HumanReviewController(ITicketingService ticketing)
        => _ticketing = ticketing;

    /// <summary>Returns all tickets pending human review.</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
        => Ok(await _ticketing.GetPendingAsync(ct));

    /// <summary>Resolves a ticket with an optional resolution note.</summary>
    [HttpPost("{ticketId}/resolve")]
    public async Task<IActionResult> Resolve(
        string ticketId,
        [FromBody] ResolveRequest? body,
        CancellationToken ct)
    {
        await _ticketing.ResolveAsync(ticketId, body?.Resolution, ct);
        return Ok(new { ticketId, status = "resolved" });
    }

    public record ResolveRequest(string? Resolution);
}
