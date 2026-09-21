using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Usb;
using NewRich.Application.Contracts.Llaves;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;

namespace NewRich.Admin.Controllers;

public sealed class InstalacionesController : AdminControllerBase
{
    private readonly IAdminApiClient _api;
    private readonly IInventarioUsb _inventario;
    private readonly IPreparadorLlaveUsb _preparador;

    public InstalacionesController(IAdminApiClient api, IInventarioUsb inventario, IPreparadorLlaveUsb preparador)
    {
        _api = api;
        _inventario = inventario;
        _preparador = preparador;
    }

    public IActionResult Index()
    {
        SetNav("instalaciones", UiTexts.NavInstalaciones);
        return View();
    }

    public IActionResult Instalar()
    {
        return RedirectToAction("Crear", "Dispositivos", new { desde = "instalaciones" });
    }

    [HttpGet]
    public async Task<IActionResult> Llave(CancellationToken cancellationToken)
    {
        SetNav("instalaciones", UiTexts.LlaveAdminTitulo);
        var modelo = await ArmarModeloAsync(new LlaveUsbViewModel(), cancellationToken);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Llave(LlaveUsbViewModel modelo, CancellationToken cancellationToken)
    {
        SetNav("instalaciones", UiTexts.LlaveAdminTitulo);
        modelo = await ArmarModeloAsync(modelo, cancellationToken);
        if (modelo.UsuarioId == Guid.Empty)
        {
            modelo.Error = UiTexts.LlaveAdminUsuarioRequerido;
            return View(modelo);
        }

        var seleccion = SeleccionDiscoUsb.Elegir(modelo.Discos, modelo.LetraUsb, out var disco);
        if (seleccion != ResultadoSeleccionUsb.Ok || disco is null)
        {
            modelo.Error = InventarioUsbMensajes.De(seleccion);
            return View(modelo);
        }

        if (!modelo.ConfirmarBorrado)
        {
            modelo.Error = LlaveMessages.AdvertenciaBorrado;
            modelo.PideConfirmacionBorrado = true;
            return View(modelo);
        }

        var estado = await _api.EstadoLlaveAsync(modelo.UsuarioId, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(estado);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!estado.Success)
        {
            modelo.Error = estado.Message;
            return View(modelo);
        }

        if (estado.Data?.TieneLlaveActiva == true && !modelo.ConfirmarReemplazo)
        {
            modelo.Error = LlaveMessages.YaTieneLlaveActiva;
            modelo.PideConfirmacionReemplazo = true;
            return View(modelo);
        }

        var formato = _preparador.FormatearNtfs(disco);
        if (!string.IsNullOrWhiteSpace(formato))
        {
            modelo.Error = formato;
            modelo.Discos = _inventario.Listar();
            return View(modelo);
        }

        modelo.Discos = _inventario.Listar();
        seleccion = SeleccionDiscoUsb.Elegir(modelo.Discos, disco.Letra, out disco);
        if (seleccion != ResultadoSeleccionUsb.Ok || disco is null)
        {
            modelo.Error = InventarioUsbMensajes.De(seleccion);
            return View(modelo);
        }

        if (!disco.EsNtfs)
        {
            modelo.Error = LlaveMessages.DiscoNoEsNtfs;
            return View(modelo);
        }

        var generada = await _api.GenerarLlaveAsync(new GenerarLlaveAdministradorRequest
        {
            UsuarioId = modelo.UsuarioId,
            SerialUsb = disco.Serial,
            Volumen = disco.Volumen,
            ConfirmarReemplazo = modelo.ConfirmarReemplazo
        }, cancellationToken);
        unauthorized = RedirectIfUnauthorized(generada);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!generada.Success || generada.Data is null)
        {
            modelo.Error = generada.Message;
            modelo.PideConfirmacionReemplazo = generada.StatusCode == 409;
            return View(modelo);
        }

        var escritura = _preparador.EscribirLlave(disco, new ContenidoLlaveUsb(1, generada.Data.Codigo, generada.Data.SecretoEnvuelto));
        if (!string.IsNullOrWhiteSpace(escritura))
        {
            modelo.Error = escritura;
            return View(modelo);
        }

        SetFlash(SuccessMessages.LlaveGenerada);
        return RedirectToAction(nameof(Index));
    }

    private async Task<LlaveUsbViewModel> ArmarModeloAsync(LlaveUsbViewModel modelo, CancellationToken cancellationToken)
    {
        modelo.Discos = _inventario.Listar();
        var admins = await _api.ListarAdministradoresLlaveAsync(cancellationToken);
        modelo.Administradores = admins.Success && admins.Data is not null
            ? admins.Data
            : Array.Empty<UsuarioResponseMini>();
        return modelo;
    }
}
