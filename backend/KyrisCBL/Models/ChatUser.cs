using Microsoft.AspNetCore.Identity;

namespace KyrisCBL.Models;

/// <summary>
/// ASP.NET Core Identity user for the chatbot application.
/// Extend this class to add custom profile fields.
/// </summary>
public sealed class ChatUser : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
}
