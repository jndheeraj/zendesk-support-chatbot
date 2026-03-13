using KyrisCBL.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KyrisCBL.Services;

/// <summary>
/// Executes concrete user workflow operations against the database.
/// Wire in a real email sender by replacing the <see cref="IEmailSender"/> registration in DI.
/// </summary>
public sealed class WorkflowService : IWorkflowService
{
    private readonly string _connectionString;
    private readonly IEmailSender _emailSender;

    public WorkflowService(IConfiguration config, IEmailSender emailSender)
    {
        _connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
        _emailSender = emailSender;
    }

    public async Task<string> UnsubscribeAsync(string email, bool isAuthenticated)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Please provide your email to unsubscribe.";

        if (isAuthenticated)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                UPDATE [user].[user]
                SET IsGdprCompliance = 1, UpdateDt = GETUTCDATE()
                WHERE Email = @Email AND IsDeleted = 0", conn);

            cmd.Parameters.AddWithValue("@Email", email);
            return await cmd.ExecuteNonQueryAsync() > 0
                ? "You have been unsubscribed."
                : "We couldn't find an active account with that email.";
        }

        await _emailSender.SendEmailAsync(
            email,
            "Unsubscribe Request",
            $"We received a request to unsubscribe <b>{email}</b> from communications.<br><br>" +
            "If this wasn't you, please ignore this message.");

        await Task.Delay(TimeSpan.FromSeconds(2));
        return "We've sent an unsubscribe link to your email. Please check your inbox.";
    }

    public async Task<string> DoNotSellAsync(string email, bool isAuthenticated)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Please provide your email to submit a Do Not Sell request.";

        if (isAuthenticated)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                UPDATE [user].[user]
                SET IsCcpaCompliance = 1, UpdateDt = GETUTCDATE()
                WHERE Email = @Email AND IsDeleted = 0", conn);

            cmd.Parameters.AddWithValue("@Email", email);
            return await cmd.ExecuteNonQueryAsync() > 0
                ? "Your 'Do Not Sell' request has been recorded."
                : "We couldn't find an active account with that email.";
        }

        await _emailSender.SendEmailAsync(
            email,
            "Do Not Sell Request",
            $"We received a Do Not Sell request for <b>{email}</b>.<br><br>" +
            "If this wasn't you, please ignore this message.");

        await Task.Delay(TimeSpan.FromSeconds(2));
        return "We've sent a Do-Not-Sell confirmation to your email. Please check your inbox.";
    }

    public async Task<string> UpdateProfileAsync(UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return "Please provide your email to update your profile.";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand { Connection = conn };
        cmd.Parameters.AddWithValue("@Email", request.Email);

        var sets = new List<string>();

        void Add(string column, string param, object? value)
        {
            if (value is null) return;
            sets.Add($"{column} = @{param}");
            cmd.Parameters.AddWithValue($"@{param}", value);
        }

        Add("FirstName",   "FirstName",   string.IsNullOrWhiteSpace(request.FirstName)   ? null : request.FirstName);
        Add("LastName",    "LastName",    string.IsNullOrWhiteSpace(request.LastName)    ? null : request.LastName);
        Add("Address1",    "Address1",    string.IsNullOrWhiteSpace(request.Address1)    ? null : request.Address1);
        Add("Address2",    "Address2",    string.IsNullOrWhiteSpace(request.Address2)    ? null : request.Address2);
        Add("City",        "City",        string.IsNullOrWhiteSpace(request.City)        ? null : request.City);
        Add("PhoneNumber", "PhoneNumber", string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber);

        if (request.Gender.HasValue)
        {
            sets.Add("Gender = @Gender");
            cmd.Parameters.Add("@Gender", SqlDbType.Char, 1).Value = char.ToUpperInvariant(request.Gender.Value);
        }

        if (request.EthnicityId.HasValue)
            Add("EthnicityId", "EthnicityId", request.EthnicityId.Value);

        if (sets.Count == 0)
            return "Tell me what to change (e.g., \"first: Alice, last: Lee\").";

        cmd.CommandText = $"UPDATE [user].[user] SET {string.Join(", ", sets)}, UpdateDt = GETUTCDATE() WHERE Email = @Email AND IsDeleted = 0;";

        return await cmd.ExecuteNonQueryAsync() > 0
            ? "Your profile has been updated!"
            : "We couldn't find an active account with that email.";
    }

    public async Task<PasswordResetResult> ResetPasswordCheckedAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new PasswordResetResult(false, "Email is required.");

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(
            "SELECT 1 FROM [User].[User] WHERE Email = @Email AND IsDeleted = 0;", conn);
        cmd.Parameters.AddWithValue("@Email", email);

        if (await cmd.ExecuteScalarAsync() is null)
            return new PasswordResetResult(false, "We couldn't find an account with that email.");

        var token    = Guid.NewGuid().ToString("N")[..8];
        var resetUrl = GenerateResetLink(email, token);

        await _emailSender.SendEmailAsync(
            email,
            "Reset your password",
            $"Please reset your password: <a href=\"{resetUrl}\">Reset Link</a>");

        await Task.Delay(TimeSpan.FromSeconds(2));
        return new PasswordResetResult(true, "We've sent a password reset link to your email.");
    }

    public async Task<string> ResetPasswordAsync(string email)
        => (await ResetPasswordCheckedAsync(email)).Message;

    public Task<string> CreateSupportTicketAsync(string userMessage, string? email = null)
    {
        var ticketId = $"SUP-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        return Task.FromResult(ticketId);
    }

    private static string GenerateResetLink(string email, string token) =>
        // TODO: Replace your-domain.com with your actual domain
        $"https://your-domain.com/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
}
