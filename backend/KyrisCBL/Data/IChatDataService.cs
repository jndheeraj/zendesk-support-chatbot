namespace KyrisCBL.Data;

/// <summary>
/// Low-level data access interface for chat-related operations.
/// Implement with a real database or API integration as needed.
/// </summary>
public interface IChatDataService
{
    Task<string> GetUserProfileAsync(string userId);
    Task<bool>   LogUnsubscribeRequestAsync(string email);
    Task<bool>   ResetPasswordAsync(string email);
}
