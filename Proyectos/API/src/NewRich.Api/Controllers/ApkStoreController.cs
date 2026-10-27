using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.ApkStore;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Api.Controllers;

/// <summary>Rutas api/apkstore: llave de firma de los APK y versiones publicadas.</summary>
[Authorize]
public sealed class ApkStoreController : ApiControllerBase
{
    private const string RolSuper = "Super";
    private readonly IApkStoreService _apkStore;

    public ApkStoreController(IApkStoreService apkStore)
    {
        _apkStore = apkStore;
    }

    [Authorize(Roles = RolSuper)]
    [HttpPost("llave")]
    public async Task<IActionResult> CrearLlave([FromBody] ConfirmarSuperusuarioRequest request, CancellationToken cancellationToken) =>
        From(await _apkStore.CrearLlaveAsync(UsuarioId, request.Password, Ip(), cancellationToken));

    [Authorize(Roles = RolSuper)]
    [HttpPost("llave/descarga")]
    public async Task<IActionResult> DescargarLlave([FromBody] ConfirmarSuperusuarioRequest request, CancellationToken cancellationToken) =>
        From(await _apkStore.DescargarLlaveAsync(UsuarioId, request.Password, Ip(), cancellationToken));

    [HttpGet("llave/huella")]
    public async Task<IActionResult> Huella(CancellationToken cancellationToken) =>
        From(await _apkStore.ObtenerHuellaAsync(cancellationToken));

    [Authorize(Roles = RolSuper)]
    [HttpPost("versiones")]
    [RequestSizeLimit(VersionAplicacionLimites.TamanoMaximoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = VersionAplicacionLimites.TamanoMaximoBytes)]
    public async Task<IActionResult> Publicar(
        IFormFile? archivo,
        [FromForm] string? nombreVersion,
        [FromForm] int numeroCompilacion,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return From(Result.Fail(VersionAplicacionMessages.ArchivoVacio));
        }

        await using var contenido = archivo.OpenReadStream();
        return From(await _apkStore.PublicarAsync(
            UsuarioId,
            contenido,
            archivo.Length,
            archivo.FileName,
            nombreVersion ?? string.Empty,
            numeroCompilacion,
            Ip(),
            cancellationToken));
    }

    [Authorize(Roles = RolSuper)]
    [HttpGet("versiones")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        From(await _apkStore.ListarVersionesAsync(cancellationToken));

    [Authorize(Roles = RolSuper)]
    [HttpGet("auditoria")]
    public async Task<IActionResult> Auditoria(CancellationToken cancellationToken) =>
        From(await _apkStore.ListarAuditoriaAsync(cancellationToken));

    [HttpGet("versiones/vigente")]
    public async Task<IActionResult> Vigente(CancellationToken cancellationToken) =>
        From(await _apkStore.ObtenerVigenteAsync(cancellationToken));

    [HttpGet("versiones/vigente/archivo")]
    public async Task<IActionResult> DescargarVigente(CancellationToken cancellationToken)
    {
        var resultado = await _apkStore.AbrirVigenteAsync(cancellationToken);
        if (!resultado.IsSuccess || resultado.Data is null)
        {
            return From(Result.Fail(resultado.Message, resultado.StatusCode));
        }

        return File(resultado.Data.Contenido, "application/vnd.android.package-archive", resultado.Data.NombreDescarga);
    }

    // Detrás del App Service la IP del cliente llega en X-Forwarded-For.
    private string? Ip() =>
        Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? HttpContext.Connection.RemoteIpAddress?.ToString();
}
