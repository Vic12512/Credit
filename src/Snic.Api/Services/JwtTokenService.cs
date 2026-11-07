using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Snic.Api.Domain;

namespace Snic.Api.Services;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public GeneratedToken Create(UserAccount account)
    {
        var key = configuration["Jwt:Key"]!;
        var issuer = configuration["Jwt:Issuer"] ?? "Snic.Api";
        var audience = configuration["Jwt:Audience"] ?? "Snic.Web";
        var minutes = configuration.GetValue("Jwt:ExpirationMinutes", 60);
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Role, account.Role.Code),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (account.InstitutionId is int institutionId)
            claims.Add(new Claim("institution_id", institutionId.ToString()));

        var token = new JwtSecurityToken(
            issuer, audience, claims, DateTime.UtcNow, expiresAt,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new GeneratedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
