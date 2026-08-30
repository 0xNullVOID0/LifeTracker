using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LifeTracker.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LifeTracker.Infrastructure;

public static class JwtTokenGenerator
{
    public static string Generate(JwtOptions jwt, string role = "Demo")
    {
        if (string.IsNullOrWhiteSpace(jwt.Key))
            throw new InvalidOperationException("JWT:Key missing");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "admin"), // TODO right this way?
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record TokenRequest(string Password);
