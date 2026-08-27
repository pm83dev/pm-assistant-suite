using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace OreTracking.Api.Controllers;

public class AdminUserSettings
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, int ExpiresIn);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AdminUserSettings _adminUser;

    public AuthController(IConfiguration configuration, IOptions<AdminUserSettings> adminUser)
    {
        _configuration = configuration;
        _adminUser = adminUser.Value;
    }

    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        if (request.Username != _adminUser.Username || request.Password != _adminUser.Password)
        {
            return Unauthorized(new { message = "Credenziali non valide." });
        }

        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key non configurata.");
        const int expiresInMinutes = 480;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenString, expiresInMinutes * 60));
    }

    [HttpPost("refresh")]
    public ActionResult<LoginResponse> Refresh()
    {
        var currentToken = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        
        if (string.IsNullOrEmpty(currentToken))
        {
            return Unauthorized(new { message = "Token non fornito." });
        }

        try
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key non configurata.");
            const int expiresInMinutes = 480;

            // Validazione del token attuale
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };

            var principal = tokenHandler.ValidateToken(currentToken, validationParameters, out _);
            
            if (!principal.Claims.Any(c => c.Type == ClaimTypes.Name))
            {
                return Unauthorized(new { message = "Token non valido." });
            }

            var username = principal.Claims.First(c => c.Type == ClaimTypes.Name).Value;

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: credentials);

            var newTokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new LoginResponse(newTokenString, expiresInMinutes * 60));
        }
        catch
        {
            return Unauthorized(new { message = "Token non valido o scaduto." });
        }
    }
}
