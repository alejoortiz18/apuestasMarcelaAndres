using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Chat;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class SoporteController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public SoporteController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(Guid? id, string? q, CancellationToken cancellationToken)
    {
        SetNav("soporte", UiTexts.NavSoporte);
        var conversacionesTask = _api.ListarConversacionesAsync(cancellationToken);
        var usuariosTask = _api.ListarUsuariosAsync(cancellationToken);
        await Task.WhenAll(conversacionesTask, usuariosTask);
        var unauthorized = RedirectIfUnauthorized(conversacionesTask.Result) ?? RedirectIfUnauthorized(usuariosTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var lista = (conversacionesTask.Result.Data ?? []).ToList();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var termino = q.Trim();
            lista = lista.Where(c =>
                c.NombreIniciador.Contains(termino, StringComparison.OrdinalIgnoreCase)
                || c.NombreDestino.Contains(termino, StringComparison.OrdinalIgnoreCase)
                || c.UltimoTexto.Contains(termino, StringComparison.OrdinalIgnoreCase)).ToList();
        }
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

        var destinatarios = (usuariosTask.Result.Data ?? [])
            .Where(u => u.Rol is RolUsuario.Vendedor or RolUsuario.Observador)
            .ToList();

        return View(new SoporteIndexViewModel
        {
            Conversaciones = lista,
            ConversacionId = selected,
            Detalle = detalle,
            Destinatarios = destinatarios,
            Busqueda = q
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
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> Iniciar(Guid destinoId, string? texto, IFormFile? archivo, CancellationToken cancellationToken)
    {
        var request = new IniciarChatRequest
        {
            DestinoId = destinoId,
            Texto = texto?.Trim() ?? string.Empty
        };
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
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.IniciarConversacionAsync(request, cancellationToken);

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

        SetFlash(SuccessMessages.ConversacionIniciada);
        return RedirectToAction(nameof(Index), new { id = result.Data.ConversacionId });
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
