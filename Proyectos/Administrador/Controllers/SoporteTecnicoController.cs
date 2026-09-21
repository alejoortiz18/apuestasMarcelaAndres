using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Chat;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Controllers;

public sealed class SoporteTecnicoController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public SoporteTecnicoController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(Guid? id, CancellationToken cancellationToken)
    {
        SetNav("soporte-tecnico", UiTexts.NavSoporteTecnico);
        ViewData["Canal"] = "tecnico";
        var conversacionesTask = _api.ListarConversacionesAsync(cancellationToken, "SoporteTecnico");
        await conversacionesTask;
        var unauthorized = RedirectIfUnauthorized(conversacionesTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var lista = (conversacionesTask.Result.Data ?? []).ToList();

        ConversacionDetalleResponse? detalle = null;
        var selected = id ?? lista.FirstOrDefault()?.ConversacionId;
        if (selected.HasValue)
        {
            var det = await _api.ObtenerConversacionAsync(selected.Value, cancellationToken);
            unauthorized = RedirectIfUnauthorized(det);
            if (unauthorized is not null)
            {
                return unauthorized;
            }

            detalle = det.Data;
        }

        return View("~/Views/Soporte/Index.cshtml", new SoporteIndexViewModel
        {
            Conversaciones = lista,
            ConversacionId = selected,
            Detalle = detalle,
            Destinatarios = []
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> Enviar(Guid id, string? texto, IFormFile? archivo, CancellationToken cancellationToken)
    {
        var request = new EnviarMensajeRequest { Texto = texto?.Trim() ?? string.Empty };
        if (archivo is { Length: > 0 })
        {
            await using var buffer = new MemoryStream();
            await archivo.CopyToAsync(buffer, cancellationToken);
            request.NombreArchivo = archivo.FileName;
            request.ContenidoBase64 = Convert.ToBase64String(buffer.ToArray());
        }

        if (string.IsNullOrWhiteSpace(request.Texto) && string.IsNullOrWhiteSpace(request.NombreArchivo))
        {
            SetFlash(ChatMessages.TextoOAdjuntoRequerido, false);
            return RedirectToAction(nameof(Index), new { id });
        }

        var result = await _api.EnviarMensajeAsync(id, request, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            SetFlash(result.Message, false);
        }

        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Adjunto(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.DescargarAdjuntoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            SetFlash(result.Message, false);
            return RedirectToAction(nameof(Index));
        }

        return File(result.Data.Contenido, result.Data.Tipo, result.Data.Nombre);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.CerrarConversacionAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.ConversacionCerrada : result.Message, result.Success);
        return RedirectToAction(nameof(Index), new { id });
    }
}
