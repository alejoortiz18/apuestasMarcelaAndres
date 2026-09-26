using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Usb;
using NewRich.Application.Contracts.Auth;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;

namespace NewRich.Admin.Controllers;

public sealed class CuentaController : Controller
{
    private readonly IAdminApiClient _api;
    private readonly IAdminSessionService _session;
    private readonly IInventarioUsb _inventario;
    private readonly ILectorLlaveUsb _lectorLlave;

    public CuentaController(
        IAdminApiClient api,
        IAdminSessionService session,
        IInventarioUsb inventario,
        ILectorLlaveUsb lectorLlave)
    {
        _api = api;
        _session = session;
        _inventario = inventario;
        _lectorLlave = lectorLlave;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Ingresar(bool expired = false)
    {
        if (User.Identity?.IsAuthenticated == true && User.EsAdministrador() && !User.DebeCambiarPassword())
        {
            return RedirectToAction("Index", "Inicio");
        }

        return View(CrearLogin(expired ? UiTexts.SesionExpiradaVista : null));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult DiscosUsb()
    {
        var discos = _inventario.Listar();
        return Json(discos.Select(d => new { letra = d.Letra, etiqueta = d.Etiqueta, ntfs = d.EsNtfs }));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(LoginViewModel model, CancellationToken cancellationToken)
    {
        model.Discos = _inventario.Listar();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var prueba = ResolverPrueba(model.Usuario.Trim(), model.LetraUsb, model.Discos);
        var result = await _api.LoginAsync(new LoginRequest
        {
            Usuario = model.Usuario.Trim(),
            Password = model.Password,
            PruebaLlave = prueba
        }, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.Error = MensajeIngreso(result.Message);
            return View(model);
        }

        if (!RolConsola.EsEquipoAdministrativo(result.Data.Rol))
        {
            model.Error = UiTexts.SoloAdministrador;
            return View(model);
        }

        await _session.SignInAsync(HttpContext, result.Data);
        if (result.Data.DebeCambiarPassword)
        {
            return RedirectToAction(nameof(CambiarPassword));
        }

        return RedirectToAction("Index", "Inicio");
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = AuthCookieNames.Scheme)]
    public IActionResult CambiarPassword()
    {
        ViewData["Title"] = UiTexts.CambiarContrasena;
        return View(new CambiarPasswordViewModel());
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthCookieNames.Scheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = UiTexts.CambiarContrasena;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.CambiarPasswordAsync(new CambiarPasswordRequest
        {
            PasswordActual = model.PasswordActual,
            PasswordNuevo = model.PasswordNuevo,
            PasswordConfirmacion = model.PasswordConfirmacion
        }, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.Error = result.Message;
            return View(model);
        }

        await _session.SignInAsync(HttpContext, result.Data);
        TempData["FlashOk"] = SuccessMessages.PasswordCambiado;
        return RedirectToAction("Index", "Inicio");
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthCookieNames.Scheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir(string? motivo, CancellationToken cancellationToken)
    {
        await _api.LogoutAsync(cancellationToken);
        await _session.SignOutAsync(HttpContext);
        if (motivo == "inactividad")
        {
            TempData["AvisoIngreso"] = UiTexts.SesionExpiradaVista;
        }

        return RedirectToAction(nameof(Ingresar));
    }

    private static string MensajeIngreso(string? mensajeApi)
    {
        if (string.IsNullOrWhiteSpace(mensajeApi) ||
            mensajeApi == AuthMessages.DispositivoNoRegistrado ||
            mensajeApi == AuthMessages.DispositivoInactivo ||
            mensajeApi == AuthMessages.DispositivoNoAsociado ||
            mensajeApi == AuthMessages.DispositivoTipoNoCorresponde ||
            mensajeApi == AuthMessages.FueraDeHorarioOperacion)
        {
            return string.IsNullOrWhiteSpace(mensajeApi)
                ? AuthMessages.CredencialesInvalidas
                : UiTexts.SoloAdministrador;
        }

        return mensajeApi;
    }

    private LoginViewModel CrearLogin(string? error) =>
        new()
        {
            Error = error,
            Discos = _inventario.Listar()
        };

    private PruebaLlaveAdministradorRequest? ResolverPrueba(string usuario, string? letra, IReadOnlyList<DiscoUsbInfo> discos)
    {
        var resultado = SeleccionDiscoUsb.Elegir(discos, letra, out var disco);
        if (resultado != ResultadoSeleccionUsb.Ok || disco is null)
        {
            return null;
        }

        return _lectorLlave.CrearPrueba(disco, usuario);
    }
}
