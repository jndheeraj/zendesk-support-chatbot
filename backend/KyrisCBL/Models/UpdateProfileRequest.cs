namespace KyrisCBL.Models
{
    public record UpdateProfileRequest(
        string Email,
        string? FirstName = null,
        string? LastName = null,
        string? Address1 = null,
        string? Address2 = null,
        string? City = null,
        char? Gender = null,
        int? EthnicityId = null,
        string? PhoneNumber = null
    );
}
