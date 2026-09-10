using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Chat;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class ChatController : ApiControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<IActionResult> Iniciar([FromBody] IniciarChatRequest request, CancellationToken cancellationToken)
    {
        return From(await _chatService.IniciarAsync(UsuarioId, request, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _chatService.ListarAsync(UsuarioId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        return From(await _chatService.ObtenerAsync(id, UsuarioId, cancellationToken));
    }

    [HttpPost("{id:guid}/mensajes")]
    public async Task<IActionResult> Enviar(Guid id, [FromBody] EnviarMensajeRequest request, CancellationToken cancellationToken)
    {
        return From(await _chatService.EnviarAsync(id, UsuarioId, request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/cerrar")]
    public async Task<IActionResult> Cerrar(Guid id, CancellationToken cancellationToken)
    {
        return From(await _chatService.CerrarAsync(id, UsuarioId, cancellationToken));
    }

    [HttpGet("adjuntos/{adjuntoId:guid}")]
    public async Task<IActionResult> Descargar(Guid adjuntoId, CancellationToken cancellationToken)
    {
        var result = await _chatService.DescargarAdjuntoAsync(adjuntoId, UsuarioId, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
        {
            return From(result);
        }

        return File(result.Data.Contenido, ChatAdjunto.TipoMime(result.Data.NombreArchivo), result.Data.NombreArchivo);
    }
}
