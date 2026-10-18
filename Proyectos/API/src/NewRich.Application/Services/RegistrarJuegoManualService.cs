using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Contracts.RegistrarJuegoManual;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IRegistrarJuegoManualService
{
    Task<Result<IReadOnlyList<LoteriaResponse>>> ListarLoteriasAsync(CancellationToken cancellationToken);
    Task<Result<RegistroJuegoManualResponse>> RegistrarAsync(Guid vendedorId, RegistrarJuegoManualRequest request, string? idempotencyKey, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<FilaHistoricoJuegoManualResponse>>> ConsultarAsync(Guid vendedorId, DateTime desde, DateTime hasta, CancellationToken cancellationToken);
}

public sealed class RegistrarJuegoManualService : IRegistrarJuegoManualService
{
    private readonly INewRichDbContext _db;
    private readonly IRegistrarJuegoManualRepositorio _registros;
    private readonly IClock _clock;

    public RegistrarJuegoManualService(INewRichDbContext db, IRegistrarJuegoManualRepositorio registros, IClock clock)
    {
        _db = db;
        _registros = registros;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<LoteriaResponse>>> ListarLoteriasAsync(CancellationToken cancellationToken)
    {
        var loterias = await _db.Loterias
            .OrderBy(l => l.Nombre)
            .Select(l => new LoteriaResponse
            {
                LoteriaId = l.LoteriaId,
                Nombre = l.Nombre,
                Estado = l.Estado,
                Tope = l.Tope
            })
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<LoteriaResponse>>.Ok(loterias, RegistrarJuegoManualMessages.Registrado);
    }

    public async Task<Result<RegistroJuegoManualResponse>> RegistrarAsync(
        Guid vendedorId,
        RegistrarJuegoManualRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result<RegistroJuegoManualResponse>.Fail(ValidationMessages.IdempotencyKeyRequerida);
        }

        var existente = await _registros.BuscarPorIdempotenciaAsync(idempotencyKey, cancellationToken);
        if (existente is not null)
        {
            return Result<RegistroJuegoManualResponse>.Ok(await MapAsync(existente, cancellationToken), VentaMessages.VentaDuplicadaIdempotente);
        }

        if (await FueraDeHorario(cancellationToken))
        {
            return Result<RegistroJuegoManualResponse>.Fail(VentaMessages.VentaFueraDeHorario);
        }

        if (request.Filas is null || request.Filas.Count == 0)
        {
            return Result<RegistroJuegoManualResponse>.Fail(RegistrarJuegoManualMessages.CamposObligatorios);
        }

        foreach (var fila in request.Filas)
        {
            if (fila.LoteriaId == Guid.Empty || fila.Valor < 0 || string.IsNullOrWhiteSpace(fila.Numero))
            {
                return Result<RegistroJuegoManualResponse>.Fail(RegistrarJuegoManualMessages.CamposObligatorios);
            }

            if (fila.Numero.Any(c => !char.IsDigit(c)))
            {
                return Result<RegistroJuegoManualResponse>.Fail(RegistrarJuegoManualMessages.NumeroSoloDigitos);
            }
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == vendedorId, cancellationToken);
        if (usuario is null || string.IsNullOrWhiteSpace(usuario.NombreCompleto))
        {
            return Result<RegistroJuegoManualResponse>.Fail(RegistrarJuegoManualMessages.UsuarioSinNombre);
        }

        var ids = request.Filas.Select(f => f.LoteriaId).Distinct().ToList();
        var loterias = await _db.Loterias.Where(l => ids.Contains(l.LoteriaId)).ToListAsync(cancellationToken);
        if (loterias.Count != ids.Count)
        {
            return Result<RegistroJuegoManualResponse>.Fail(VentaMessages.LoteriaNoEncontrada);
        }

        var registro = new RegistroJuegoManual
        {
            RegistroId = Guid.NewGuid(),
            UsuarioId = vendedorId,
            NombreCompleto = usuario.NombreCompleto.Trim(),
            FechaRegistro = _clock.LocalNow,
            IdempotencyKey = idempotencyKey,
            CodigoPublico = await GenerarCodigo(cancellationToken),
            Filas = request.Filas.Select(f => new FilaRegistroJuegoManual
            {
                FilaId = Guid.NewGuid(),
                LoteriaId = f.LoteriaId,
                Valor = f.Valor,
                Numero = f.Numero.Trim()
            }).ToList()
        };
        foreach (var fila in registro.Filas)
        {
            fila.RegistroId = registro.RegistroId;
        }

        await _registros.GuardarAsync(registro, cancellationToken);
        return Result<RegistroJuegoManualResponse>.Ok(await MapAsync(registro, cancellationToken), RegistrarJuegoManualMessages.Registrado);
    }

    public async Task<Result<IReadOnlyList<FilaHistoricoJuegoManualResponse>>> ConsultarAsync(
        Guid vendedorId,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken)
    {
        var filas = await _registros.ConsultarAsync(vendedorId, desde, hasta, cancellationToken);
        IReadOnlyList<FilaHistoricoJuegoManualResponse> lista = filas.Select(f => new FilaHistoricoJuegoManualResponse
        {
            FechaRegistro = f.FechaRegistro,
            CodigoPublico = f.CodigoPublico.Trim(),
            Loteria = f.Loteria,
            Numero = f.Numero,
            Valor = f.Valor
        }).ToList();
        return Result<IReadOnlyList<FilaHistoricoJuegoManualResponse>>.Ok(lista, RegistrarJuegoManualMessages.Registrado);
    }

    private async Task<bool> FueraDeHorario(CancellationToken cancellationToken)
    {
        var aperturaCfg = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "HoraApertura", cancellationToken);
        var cierreCfg = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "HoraCierre", cancellationToken);
        if (aperturaCfg is null || cierreCfg is null
            || !TimeSpan.TryParse(aperturaCfg.Valor, out var apertura)
            || !TimeSpan.TryParse(cierreCfg.Valor, out var cierre))
        {
            return false;
        }

        return HorarioOperacion.EstaFuera(_clock.LocalNow.TimeOfDay, apertura, cierre);
    }

    private async Task<string> GenerarCodigo(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20; i++)
        {
            var codigo = CodigoPublicoGenerator.Formatear(Random.Shared.Next(0, 10_000_000));
            if (!await _registros.ExisteCodigoAsync(codigo, cancellationToken))
            {
                return codigo;
            }
        }

        throw new InvalidOperationException(VentaMessages.CodigoPublicoNoGenerado);
    }

    private async Task<RegistroJuegoManualResponse> MapAsync(RegistroJuegoManual registro, CancellationToken cancellationToken)
    {
        var ids = registro.Filas.Select(f => f.LoteriaId).Distinct().ToList();
        var nombres = await _db.Loterias
            .Where(l => ids.Contains(l.LoteriaId))
            .ToDictionaryAsync(l => l.LoteriaId, l => l.Nombre, cancellationToken);
        return new RegistroJuegoManualResponse
        {
            RegistroId = registro.RegistroId,
            CodigoPublico = registro.CodigoPublico.Trim(),
            FechaRegistro = registro.FechaRegistro,
            Filas = registro.Filas.Select(f => new FilaRegistroJuegoManualResponse
            {
                Numero = f.Numero,
                Valor = f.Valor,
                Loteria = nombres.GetValueOrDefault(f.LoteriaId) ?? string.Empty
            }).ToList()
        };
    }
}
