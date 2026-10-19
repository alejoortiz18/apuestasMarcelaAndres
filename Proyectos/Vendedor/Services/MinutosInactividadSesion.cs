using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NewRich.Shared.Helpers;
using NewRich.Vendedor.Constants;

namespace NewRich.Vendedor.Services;

public sealed class MinutosInactividadSesion
{
    public const string ItemKey = "MinutosInactividadSesion";
    private const string CacheKey = "minutos-inactividad-vendedor";
    private readonly IVendedorApiClient _api;
    private readonly IMemoryCache _cache;

    public MinutosInactividadSesion(IVendedorApiClient api, IMemoryCache cache)
    {
        _api = api;
        _cache = cache;
    }

    public static int De(HttpContext http) =>
        http.Items[ItemKey] is int minutos && minutos >= 1
            ? minutos
            : InactividadSesion.MinutosPorDefecto;

    public async Task<int> ActualesAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out int minutos) && minutos >= 1)
        {
            return minutos;
        }

        var result = await _api.OperativaAsync(cancellationToken);
        if (!result.Success || result.Data is null || result.Data.MinutosInactividadSesion < 1)
        {
            return InactividadSesion.MinutosPorDefecto;
        }

        minutos = Math.Min(result.Data.MinutosInactividadSesion, InactividadSesion.MinutosMaximo);
        _cache.Set(CacheKey, minutos, TimeSpan.FromSeconds(30));
        return minutos;
    }
}

public sealed class MinutosInactividadMiddleware
{
    private readonly RequestDelegate _next;

    public MinutosInactividadMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(
        HttpContext contexto,
        MinutosInactividadSesion minutos,
        IOptionsMonitor<CookieAuthenticationOptions> opciones)
    {
        var valor = InactividadSesion.MinutosPorDefecto;
        if (contexto.Request.Cookies.ContainsKey(AuthCookieNames.AccessToken))
        {
            valor = await minutos.ActualesAsync(contexto.RequestAborted);
        }

        contexto.Items[MinutosInactividadSesion.ItemKey] = valor;
        opciones.Get(AuthCookieNames.Scheme).ExpireTimeSpan = TimeSpan.FromMinutes(valor);
        await _next(contexto);
    }
}
