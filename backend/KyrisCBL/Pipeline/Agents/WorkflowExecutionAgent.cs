using KyrisCBL.Models;
using KyrisCBL.Pipeline.Core;
using KyrisCBL.Pipeline.HumanReview;
using KyrisCBL.Services;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace KyrisCBL.Pipeline.Agents;

/// <summary>
/// Level-4 agent: executes concrete user workflow actions (password reset, unsubscribe, etc.)
/// and handles escalation by creating a support ticket via <see cref="ITicketingService"/>.
/// This is the HITL gate — when escalation is required, a human is brought in.
/// </summary>
public sealed class WorkflowExecutionAgent : IAgent
{
    public string Name => "WorkflowExecution";

    private readonly IWorkflowService _workflow;
    private readonly ITicketingService _ticketing;
    private readonly ILogger<WorkflowExecutionAgent> _logger;

    public WorkflowExecutionAgent(
        IWorkflowService workflow,
        ITicketingService ticketing,
        ILogger<WorkflowExecutionAgent> logger)
    {
        _workflow  = workflow;
        _ticketing = ticketing;
        _logger    = logger;
    }

    public async Task ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var intent = (context.DetectedIntent ?? "other").Trim().ToLowerInvariant();
        var email  = context.UserEmail;
        var isAuth = context.IsAuthenticated;

        // ── 1. Workflow intents ───────────────────────────────────────────────
        switch (intent)
        {
            case "reset_password":
                if (string.IsNullOrWhiteSpace(email))
                {
                    context.FinalResponse = "Please provide your email to send the password reset link.";
                    context.Solved = false;
                    return;
                }
                var resetResult = await _workflow.ResetPasswordCheckedAsync(email);
                context.FinalResponse = resetResult.Message;
                context.Solved = resetResult.Exists;
                context.WorkflowExecuted = true;
                return;

            case "unsubscribe":
                if (string.IsNullOrWhiteSpace(email))
                {
                    context.FinalResponse = "Please provide your email to unsubscribe.";
                    context.Solved = false;
                    return;
                }
                context.FinalResponse = await _workflow.UnsubscribeAsync(email, isAuth);
                context.Solved = true;
                context.WorkflowExecuted = true;
                return;

            case "do_not_sell":
                if (string.IsNullOrWhiteSpace(email))
                {
                    context.FinalResponse = "Please provide your email to submit a Do Not Sell request.";
                    context.Solved = false;
                    return;
                }
                context.FinalResponse = await _workflow.DoNotSellAsync(email, isAuth);
                context.Solved = true;
                context.WorkflowExecuted = true;
                return;

            case "update_profile":
                if (string.IsNullOrWhiteSpace(email))
                {
                    context.FinalResponse = "Please sign in or provide your email to update your profile.";
                    context.Solved = false;
                    return;
                }
                var dto = ParseProfileRequest(context.UserMessage, email);
                context.FinalResponse = await _workflow.UpdateProfileAsync(dto);
                context.Solved = context.FinalResponse.Contains("updated", StringComparison.OrdinalIgnoreCase);
                context.WorkflowExecuted = true;
                return;
        }

        // ── 2. Escalation → HITL ────────────────────────────────────────────
        if (context.EscalationRequested)
        {
            if (string.IsNullOrWhiteSpace(email) && !isAuth)
            {
                context.FinalResponse = "To create a support ticket, please sign in or share your email address.";
                context.Solved = false;
                context.RequiresHumanReview = false;
                return;
            }

            var ticketId = context.Memory.ActiveTicketId
                           ?? await _workflow.CreateSupportTicketAsync(context.UserMessage, email);

            context.Memory.ActiveTicketId = ticketId;
            context.TicketId              = ticketId;
            context.RequiresHumanReview   = true;
            context.Solved                = true;

            // Notify the ticketing system (HITL gate)
            await _ticketing.CreateOrUpdateAsync(new SupportTicket
            {
                TicketId    = ticketId,
                UserEmail   = email,
                UserMessage = context.UserMessage,
                CreatedUtc  = DateTime.UtcNow,
                Status      = TicketStatus.PendingHumanReview
            });

            context.FinalResponse = context.FinalResponse.Contains("[[TICKET_ID]]")
                ? context.FinalResponse.Replace("[[TICKET_ID]]", ticketId)
                : $"I've created a support ticket for you (#{ticketId}). Our team will follow up shortly.";

            _logger.LogWarning("[HITL] Ticket {TicketId} created for user '{Email}'.", ticketId, email ?? "anonymous");
        }
    }

    private static UpdateProfileRequest ParseProfileRequest(string msg, string email)
    {
        static string? Field(string text, string key)
        {
            var i = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            return text[(i + key.Length)..].Split(',', ';')[0].Trim();
        }

        char? gender = null;
        if (msg.Contains("gender:", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var token in msg.Split(' ', ':'))
            {
                if (token is "M" or "m") { gender = 'M'; break; }
                if (token is "F" or "f") { gender = 'F'; break; }
                if (token is "O" or "o") { gender = 'O'; break; }
            }
        }

        int? ethnicity = null;
        if (msg.Contains("hispanic", StringComparison.OrdinalIgnoreCase)) ethnicity = 1;
        else if (msg.Contains("asian", StringComparison.OrdinalIgnoreCase)) ethnicity = 2;
        else if (msg.Contains("black", StringComparison.OrdinalIgnoreCase)) ethnicity = 3;

        var phoneMatch = Regex.Match(msg, @"\b\d{7,15}\b");

        return new UpdateProfileRequest(
            Email:       email,
            FirstName:   Field(msg, "first:") ?? Field(msg, "firstname:"),
            LastName:    Field(msg, "last:")  ?? Field(msg, "lastname:"),
            Address1:    Field(msg, "address:"),
            Address2:    null,
            City:        Field(msg, "city:"),
            Gender:      gender,
            EthnicityId: ethnicity,
            PhoneNumber: phoneMatch.Success ? phoneMatch.Value : null
        );
    }
}
