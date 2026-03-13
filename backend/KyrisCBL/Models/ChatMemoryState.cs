namespace KyrisCBL.Models
{
    public class ChatMemoryState
    {
        public int ClarifyAttempts { get; set; } = 0;   
        public string? LastIntent { get; set; }
        public string? ActiveTicketId { get; set; }
        public bool StartNewTicketOnNextMessage { get; set; }
    }
}
