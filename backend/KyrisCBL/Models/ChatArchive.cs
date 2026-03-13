namespace KyrisCBL.Models
{
    public class ChatArchive
    {
        public long Id { get; set; }
        public string Tenant { get; set; } = "default";
        public string UserKey { get; set; } = "";          // email if known, else Session.Id or client SessionId
        public string? Email { get; set; }                  // optional, if known
        public string Channel { get; set; } = "web-widget";
        public string Summary { get; set; } = "";           // optional short summary
        public string Json { get; set; } = "";              // full transcript as JSON
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
