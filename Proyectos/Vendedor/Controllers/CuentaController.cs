using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Auth;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Filters;
using NewRich.Vendedor.Models;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.Controllers;

public sealed class CuentaController : Controller
{
    private readonly IVendedorApiClient _api;
    private readonly IVendedorSessionService _session;

    public CuentaController(IVendedorApiClient api, IVendedorSessionService session)
    {
        _api = api;
        _session = session;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Ingresar(bool expired = false)
    {
        if (expired)
        {
            await _session.SignOutAsync(HttpContext);
            return View(new LoginViewModel { Error = UiTexts.SesionExpirada });
        }

        if (User.Identity?.IsAuthenticated == true && User.EsVendedor() && !User.DebeCambiarPassword())
        {
            return RedirectToAction("Index", "Inicio");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidarFormularioIngreso]
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
            model.Error = string.IsNullOrWhiteSpace(result.Message) ? UiTexts.ApiNoDisponible : result.Message;
            return View(model);
        }

        if (result.Data.Rol != RolUsuario.Vendedor)
        {
            model.Error = UiTexts.SoloVendedor;
            return View(model);
        }

        await _session.SignInAsync(HttpContext, result.Data);
        return result.Data.DebeCambiarPassword
            ? RedirectToAction(nameof(CambiarPassword))
            : RedirectToAction("Index", "Inicio");
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = AuthCookieNames.Scheme, Roles = "Vendedor")]
    public IActionResult CambiarPassword()
    {
        ViewData["Title"] = UiTexts.CambiarContrasena;
        return View(new CambiarPasswordViewModel());
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthCookieNames.Scheme, Roles = "Vendedor")]
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
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await _session.SignInAsync(HttpContext, result.Data);
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
}
