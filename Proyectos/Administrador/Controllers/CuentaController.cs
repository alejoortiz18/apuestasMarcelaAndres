using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Auth;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class CuentaController : Controller
{
    private readonly IAdminApiClient _api;
    private readonly IAdminSessionService _session;

    public CuentaController(IAdminApiClient api, IAdminSessionService session)
    {
        _api = api;
        _session = session;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Ingresar(bool expired = false)
    {
        if (User.Identity?.IsAuthenticated == true && User.EsAdministrador() && !User.DebeCambiarPassword())
        {
            return RedirectToAction("Index", "Inicio");
        }

        return View(new LoginViewModel
        {
            Error = expired ? UiTexts.SesionExpiradaVista : null
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.LoginAsync(new LoginRequest
        {
            Usuario = model.Usuario.Trim(),
            Password = model.Password
        }, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            model.Error = MensajeIngreso(result.Message);
            return View(model);
        }

        if (result.Data.Rol != RolUsuario.Administrador)
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
    public async Task<IActionResult> Salir(CancellationToken cancellationToken)
    {
        await _api.LogoutAsync(cancellationToken);
        await _session.SignOutAsync(HttpContext);
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
}
