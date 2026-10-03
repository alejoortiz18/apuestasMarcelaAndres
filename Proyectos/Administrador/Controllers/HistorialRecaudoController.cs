using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class HistorialRecaudoController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public HistorialRecaudoController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        string? desde,
        string? hasta,
        Guid? recaudadorId,
        Guid? grupoId,
        Guid? vendedorId,
        string? estado,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        SetNav("recaudo-historial", UiTexts.NavHistorialRecaudo);
        var inicio = RecaudoFechas.Leer(desde, RecaudoFechas.Hoy());
        var fin = RecaudoFechas.Leer(hasta, inicio);
        var historial = await _api.HistorialRecaudoAsync(new FiltroHistorialRecaudo
        {
            Desde = inicio,
            Hasta = fin,
            RecaudadorId = recaudadorId,
            GrupoId = grupoId,
            VendedorId = vendedorId,
            Estado = string.IsNullOrWhiteSpace(estado) ? null : estado
        }, cancellationToken);
        var usuarios = await _api.ListarUsuariosAsync(cancellationToken);
        var grupos = await _api.ListarGruposAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(historial) ?? RedirectIfUnauthorized(usuarios) ?? RedirectIfUnauthorized(grupos);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!historial.Success)
        {
            SetFlash(historial.Message, false);
        }

        var lista = usuarios.Data ?? [];
        var filas = (historial.Data ?? []).Select(m => new FilaHistorialRecaudo
        {
            FechaHora = m.FechaHora,
            Recaudador = m.Recaudador,
            Vendedor = m.Vendedor,
            Grupo = m.Grupo,
            ValorRecibido = m.ValorRecibido,
            SaldoResultante = m.SaldoResultante,
            Estado = m.Estado
        }).ToList();

        return View(new HistorialRecaudoViewModel
        {
            Desde = inicio,
            Hasta = fin,
            RecaudadorId = recaudadorId,
            GrupoId = grupoId,
            VendedorId = vendedorId,
            Estado = estado,
            Recaudadores = lista.Where(u => u.Rol == RolUsuario.Recaudador).OrderBy(u => u.NombreCompleto)
                .Select(u => new OpcionRecaudo { Id = u.UsuarioId, Nombre = u.NombreCompleto }).ToList(),
            Vendedores = lista.Where(u => u.Rol == RolUsuario.Vendedor).OrderBy(u => u.NombreCompleto)
                .Select(u => new OpcionRecaudo { Id = u.UsuarioId, Nombre = u.NombreCompleto }).ToList(),
            Grupos = (grupos.Data ?? []).OrderBy(g => g.Nombre)
                .Select(g => new OpcionRecaudo { Id = g.GrupoId, Nombre = g.Nombre }).ToList(),
            Pagina = PagingHelper.Paginate(filas, page, pageSize)
        });
    }
}
