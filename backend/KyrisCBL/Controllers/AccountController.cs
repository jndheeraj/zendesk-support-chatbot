using KyrisCBL.Models;
using KyrisCBL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace KyrisCBL.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class AccountController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWorkflowService _workflow;

    public AccountController(IConfiguration config, IWorkflowService workflow)
    {
        _config   = config;
        _workflow = workflow;
    }

    private string? GetEmail() => User?.FindFirstValue(ClaimTypes.Email);
    private SqlConnection NewConn() => new SqlConnection(_config.GetConnectionString("Default"));

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var email = GetEmail();
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

        await using var conn = NewConn();
        await conn.OpenAsync();

        const string sql = @"
SELECT TOP (1)
    UserId, Email, FirstName, LastName,
    Address1, Address2, City, StateProvince, ZipCode,
    PhoneNumber, Gender, EthnicityId
FROM [user].[user]
WHERE Email = @Email AND IsDeleted = 0;";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Email", email);

        await using var r = await cmd.ExecuteReaderAsync();
        if (!r.Read()) return NotFound();

        string genderStr = "";
        if (r["Gender"] != DBNull.Value)
        {
            var g = r["Gender"].ToString();
            genderStr = g == "M" ? "Male" : g == "F" ? "Female" : "Other";
        }

        return Ok(new
        {
            firstName         = r["FirstName"] as string,
            lastName          = r["LastName"] as string,
            address1          = r["Address1"] as string,
            address2          = r["Address2"] as string,
            city              = r["City"] as string,
            stateProvince     = r["StateProvince"] as string ?? "",
            zipPostalCode     = r["ZipCode"] as string ?? "",
            phoneNumber       = r["PhoneNumber"] as string,
            gender            = genderStr,
            dob               = (string?)null,
            ethnicity         = r["EthnicityId"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["EthnicityId"]),
            preferredLanguage = (string?)null,
            email             = r["Email"] as string
        });
    }

    public record AccountUpdateProfileDto(
        string? FirstName,
        string? LastName,
        string? Address1,
        string? Address2,
        string? City,
        string? Gender,
        int? EthnicityId,
        string? PhoneNumber
    );

    [Authorize]
    [HttpPost("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] AccountUpdateProfileDto dto)
    {
        var email = GetEmail();
        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized(new { message = "Please sign in.", solved = false });

        char? g = null;
        if (!string.IsNullOrWhiteSpace(dto.Gender))
        {
            var t = dto.Gender.Trim().ToLowerInvariant();
            g = t.StartsWith("m") ? 'M' : t.StartsWith("f") ? 'F' : 'O';
        }

        var req = new UpdateProfileRequest(
            Email:       email,
            FirstName:   dto.FirstName,
            LastName:    dto.LastName,
            Address1:    dto.Address1,
            Address2:    dto.Address2,
            City:        dto.City,
            Gender:      g,
            EthnicityId: dto.EthnicityId,
            PhoneNumber: dto.PhoneNumber
        );

        var msg    = await _workflow.UpdateProfileAsync(req);
        var solved = msg.Contains("updated", StringComparison.OrdinalIgnoreCase);
        return Ok(new { message = msg, solved });
    }

    public record ChangePasswordDto(string CurrentPassword, string NewPassword);

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto body)
    {
        var email = GetEmail();
        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized(new { message = "Please sign in.", solved = false });

        if (string.IsNullOrWhiteSpace(body.CurrentPassword) || string.IsNullOrWhiteSpace(body.NewPassword))
            return BadRequest(new { message = "Both current and new passwords are required.", solved = false });

        await using var conn = NewConn();
        await conn.OpenAsync();

        // Fetch current stored password (plaintext for now; swap to hashing later)
        const string getSql = "SELECT TOP(1) PasswordHash FROM [user].[user] WHERE Email=@Email AND IsDeleted=0;";
        await using (var getCmd = new SqlCommand(getSql, conn))
        {
            getCmd.Parameters.AddWithValue("@Email", email);
            var stored = (string?)await getCmd.ExecuteScalarAsync();

            if (string.IsNullOrEmpty(stored) || !string.Equals(stored, body.CurrentPassword))
                return BadRequest(new { message = "Current password is incorrect.", solved = false });
        }

        // Update to new password
        const string updSql = @"
UPDATE [user].[user]
SET PasswordHash = @NewPassword, UpdateDt = GETDATE()
WHERE Email=@Email AND IsDeleted=0;";
        await using (var updCmd = new SqlCommand(updSql, conn))
        {
            updCmd.Parameters.AddWithValue("@Email",       email);
            updCmd.Parameters.AddWithValue("@NewPassword", body.NewPassword); // TODO: hash later
            var rows = await updCmd.ExecuteNonQueryAsync();
            if (rows == 0)
                return StatusCode(500, new { message = "Failed to update password.", solved = false });
        }

        return Ok(new { message = "Your password has been updated.", solved = true });
    }
}
