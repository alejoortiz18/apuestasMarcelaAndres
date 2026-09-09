using Microsoft.AspNetCore.Mvc;
using NewRich.Shared.Results;

namespace NewRich.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult From(Result result)
    {
        return StatusCode(result.StatusCode, ApiResponse.From(result));
    }

    protected IActionResult From<T>(Result<T> result)
    {
        return StatusCode(result.StatusCode, ApiResponse.From(result));
    }

    protected Guid UsuarioId => Guid.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);

    protected Guid SesionId => Guid.Parse(User.FindFirst("sesionId")!.Value);

    protected Guid? DispositivoId =>
        Guid.TryParse(User.FindFirst("dispositivoId")?.Value, out var id) ? id : null;
}
