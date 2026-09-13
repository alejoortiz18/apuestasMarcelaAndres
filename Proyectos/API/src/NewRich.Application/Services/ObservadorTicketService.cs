using NewRich.Application.Contracts.Boletos;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IObservadorTicketService
{
    Task<Result<ConsultaTicketResponse>> ConsultarAsync(string? contenidoQr, CancellationToken cancellationToken);
}

public sealed class ObservadorTicketService : IObservadorTicketService
{
    private readonly IValidacionBoletoService _validacion;

    public ObservadorTicketService(IValidacionBoletoService validacion)
    {
        _validacion = validacion;
    }

    public Task<Result<ConsultaTicketResponse>> ConsultarAsync(string? contenidoQr, CancellationToken cancellationToken) =>
        _validacion.ConsultarPorCodigoAsync(contenidoQr, cancellationToken);
}
