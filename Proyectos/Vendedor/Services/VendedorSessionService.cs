using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using NewRich.Application.Contracts.Auth;
using NewRich.Domain.Enums;
using NewRich.Vendedor.Constants;

namespace NewRich.Vendedor.Services;

public interface IVendedorSessionService
{
    Task SignInAsync(HttpContext httpContext, LoginResponse login);
    Task SignOutAsync(HttpContext httpContext);
}

public sealed class VendedorSessionService : IVendedorSessionService
{
    public async Task SignInAsync(HttpContext httpContext, LoginResponse login)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.UsuarioId.ToString()),
            new(ClaimTypes.Name, login.NombreUsuario),
            new(ClaimTypes.GivenName, login.NombreCompleto),
            new(ClaimTypes.Role, login.Rol.ToString()),
            new("debeCambiarPassword", login.DebeCambiarPassword ? "true" : "false")
        };
        var identity = new ClaimsIdentity(claims, AuthCookieNames.Scheme);
        await httpContext.SignInAsync(AuthCookieNames.Scheme, new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = login.FechaExpiracion.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(login.FechaExpiracion, DateTimeKind.Utc)
                : login.FechaExpiracion.ToUniversalTime()
        });
        httpContext.Response.Cookies.Append(AuthCookieNames.AccessToken, login.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = login.FechaExpiracion.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(login.FechaExpiracion, DateTimeKind.Utc)
                : login.FechaExpiracion.ToUniversalTime()
        });
    }

    public async Task SignOutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(AuthCookieNames.Scheme);
        httpContext.Response.Cookies.Delete(AuthCookieNames.AccessToken);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static bool DebeCambiarPassword(this ClaimsPrincipal user) =>
        string.Equals(user.FindFirstValue("debeCambiarPassword"), "true", StringComparison.OrdinalIgnoreCase);

    public static string NombreMostrado(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.GivenName) ?? user.Identity?.Name ?? string.Empty;

    public static string Iniciales(this ClaimsPrincipal user)
    {
        var parts = user.NombreMostrado().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "VE";
        }

        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
    }

    public static bool EsVendedor(this ClaimsPrincipal user) =>
        user.IsInRole(nameof(RolUsuario.Vendedor));
}
