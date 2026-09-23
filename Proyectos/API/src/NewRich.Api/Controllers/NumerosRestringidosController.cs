using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Services;
using NewRich.Constants;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador,Super")]
public sealed class NumerosRestringidosController : ApiControllerBase
{
    private readonly INumerosRestringidosService _numeros;

    public NumerosRestringidosController(INumerosRestringidosService numeros)
    {
        _numeros = numeros;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _numeros.ListarAsync(cancellationToken));
    }

    [Authorize(Roles = "Administrador,Super")]
    [RequiereConfirmacion(AccionesProtegidas.ConfiguracionNumerosRestringidos)]
    [HttpPost]
    public async Task<IActionResult> Agregar([FromBody] AgregarNumeroRestringidoRequest request, CancellationToken cancellationToken)
    {
        return From(await _numeros.AgregarAsync(request.Numero, cancellationToken));
    }

    [Authorize(Roles = "Administrador,Super")]
    [RequiereConfirmacion(AccionesProtegidas.ConfiguracionNumerosRestringidos)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        return From(await _numeros.EliminarAsync(id, cancellationToken));
    }
}
