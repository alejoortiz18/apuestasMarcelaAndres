using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.RegistrarJuegoNumerosWebManual;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IRegistrarJuegoNumerosWebManualService
{
    Task<Result<RegistroJuegoNumerosWebManualResponse>> RegistrarAsync(
        Guid vendedorId,
        RegistrarJuegoNumerosWebManualRequest request,
        CancellationToken cancellationToken);
}

public sealed class RegistrarJuegoNumerosWebManualService : IRegistrarJuegoNumerosWebManualService
{
    private readonly INewRichDbContext _db;
    private readonly IRegistrarJuegoNumerosWebManualRepositorio _registros;
    private readonly IClock _clock;

    public RegistrarJuegoNumerosWebManualService(
        INewRichDbContext db,
        IRegistrarJuegoNumerosWebManualRepositorio registros,
        IClock clock)
    {
        _db = db;
        _registros = registros;
        _clock = clock;
    }

    public async Task<Result<RegistroJuegoNumerosWebManualResponse>> RegistrarAsync(
        Guid vendedorId,
        RegistrarJuegoNumerosWebManualRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RegistroId == Guid.Empty)
        {
            return Fallo(JuegoNumerosManualMessages.VentaIdRequerido);
        }

        var existente = await _registros.BuscarAsync(request.RegistroId, cancellationToken);
        if (existente is not null)
        {
            return Result<RegistroJuegoNumerosWebManualResponse>.Ok(await MapAsync(existente, cancellationToken), JuegoNumerosManualMessages.YaRegistrada);
        }

        var fecha = _clock.LocalNow;
        var validacion = await ValidarAsync(fecha, request, cancellationToken);
        if (validacion.Error is not null)
        {
            return Fallo(validacion.Error);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == vendedorId, cancellationToken);
        if (usuario is null || string.IsNullOrWhiteSpace(usuario.NombreCompleto))
        {
            return Fallo(RegistrarJuegoManualMessages.UsuarioSinNombre);
        }

        var registro = new RegistroJuegoNumerosWebManual
        {
            RegistroId = request.RegistroId,
            UsuarioId = vendedorId,
            NombreCompleto = usuario.NombreCompleto.Trim(),
            FechaRegistro = fecha,
            CodigoPublico = await GenerarCodigo(cancellationToken),
            ValorJugado = request.ValorJugado,
            Apuestas = RepartoJuegoNumerosManual
                .Repartir(request.ValorJugado, validacion.Numeros, validacion.Loterias.Select(l => l.LoteriaId).ToList())
                .Select(a => new ApuestaRegistroJuegoNumerosWebManual
                {
                    ApuestaId = Guid.NewGuid(),
                    RegistroId = request.RegistroId,
                    LoteriaId = a.LoteriaId,
                    Numero = a.Numero,
                    Valor = a.Valor
                }).ToList()
        };

        await _registros.GuardarAsync(registro, cancellationToken);
        return Result<RegistroJuegoNumerosWebManualResponse>.Ok(await MapAsync(registro, cancellationToken), JuegoNumerosManualMessages.Registrada);
    }

    private async Task<RegistroJuegoNumerosWebManualResponse> MapAsync(RegistroJuegoNumerosWebManual registro, CancellationToken cancellationToken)
    {
        var ids = registro.Apuestas.Select(a => a.LoteriaId).Distinct().ToList();
        var nombres = await _db.Loterias
            .Where(l => ids.Contains(l.LoteriaId))
            .ToDictionaryAsync(l => l.LoteriaId, l => l.Nombre, cancellationToken);
        var numeros = registro.Apuestas.Select(a => a.Numero).Distinct().ToList();
        var apuestas = registro.Apuestas.Count;
        return new RegistroJuegoNumerosWebManualResponse
        {
            RegistroId = registro.RegistroId,
            CodigoPublico = registro.CodigoPublico.Trim(),
            Qr = registro.CodigoPublico.Trim(),
            FechaRegistro = registro.FechaRegistro,
            ValorJugado = registro.ValorJugado,
            CantidadNumeros = numeros.Count,
            CantidadLoterias = ids.Count,
            CantidadApuestas = apuestas,
            ValorPorApuesta = RepartoJuegoNumerosManual.ValorBase(registro.ValorJugado, apuestas),
            ApuestasConSobrante = RepartoJuegoNumerosManual.ApuestasConSobrante(registro.ValorJugado, apuestas),
            Numeros = numeros,
            Loterias = ids.Select(id => nombres.GetValueOrDefault(id) ?? string.Empty).Where(n => n.Length > 0).Distinct().ToList(),
            Apuestas = registro.Apuestas.Select(a => new ApuestaJuegoNumerosWebManualResponse
            {
                Loteria = nombres.GetValueOrDefault(a.LoteriaId) ?? string.Empty,
                Numero = a.Numero,
                Valor = a.Valor
            }).ToList()
        };
    }

    private sealed record Validacion(string? Error, List<string> Numeros, List<Loteria> Loterias);

    private async Task<Validacion> ValidarAsync(DateTime fecha, RegistrarJuegoNumerosWebManualRequest request, CancellationToken cancellationToken)
    {
        static Validacion Error(string mensaje) => new(mensaje, [], []);

        var loteriaIds = request.LoteriaIds ?? [];
        if (loteriaIds.Count == 0)
        {
            return Error(JuegoNumerosManualMessages.LoteriasRequeridas);
        }

        if (loteriaIds.Distinct().Count() != loteriaIds.Count)
        {
            return Error(JuegoNumerosManualMessages.LoteriaRepetida);
        }

        if (request.ValorJugado <= 0)
        {
            return Error(JuegoNumerosManualMessages.ValorRequerido);
        }

        if (request.ValorJugado != decimal.Truncate(request.ValorJugado))
        {
            return Error(JuegoNumerosManualMessages.ValorEntero);
        }

        var numeros = (request.Numeros ?? []).Select(n => (n ?? string.Empty).Trim()).Where(n => n.Length > 0).ToList();
        if (numeros.Count == 0)
        {
            return Error(JuegoNumerosManualMessages.NumerosRequeridos);
        }

        var restringidos = await _db.NumerosRestringidos.AsNoTracking().Select(x => x.Numero).ToListAsync(cancellationToken);
        var vistos = new HashSet<string>();
        foreach (var numero in numeros)
        {
            if (!NumeroApuesta.EsValido(numero))
            {
                return Error(string.Format(JuegoNumerosManualMessages.NumeroFormato, numero));
            }

            if (!vistos.Add(numero))
            {
                return Error(string.Format(JuegoNumerosManualMessages.NumeroRepetido, numero));
            }

            if (NumerosRestringidos.EstaBloqueado(numero, restringidos))
            {
                return Error(string.Format(JuegoNumerosManualMessages.NumeroRestringido, numero));
            }
        }

        var cantidad = RepartoJuegoNumerosManual.CantidadApuestas(numeros.Count, loteriaIds.Count);
        if (!RepartoJuegoNumerosManual.AlcanzaParaTodas(request.ValorJugado, cantidad))
        {
            return Error(string.Format(JuegoNumerosManualMessages.ValorInsuficiente, cantidad));
        }

        var encontradas = await _db.Loterias
            .Where(l => loteriaIds.Contains(l.LoteriaId))
            .ToDictionaryAsync(l => l.LoteriaId, cancellationToken);
        if (encontradas.Count != loteriaIds.Count)
        {
            return Error(JuegoNumerosManualMessages.LoteriaNoEncontrada);
        }

        var dias = await _db.LoteriasDiasSemana.AsNoTracking()
            .Where(d => loteriaIds.Contains(d.LoteriaId))
            .ToListAsync(cancellationToken);
        var diasPorLoteria = dias.GroupBy(d => d.LoteriaId).ToDictionary(g => g.Key, g => g.Select(x => x.DiaSemana).ToList());
        var dia = DiasVentaLoteria.DiaDe(fecha);
        var loterias = loteriaIds.Select(id => encontradas[id]).ToList();
        foreach (var loteria in loterias)
        {
            if (loteria.Estado != EstadoGeneral.Activo)
            {
                return Error(string.Format(JuegoNumerosManualMessages.LoteriaInactiva, loteria.Nombre));
            }

            if (!ValidacionTope.LoteriaJugable(loteria.Tope))
            {
                return Error(string.Format(JuegoNumerosManualMessages.LoteriaTopeCero, loteria.Nombre));
            }

            if (!DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, diasPorLoteria.GetValueOrDefault(loteria.LoteriaId), dia))
            {
                return Error(string.Format(JuegoNumerosManualMessages.LoteriaNoHabilitadaHoy, loteria.Nombre));
            }
        }

        return new Validacion(null, numeros, loterias);
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

    private static Result<RegistroJuegoNumerosWebManualResponse> Fallo(string mensaje) =>
        Result<RegistroJuegoNumerosWebManualResponse>.Fail(mensaje);
}
