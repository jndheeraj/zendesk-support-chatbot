using KyrisCBL.Models;

namespace KyrisCBL.Services;

/// <summary>
/// Handles concrete user workflow operations that the chatbot can execute directly
/// (password reset, unsubscribe, profile updates, etc.).
/// </summary>
public interface IWorkflowService
{
    Task<string>              ResetPasswordAsync(string email);
    Task<PasswordResetResult> ResetPasswordCheckedAsync(string email);
    Task<string>              UnsubscribeAsync(string email, bool isAuthenticated);
    Task<string>              DoNotSellAsync(string email, bool isAuthenticated);
    Task<string>              UpdateProfileAsync(UpdateProfileRequest request);
    Task<string>              CreateSupportTicketAsync(string userMessage, string? email = null);
}
