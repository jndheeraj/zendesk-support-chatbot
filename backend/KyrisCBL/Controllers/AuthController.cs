using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using KyrisCBL.Models;

namespace KyrisCBL.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly UserManager<ChatUser> _userManager;
    private readonly SignInManager<ChatUser> _signInManager;

    public AuthController(
        IConfiguration config,
        UserManager<ChatUser> userManager,
        SignInManager<ChatUser> signInManager)
    {
        _config        = config;
        _userManager   = userManager;
        _signInManager = signInManager;
    }

    public record LoginRequest(string Email, string Password);
    public record CreateAccountDto(string Email, string Password, string? FirstName, string? LastName);

    // ── Login ────────────────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required." });

        await using var conn = new SqlConnection(_config.GetConnectionString("Default"));
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(@"
            SELECT UserId, Email, FirstName, LastName, PasswordHash
            FROM [user].[user]
            WHERE Email = @Email AND IsDeleted = 0", conn);

        cmd.Parameters.AddWithValue("@Email", req.Email.Trim());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.Read()) return Unauthorized(new { message = "Invalid email or password." });

        var stored = reader["PasswordHash"]?.ToString();

        // TODO: Replace plaintext comparison with a proper password hasher (e.g. BCrypt or ASP.NET Identity PasswordHasher)
        if (string.IsNullOrEmpty(stored) || stored != req.Password)
            return Unauthorized(new { message = "Invalid email or password." });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, reader["UserId"].ToString()!),
            new(ClaimTypes.Name,           reader["FirstName"]?.ToString() ?? ""),
            new(ClaimTypes.Email,          reader["Email"].ToString()!)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        return Ok(new
        {
            success   = true,
            id        = reader["UserId"],
            email     = reader["Email"],
            firstName = reader["FirstName"]
        });
    }

    // ── Logout ───────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { status = "Logged out." });
    }

    // ── Me ───────────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id    = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email),
        name  = User.FindFirstValue(ClaimTypes.Name)
    });

    // ── Create Account ───────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateAccountDto body)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            return BadRequest(new { message = "Email and password are required." });

        var email = body.Email.Trim();

        await using var conn = new SqlConnection(_config.GetConnectionString("Default"));
        await conn.OpenAsync();

        await using (var existsCmd = new SqlCommand(
            "SELECT 1 FROM [user].[user] WHERE Email = @Email AND IsDeleted = 0;", conn))
        {
            existsCmd.Parameters.AddWithValue("@Email", email);
            if (await existsCmd.ExecuteScalarAsync() is not null)
                return Conflict(new { message = "An account with that email already exists." });
        }

        await using var insertCmd = new SqlCommand(@"
            INSERT INTO [user].[user] (UserGuid, Email, PasswordHash, FirstName, LastName, ZipCode)
            OUTPUT INSERTED.UserId
            VALUES (NEWID(), @Email, @PasswordHash, @FirstName, @LastName, @ZipCode);", conn);

        // TODO: Hash the password before storing. Example: new PasswordHasher<object>().HashPassword(null!, body.Password)
        insertCmd.Parameters.AddWithValue("@Email",        email);
        insertCmd.Parameters.AddWithValue("@PasswordHash", body.Password);
        insertCmd.Parameters.AddWithValue("@FirstName",    (object?)body.FirstName ?? DBNull.Value);
        insertCmd.Parameters.AddWithValue("@LastName",     (object?)body.LastName  ?? DBNull.Value);
        insertCmd.Parameters.AddWithValue("@ZipCode",      "");

        var userIdObj = await insertCmd.ExecuteScalarAsync();
        if (userIdObj is null) return StatusCode(500, new { message = "Failed to create account." });

        var userId = userIdObj.ToString()!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email,          email),
            new(ClaimTypes.Name,           (body.FirstName ?? "").Trim())
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        return Ok(new { message = "Account created.", id = userId, email, firstName = body.FirstName });
    }
}
