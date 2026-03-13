namespace KyrisCBL.Data;

/// <summary>
/// Default implementation of <see cref="IChatDataService"/>.
/// Replace the stub bodies with real database calls when connecting to a persistent store.
/// </summary>
public class ChatDataService : IChatDataService
{
    public Task<string> GetUserProfileAsync(string userId)
    {
        // TODO: Replace with a real database query
        return Task.FromResult($"[Profile for user {userId}]");
    }

    public Task<bool> LogUnsubscribeRequestAsync(string email)
    {
        // TODO: Replace with a real database insert/update
        return Task.FromResult(true);
    }

    public Task<bool> ResetPasswordAsync(string email)
    {
        // TODO: Replace with a real password-reset flow
        return Task.FromResult(true);
    }
}
