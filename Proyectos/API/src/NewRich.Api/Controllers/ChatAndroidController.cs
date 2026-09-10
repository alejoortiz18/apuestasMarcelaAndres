using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Chat;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class ChatAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public ChatAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [HttpPost("IniciarMob")]
    public async Task<IActionResult> IniciarMob([FromBody] IniciarChatRequest request, CancellationToken cancellationToken)
    {
        return From(await _pda.IniciarChatMobAsync(UsuarioId, request, cancellationToken));
    }

    [HttpGet("ListarMob")]
    public async Task<IActionResult> ListarMob(CancellationToken cancellationToken)
    {
        return From(await _pda.ListarChatMobAsync(UsuarioId, cancellationToken));
    }

    [HttpGet("ObtenerMob/{id:guid}")]
    public async Task<IActionResult> ObtenerMob(Guid id, CancellationToken cancellationToken)
    {
        return From(await _pda.ObtenerChatMobAsync(id, UsuarioId, cancellationToken));
    }

    [HttpPost("EnviarMob/{id:guid}")]
    public async Task<IActionResult> EnviarMob(Guid id, [FromBody] EnviarMensajeRequest request, CancellationToken cancellationToken)
    {
        return From(await _pda.EnviarChatMobAsync(id, UsuarioId, request, cancellationToken));
    }

    [HttpGet("DescargarAdjuntoMob/{adjuntoId:guid}")]
    public async Task<IActionResult> DescargarAdjuntoMob(Guid adjuntoId, CancellationToken cancellationToken)
    {
        var result = await _pda.DescargarAdjuntoMobAsync(adjuntoId, UsuarioId, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
        {
            return From(result);
        }

        return File(result.Data.Contenido, ChatAdjunto.TipoMime(result.Data.NombreArchivo), result.Data.NombreArchivo);
    }
}
