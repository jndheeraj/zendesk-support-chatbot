namespace KyrisCBL.Models;

/// <summary>The response returned from the chatbot pipeline to the controller.</summary>
public sealed record BotReply(
    string Message,
    bool Solved,
    bool RequiresHumanReview = false,
    string? TicketId = null
);
