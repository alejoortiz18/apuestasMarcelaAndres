using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NewRich.Admin.Constants;
using NewRich.Application.Contracts.Auth;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Services;

public interface IAdminSessionService
{
    Task SignInAsync(HttpContext httpContext, LoginResponse login);
    Task SignOutAsync(HttpContext httpContext);
}

public sealed class AdminSessionService : IAdminSessionService
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
        var principal = new ClaimsPrincipal(identity);
        await httpContext.SignInAsync(AuthCookieNames.Scheme, principal, new AuthenticationProperties
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

    public static string NombreUsuario(this ClaimsPrincipal user) =>
        user.Identity?.Name ?? string.Empty;

    public static string RolMostrado(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public static string Iniciales(this ClaimsPrincipal user)
    {
        var name = user.NombreMostrado();
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "AD";
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
    }

    public static bool EsAdministrador(this ClaimsPrincipal user) =>
        user.IsInRole(nameof(RolUsuario.Administrador));
}
