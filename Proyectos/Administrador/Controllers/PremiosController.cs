using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Premios;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class PremiosController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public PremiosController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, string? estado, int page = 1, int pageSize = 5, CancellationToken cancellationToken = default)
    {
        SetNav("premios", UiTexts.NavPremios);
        var listado = await _api.ListarCasosPremioAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(listado);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<CasoGanadorResponse> items = listado.Data ?? [];
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            items = items.Where(c =>
                    c.Ticket.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || c.Vendedor.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || c.Pda.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            items = items.Where(c => string.Equals(c.Estado, estado, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(new PremiosIndexViewModel
        {
            Busqueda = q,
            Estado = estado,
            Pagina = PagingHelper.Paginate(items, page, pageSize)
        });
    }

    [HttpGet]
    public IActionResult Reportar()
    {
        SetNav("premios", UiTexts.ReportarTicket);
        return View(new PremioReportarViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reportar(PremioReportarViewModel model, CancellationToken cancellationToken)
    {
        SetNav("premios", UiTexts.ReportarTicket);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.ConsultarTicketPremioAsync(new ConsultaTicketRequest { TicketCode = model.TicketCode }, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        var consulta = result.Data;
        model.ResultadoVisual = consulta.ResultadoVisual;
        model.MensajeEstado = consulta.Mensaje;
        model.Tono = consulta.Tono;
        model.PuedeIniciarCaso = consulta.PuedeIniciarCaso;
        model.BoletoId = consulta.BoletoId;
        if (consulta.Tirilla is not null && consulta.BoletoId.HasValue)
        {
            model.Tirilla = new TirillaViewModel
            {
                BoletoId = consulta.BoletoId.Value,
                Tirilla = consulta.Tirilla
            };
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarCaso(PremioReportarViewModel model, CancellationToken cancellationToken)
    {
        SetNav("premios", UiTexts.ReportarTicket);
        if (!ModelState.IsValid)
        {
            return View(nameof(Reportar), model);
        }

        var result = await _api.ReportarCasoPremioAsync(new ReportarCasoGanadorRequest { TicketCode = model.TicketCode }, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await Reportar(model, cancellationToken);
        }

        SetFlash(SuccessMessages.CasoGanadorReportado);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Ver(Guid id, CancellationToken cancellationToken)
    {
        SetNav("premios", UiTexts.DetalleCasoGanador);
        var casoTask = _api.ObtenerCasoPremioAsync(id, cancellationToken);
        var usuariosTask = _api.ListarUsuariosAsync(cancellationToken);
        await Task.WhenAll(casoTask, usuariosTask);
        var unauthorized = RedirectIfUnauthorized(casoTask.Result) ?? RedirectIfUnauthorized(usuariosTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!casoTask.Result.Success || casoTask.Result.Data is null)
        {
            SetFlash(casoTask.Result.Message, false);
            return RedirectToAction(nameof(Index));
        }

        var caso = casoTask.Result.Data;
        return View(new PremioVerViewModel
        {
            Caso = caso,
            ObservadorId = Guid.Empty,
            Observadores = (usuariosTask.Result.Data ?? [])
                .Where(u => u.Rol == RolUsuario.Observador && u.Estado == EstadoUsuario.Activo && !u.EstadoBloqueado)
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.ValidarCasoPremioAsync(id, cancellationToken);
        return ResultadoAccion(result, id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.RechazarCasoPremioAsync(id, cancellationToken);
        return ResultadoAccion(result, id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(Guid id, Guid observadorId, CancellationToken cancellationToken)
    {
        var result = await _api.AsignarObservadorPremioAsync(id, new AsignarObservadorRequest { ObservadorId = observadorId }, cancellationToken);
        return ResultadoAccion(result, id);
    }

    private IActionResult ResultadoAccion(ApiCallResult<CasoGanadorResponse> result, Guid id)
    {
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Message, result.Success);
        if (!result.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Ver), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Foto(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.DescargarFotoCasoPremioAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            return NotFound();
        }

        return File(result.Data.Contenido, result.Data.Tipo, result.Data.Nombre);
    }
}
