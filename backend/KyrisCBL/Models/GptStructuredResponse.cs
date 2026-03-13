namespace KyrisCBL.Models
{
    public class GptStructuredResponse
    {
        public string Query { get; set; } = string.Empty;
        public string Intent { get; set; } = "other";
        public bool Escalation { get; set; } = false;
        public string Response { get; set; } = string.Empty;
    }
}
