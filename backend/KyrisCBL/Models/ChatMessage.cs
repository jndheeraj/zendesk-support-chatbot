namespace KyrisCBL.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; }
        public string Sender { get; set; }
        public string? Email { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool? StartNewIssue { get; set; }
    }

    public record BotReply(string Message, bool Solved = false);

    public record PasswordResetResult(bool Exists, string Message);

}
