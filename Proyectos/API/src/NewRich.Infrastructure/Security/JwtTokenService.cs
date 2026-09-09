using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NewRich.Application.Abstractions;

namespace NewRich.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(JwtUser user, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UsuarioId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
            new(ClaimTypes.Name, user.NombreUsuario),
            new(ClaimTypes.Role, user.Rol.ToString()),
            new("sesionId", user.SesionId.ToString()),
            new("debeCambiarPassword", user.DebeCambiarPassword.ToString())
        };

        if (user.DispositivoId.HasValue)
        {
            claims.Add(new Claim("dispositivoId", user.DispositivoId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
