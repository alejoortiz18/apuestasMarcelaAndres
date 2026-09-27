using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador,Vendedor")]
public sealed class VersionesAplicacionController : ApiControllerBase
{
    private readonly IVersionAplicacionService _versiones;

    public VersionesAplicacionController(IVersionAplicacionService versiones)
    {
        _versiones = versiones;
    }

    [HttpGet("vigente")]
    public async Task<IActionResult> Vigente(CancellationToken cancellationToken)
    {
        return From(await _versiones.ObtenerVigenteAsync(cancellationToken));
    }

    [HttpGet("vigente/archivo")]
    public async Task<IActionResult> Descargar(CancellationToken cancellationToken)
    {
        var resultado = await _versiones.AbrirVigenteAsync(cancellationToken);
        if (!resultado.IsSuccess || resultado.Data is null)
        {
            return From(Result.Fail(resultado.Message, resultado.StatusCode));
        }

        return File(resultado.Data.Contenido, "application/vnd.android.package-archive", resultado.Data.NombreDescarga);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _versiones.ListarAsync(cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.ConfiguracionVersionAplicacion)]
    [HttpPost]
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
        return From(await _versiones.PublicarAsync(
            contenido,
            archivo.Length,
            archivo.FileName,
            nombreVersion ?? string.Empty,
            numeroCompilacion,
            cancellationToken));
    }
}
