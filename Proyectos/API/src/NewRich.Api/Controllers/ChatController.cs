using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Chat;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Services;
using NewRich.Constants;

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
    public async Task<IActionResult> Listar([FromQuery] string? tipo, CancellationToken cancellationToken)
    {
        var canal = ResolverTipo(tipo);
        return From(await _chatService.ListarAsync(UsuarioId, canal, cancellationToken));
    }

    [HttpPost("reporte-tecnico")]
    [Authorize(Roles = "Vendedor")]
    public async Task<IActionResult> ReportarTecnico([FromBody] ReporteTecnicoRequest request, CancellationToken cancellationToken)
    {
        return From(await _chatService.ReportarVentaTecnicoAsync(UsuarioId, request, cancellationToken));
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
    [RequiereConfirmacion(AccionesProtegidas.SoporteCerrar)]
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

    private static NewRich.Domain.Enums.TipoConversacion ResolverTipo(string? tipo)
    {
        if (string.Equals(tipo, nameof(NewRich.Domain.Enums.TipoConversacion.SoporteTecnico), StringComparison.OrdinalIgnoreCase)
            || string.Equals(tipo, "SoporteTecnico", StringComparison.OrdinalIgnoreCase))
        {
            return NewRich.Domain.Enums.TipoConversacion.SoporteTecnico;
        }

        return NewRich.Domain.Enums.TipoConversacion.AtencionCliente;
    }
}
