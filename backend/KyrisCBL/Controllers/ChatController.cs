using KyrisCBL.Helpers;
using KyrisCBL.Models;
using KyrisCBL.Pipeline.Core;
using KyrisCBL.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace KyrisCBL.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class ChatController : ControllerBase
{
    private readonly AgentPipeline _pipeline;
    private readonly IWorkflowService _workflow;
    private readonly AppDbContext _db;
    private readonly ILogger<ChatController> _logger;

    private const string SessionMemoryKey  = "ChatMemory";
    private const string SessionHistoryKey = "ChatHistory";

    public ChatController(
        AgentPipeline pipeline,
        IWorkflowService workflow,
        AppDbContext db,
        ILogger<ChatController> logger)
    {
        _pipeline = pipeline;
        _workflow  = workflow;
        _db        = db;
        _logger    = logger;
    }

    // ── POST /api/chat ───────────────────────────────────────────────────────

    [HttpPost("")]
    public async Task<IActionResult> PostMessage(
        [FromBody] ChatMessage userMessage,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userMessage?.Text))
            return BadRequest(new { message = "Text field is required." });

        // Handle "start new issue" signal from the frontend
        if (userMessage.StartNewIssue == true)
        {
            var mem = LoadMemory();
            mem.StartNewTicketOnNextMessage = true;
            SaveMemory(mem);
            return Ok(new { text = "", sender = "bot", timestamp = DateTime.UtcNow, solved = false });
        }

        // Resolve auth and email
        var authResult      = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var isAuthenticated = authResult.Succeeded && authResult.Principal is not null;
        var email           = userMessage.Email;

        if (isAuthenticated)
        {
            HttpContext.User = authResult.Principal!;
            email ??= authResult.Principal!.FindFirstValue(ClaimTypes.Email);
        }

        var memory = LoadMemory();

        if (memory.StartNewTicketOnNextMessage)
        {
            memory.ActiveTicketId              = null;
            memory.StartNewTicketOnNextMessage = false;
            memory.ClarifyAttempts             = 0;
        }

        try
        {
            var context = new AgentContext
            {
                UserMessage     = userMessage.Text,
                UserEmail       = email,
                IsAuthenticated = isAuthenticated,
                Memory          = memory
            };

            await _pipeline.RunAsync(context, ct);
            SaveMemory(context.Memory);

            AppendToHistory(new ChatMessage
            {
                Text      = userMessage.Text,
                Sender    = "user",
                Timestamp = DateTime.UtcNow,
                Email     = email
            });
            AppendToHistory(new ChatMessage
            {
                Text      = context.FinalResponse,
                Sender    = "bot",
                Timestamp = DateTime.UtcNow
            });

            return Ok(new
            {
                text                = context.FinalResponse,
                sender              = "bot",
                timestamp           = DateTime.UtcNow,
                solved              = context.Solved,
                requiresHumanReview = context.RequiresHumanReview,
                ticketId            = context.TicketId
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Request timed out." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in ChatController.");
            return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }

    // ── Workflow endpoints (direct HTTP calls from the frontend) ─────────────

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] EmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Email))
            return BadRequest(new { message = "Email is required.", solved = false });

        var result = await _workflow.ResetPasswordCheckedAsync(dto.Email.Trim());
        return result.Exists
            ? Ok(new { message = result.Message, solved = true })
            : NotFound(new { message = result.Message, solved = false });
    }

    [AllowAnonymous]
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] EmailDto? dto)
    {
        var (email, isAuth) = await ResolveEmailAndAuth(dto?.Email);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required.", solved = false });

        return Ok(new { message = await _workflow.UnsubscribeAsync(email, isAuth), solved = true });
    }

    [AllowAnonymous]
    [HttpPost("do-not-sell")]
    public async Task<IActionResult> DoNotSell([FromBody] EmailDto? dto)
    {
        var (email, isAuth) = await ResolveEmailAndAuth(dto?.Email);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required.", solved = false });

        return Ok(new { message = await _workflow.DoNotSellAsync(email, isAuth), solved = true });
    }

    [Authorize]
    [HttpPost("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest dto)
    {
        if (dto is null) return BadRequest(new { message = "Invalid payload." });
        var email = dto.Email ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized(new { message = "Please sign in to update your profile." });

        var req = dto with { Email = email };
        var msg = await _workflow.UpdateProfileAsync(req);
        return Ok(new { message = msg, solved = msg.Contains("updated", StringComparison.OrdinalIgnoreCase) });
    }

    // ── Chat history ─────────────────────────────────────────────────────────

    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        var history = HttpContext.Session.GetObject<List<ChatMessage>>(SessionHistoryKey)
                      ?? new List<ChatMessage>
                      {
                          new() { Text = "Hello! I'm your support assistant. How can I help you today?", Sender = "bot", Timestamp = DateTime.UtcNow }
                      };
        return Ok(history);
    }

    // ── End chat / archive ───────────────────────────────────────────────────

    [HttpPost("end")]
    public async Task<IActionResult> EndChat([FromBody] EndChatRequest? body, CancellationToken ct)
    {
        var email      = body?.Email ?? User.FindFirstValue(ClaimTypes.Email);
        var transcript = HttpContext.Session.GetObject<List<ChatMessage>>(SessionHistoryKey) ?? new();

        if (transcript.Count == 0)
            return Ok(new { ok = true, message = "Nothing to archive." });

        _db.ChatArchives.Add(new ChatArchive
        {
            Tenant     = "default",
            UserKey    = email ?? HttpContext.Session.Id,
            Email      = email,
            Channel    = "web-widget",
            Summary    = body?.Summary ?? string.Empty,
            Json       = JsonSerializer.Serialize(transcript),
            CreatedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        HttpContext.Session.Remove(SessionHistoryKey);

        return Ok(new { ok = true });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public record EmailDto(string? Email);
    public record EndChatRequest(string? Email, string? Summary);

    private ChatMemoryState LoadMemory()
        => HttpContext.Session.GetObject<ChatMemoryState>(SessionMemoryKey) ?? new();

    private void SaveMemory(ChatMemoryState m)
        => HttpContext.Session.SetObject(SessionMemoryKey, m);

    private void AppendToHistory(ChatMessage msg)
    {
        var history = HttpContext.Session.GetObject<List<ChatMessage>>(SessionHistoryKey) ?? new();
        history.Add(msg);
        HttpContext.Session.SetObject(SessionHistoryKey, history);
    }

    private async Task<(string? email, bool isAuth)> ResolveEmailAndAuth(string? bodyEmail)
    {
        var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var isAuth     = authResult.Succeeded && authResult.Principal is not null;
        var email      = bodyEmail?.Trim();
        if (isAuth)
        {
            HttpContext.User = authResult.Principal!;
            email ??= authResult.Principal!.FindFirstValue(ClaimTypes.Email);
        }
        return (email, isAuth);
    }
}
