using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lex.Modules.Auth.Application.Contracts;
using Lex.Modules.Auth.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lex.Modules.Auth.Infrastructure.Security;

internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generate(User user)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new ArgumentNullException("Jwt:Secret is missing");
        var issuer = _configuration["Jwt:Issuer"] ?? "LexAI";
        var audience = _configuration["Jwt:Audience"] ?? "LexAI_Clients";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
