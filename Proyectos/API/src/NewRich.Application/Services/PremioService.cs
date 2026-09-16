using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Chat;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Premios;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class PremioService : IPremioService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;
    private readonly INotificacionService _notificaciones;
    private readonly IQrCryptoService _qr;
    private readonly IChatFileStorage _files;
    private readonly IValidacionBoletoService _validacion;

    public PremioService(
        INewRichDbContext db,
        IClock clock,
        INotificacionService notificaciones,
        IQrCryptoService qr,
        IChatFileStorage files,
        IValidacionBoletoService validacion)
    {
        _db = db;
        _clock = clock;
        _notificaciones = notificaciones;
        _qr = qr;
        _files = files;
        _validacion = validacion;
    }

    public async Task<Result<IReadOnlyList<CasoGanadorResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var casos = await Query().OrderByDescending(c => c.FechaReporte).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<CasoGanadorResponse>>.Ok(casos.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IReadOnlyList<CasoGanadorResponse>>> ListarAsignadosAsync(Guid observadorId, CancellationToken cancellationToken)
    {
        var casos = await Query()
            .Where(c => c.ObservadorAsignado == observadorId
                && (c.Estado == EstadoCasoGanador.Asignado || c.Estado == EstadoCasoGanador.EnProceso))
            .OrderByDescending(c => c.FechaAsignacion ?? c.FechaReporte)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<CasoGanadorResponse>>.Ok(casos.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<CasoGanadorResponse>> ObtenerAsync(Guid casoId, CancellationToken cancellationToken)
    {
        var caso = await Query()
            .Include(c => c.EntregaGanador)
            .ThenInclude(e => e!.Evidencias)
            .Include(c => c.EntregaGanador)
            .ThenInclude(e => e!.PersonaQueEntregaNavigation)
            .FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        var detalle = Map(caso);
        await AgregarDetalleApuestaAsync(caso, detalle, cancellationToken);
        AgregarDetalleEntrega(caso, detalle);
        return Result<CasoGanadorResponse>.Ok(detalle, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<DescargaAdjuntoResponse>> ObtenerEvidenciaAsync(
        Guid casoId,
        Guid evidenciaId,
        CancellationToken cancellationToken)
    {
        var evidencia = await _db.EvidenciasGanador
            .Include(e => e.EntregaGanador)
            .FirstOrDefaultAsync(e => e.EvidenciaId == evidenciaId && e.EntregaGanador!.CasoId == casoId, cancellationToken);
        if (evidencia is null)
        {
            return Result<DescargaAdjuntoResponse>.Fail(PremioMessages.EvidenciaNoEncontrada, 404);
        }

        var stream = await _files.OpenReadAsync(evidencia.RutaImagen, cancellationToken);
        if (stream is null)
        {
            return Result<DescargaAdjuntoResponse>.Fail(PremioMessages.EvidenciaNoEncontrada, 404);
        }

        return Result<DescargaAdjuntoResponse>.Ok(new DescargaAdjuntoResponse
        {
            NombreArchivo = Path.GetFileName(evidencia.RutaImagen),
            Contenido = stream
        }, SuccessMessages.OperacionExitosa);
    }

    private async Task AgregarDetalleApuestaAsync(
        CasoGanador caso,
        CasoGanadorResponse detalle,
        CancellationToken cancellationToken)
    {
        var boleto = await _db.Boletos
            .Include(b => b.Venta)
            .Include(b => b.Juegos)
            .ThenInclude(j => j.JuegoLoterias)
            .ThenInclude(jl => jl.Loteria)
            .FirstOrDefaultAsync(b => b.BoletoId == caso.BoletoId, cancellationToken);
        if (boleto is null)
        {
            return;
        }

        detalle.TotalApostado = boleto.Venta?.Total;
        var consecutivoOffline = await _db.CodigosPreventaOffline
            .AsNoTracking()
            .Where(c => c.CodigoId == boleto.BoletoId)
            .Select(c => c.ConsecutivoUnico)
            .FirstOrDefaultAsync(cancellationToken);
        detalle.CodigoRecibo = CodigoImpresoTicket.De(boleto.CodigoPublico, boleto.QrCifrado, consecutivoOffline);
        var desfase = FechaJuegoBoleto.Desfase(_clock.UtcNow, _clock.LocalNow);
        var fechaJuego = FechaJuegoBoleto.De(boleto.Venta?.FechaVenta ?? boleto.FechaCreacion, desfase);
        detalle.FechaJuego = fechaJuego.ToDateTime(TimeOnly.MinValue);

        var loteriaIds = boleto.Juegos.SelectMany(j => j.JuegoLoterias.Select(jl => jl.LoteriaId)).Distinct().ToList();
        var (inicio, fin) = FechaJuegoBoleto.Rango(fechaJuego);
        var publicados = await _db.NumerosGanadores
            .Where(n => n.FechaJuego >= inicio && n.FechaJuego < fin && loteriaIds.Contains(n.LoteriaId))
            .ToListAsync(cancellationToken);

        detalle.Resultados = boleto.Juegos
            .SelectMany(juego => juego.JuegoLoterias.Select(jl =>
            {
                var ganador = publicados.FirstOrDefault(n => n.LoteriaId == jl.LoteriaId)?.Numero?.Trim();
                var apostado = juego.Numero.Trim();
                return new ResultadoLoteriaResponse
                {
                    Loteria = jl.Loteria?.Nombre ?? string.Empty,
                    Numero = apostado,
                    NumeroGanador = ganador,
                    Gano = ganador is not null && string.Equals(ganador, apostado, StringComparison.Ordinal)
                };
            }))
            .ToList();
    }

    private static void AgregarDetalleEntrega(CasoGanador caso, CasoGanadorResponse detalle)
    {
        if (caso.EntregaGanador is not { } entrega)
        {
            return;
        }

        detalle.NombreVendedorEntrega = entrega.NombreVendedor;
        detalle.PersonaQueEntrega = entrega.PersonaQueEntregaNavigation?.NombreCompleto
            ?? caso.ObservadorAsignadoNavigation?.NombreCompleto;
        detalle.FechaEntrega = entrega.FechaEntrega;
        detalle.Evidencias = entrega.Evidencias
            .OrderBy(e => e.TipoEvidencia)
            .Select(e => new EvidenciaEntregaResponse
            {
                EvidenciaId = e.EvidenciaId,
                Tipo = NombreEvidencia(e.TipoEvidencia),
                FechaCaptura = e.FechaCaptura
            })
            .ToList();
    }

    private static string NombreEvidencia(TipoEvidencia tipo) => tipo switch
    {
        TipoEvidencia.TicketConQR => PremioMessages.EvidenciaTicketConQr,
        TipoEvidencia.GanadorConTicket => PremioMessages.EvidenciaGanadorConTicket,
        TipoEvidencia.CedulaReverso => PremioMessages.EvidenciaCedulaReverso,
        _ => PremioMessages.EvidenciaCedula
    };

    public async Task<Result<CasoGanadorResponse>> ReportarAsync(Guid solicitanteId, ReportarCasoGanadorRequest request, CancellationToken cancellationToken)
    {
        var bruto = (request.TicketCode ?? string.Empty).Trim();
        if (bruto.Length == 0)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketRequerido);
        }

        if (bruto.Length > TicketCodeLimits.MaxInputLength)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketDemasiadoLargo);
        }

        var solicitante = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == solicitanteId, cancellationToken);
        if (solicitante is null)
        {
            return Result<CasoGanadorResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        var boletos = _db.Boletos
            .Include(b => b.Venta)
            .ThenInclude(v => v!.Dispositivo)
            .Include(b => b.Venta)
            .ThenInclude(v => v!.Usuario);

        var consulta = await _validacion.ConsultarPorCodigoAsync(bruto, cancellationToken);
        if (!consulta.IsSuccess || consulta.Data?.BoletoId is not Guid boletoId)
        {
            return Result<CasoGanadorResponse>.Fail(consulta.Message, consulta.StatusCode);
        }

        var boleto = await BoletoPorCodigo.BuscarAsync(boletos, _qr, bruto, cancellationToken)
            ?? await boletos.FirstOrDefaultAsync(b => b.BoletoId == boletoId, cancellationToken);
        if (boleto is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketNoEncontrado, 404);
        }

        var codigo = boleto.CodigoPublico;

        if (boleto.EstadoBoleto == EstadoBoleto.PremioEntregado || boleto.EstadoDelPremio == EstadoDelPremio.PremioEntregado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketPremioEntregado);
        }

        var listoParaCobrar = consulta.Data.ResultadoVisual == BoletoMessages.BoletoGanador
            || boleto.EstadoBoleto == EstadoBoleto.Ganador;
        if (!listoParaCobrar)
        {
            return Result<CasoGanadorResponse>.Fail(
                string.IsNullOrWhiteSpace(consulta.Data.Mensaje)
                    ? PremioMessages.BoletoNoEsGanadorParaCaso
                    : consulta.Data.Mensaje);
        }

        if (solicitante.Rol == RolUsuario.Vendedor && boleto.Venta?.UsuarioId != solicitanteId)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketNoEsDelVendedor, 403);
        }

        if (await _db.CasosGanadores.AnyAsync(c => c.BoletoId == boleto.BoletoId, cancellationToken))
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoYaExiste);
        }

        var fotoObligatoria = false;
        var foto = await ResolverFotoAsync(request, fotoObligatoria, cancellationToken);
        if (!foto.IsSuccess)
        {
            return Result<CasoGanadorResponse>.Fail(foto.Message);
        }

        var vendedorId = boleto.Venta?.UsuarioId ?? solicitanteId;
        var caso = new CasoGanador
        {
            CasoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            TicketCode = codigo,
            Estado = EstadoCasoGanador.Reportado,
            FechaReporte = _clock.UtcNow,
            VendedorQueReporto = vendedorId,
            FotoTicketRuta = foto.Data?.Ruta,
            FotoTicketNombre = foto.Data?.Nombre
        };
        _db.CasosGanadores.Add(caso);
        boleto.EstadoDelPremio = EstadoDelPremio.Vigente;
        await _db.SaveChangesAsync(cancellationToken);
        boleto.CasoGanadorId = caso.CasoId;

        var admins = await _db.Usuarios
            .Where(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo && !u.EstadoBloqueado)
            .Select(u => u.UsuarioId)
            .ToListAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await _notificaciones.CrearParaAsync(admins, "CasoGanador", $"Se reportó el ticket {codigo} como ganador.", cancellationToken);
        var creado = await Query().FirstAsync(c => c.CasoId == caso.CasoId, cancellationToken);
        return Result<CasoGanadorResponse>.Created(Map(creado), SuccessMessages.CasoGanadorReportado);
    }

    public async Task<Result<DescargaAdjuntoResponse>> ObtenerFotoAsync(Guid casoId, CancellationToken cancellationToken)
    {
        var caso = await _db.CasosGanadores.FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<DescargaAdjuntoResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        if (string.IsNullOrWhiteSpace(caso.FotoTicketRuta))
        {
            return Result<DescargaAdjuntoResponse>.Fail(PremioMessages.FotoQrRequerida, 404);
        }

        var stream = await _files.OpenReadAsync(caso.FotoTicketRuta, cancellationToken);
        if (stream is null)
        {
            return Result<DescargaAdjuntoResponse>.Fail(PremioMessages.FotoQrRequerida, 404);
        }

        return Result<DescargaAdjuntoResponse>.Ok(new DescargaAdjuntoResponse
        {
            NombreArchivo = caso.FotoTicketNombre ?? "ticket.jpg",
            Contenido = stream
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<CasoGanadorResponse>> ValidarAsync(Guid casoId, Guid adminId, CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        if (caso.Estado != EstadoCasoGanador.Reportado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        caso.Estado = EstadoCasoGanador.Validado;
        caso.FechaValidacionAdmin = _clock.UtcNow;
        caso.AdminQueValido = adminId;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.CasoGanadorValidado);
    }

    public async Task<Result<CasoGanadorResponse>> RechazarAsync(Guid casoId, Guid adminId, CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        if (caso.Estado != EstadoCasoGanador.Reportado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        caso.Estado = EstadoCasoGanador.Rechazado;
        caso.FechaValidacionAdmin = _clock.UtcNow;
        caso.AdminQueValido = adminId;
        if (caso.Boleto is not null)
        {
            caso.Boleto.EstadoDelPremio = EstadoDelPremio.Rechazado;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.CasoGanadorRechazado);
    }

    public async Task<Result<CasoGanadorResponse>> AsignarAsync(Guid casoId, Guid adminId, AsignarObservadorRequest request, CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        if (caso.Estado is EstadoCasoGanador.Registrado or EstadoCasoGanador.Rechazado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        if (caso.Estado is not (EstadoCasoGanador.Validado or EstadoCasoGanador.Asignado or EstadoCasoGanador.EnProceso))
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        var observador = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == request.ObservadorId, cancellationToken);
        if (observador is null
            || observador.Rol != RolUsuario.Observador
            || observador.Estado != EstadoUsuario.Activo
            || observador.EstadoBloqueado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.ObservadorInvalido);
        }

        caso.ObservadorAsignado = observador.UsuarioId;
        caso.ObservadorAsignadoNavigation = observador;
        caso.AdminQueAsigno = adminId;
        caso.FechaAsignacion = _clock.UtcNow;
        if (caso.Estado == EstadoCasoGanador.Validado)
        {
            caso.Estado = EstadoCasoGanador.Asignado;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _notificaciones.CrearParaAsync(
            [observador.UsuarioId],
            "CasoAsignado",
            $"Se te asignó el caso del ticket {caso.TicketCode.Trim()}.",
            cancellationToken);
        return Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.CasoGanadorAsignado);
    }

    public async Task<Result<CasoGanadorResponse>> IniciarRegistroAsync(Guid casoId, Guid observadorId, CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null || caso.ObservadorAsignado != observadorId)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoAsignado, 404);
        }

        if (caso.Estado == EstadoCasoGanador.EnProceso)
        {
            return Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.OperacionExitosa);
        }

        if (caso.Estado != EstadoCasoGanador.Asignado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        caso.Estado = EstadoCasoGanador.EnProceso;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<CasoGanadorResponse>> RegistrarEntregaAsync(
        Guid casoId,
        Guid observadorId,
        RegistrarEntregaPremioRequest request,
        CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null || caso.ObservadorAsignado != observadorId)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoAsignado, 404);
        }

        if (caso.Estado is not (EstadoCasoGanador.Asignado or EstadoCasoGanador.EnProceso))
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        if (!DatosEntregaCompletos(request))
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.DatosEntregaIncompletos);
        }

        if (request.FotoTicketConQr is null
            || request.FotoGanadorConTicket is null
            || request.FotoCedulaFrente is null
            || request.FotoCedulaReverso is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.FotosObligatorias);
        }

        var fotoTicket = await GuardarEvidenciaAsync(request.FotoTicketConQr, cancellationToken);
        if (!fotoTicket.IsSuccess)
        {
            return Result<CasoGanadorResponse>.Fail(fotoTicket.Message);
        }

        var fotoGanador = await GuardarEvidenciaAsync(request.FotoGanadorConTicket, cancellationToken);
        if (!fotoGanador.IsSuccess)
        {
            return Result<CasoGanadorResponse>.Fail(fotoGanador.Message);
        }

        var fotoCedulaFrente = await GuardarEvidenciaAsync(request.FotoCedulaFrente, cancellationToken);
        if (!fotoCedulaFrente.IsSuccess)
        {
            return Result<CasoGanadorResponse>.Fail(fotoCedulaFrente.Message);
        }

        var fotoCedulaReverso = await GuardarEvidenciaAsync(request.FotoCedulaReverso, cancellationToken);
        if (!fotoCedulaReverso.IsSuccess)
        {
            return Result<CasoGanadorResponse>.Fail(fotoCedulaReverso.Message);
        }

        var ahora = _clock.UtcNow;
        var entrega = new EntregaGanador
        {
            EntregaId = Guid.NewGuid(),
            CasoId = caso.CasoId,
            NombreGanador = request.NombreGanador.Trim(),
            ApellidoGanador = request.ApellidoGanador.Trim(),
            NumeroContacto = request.NumeroContacto.Trim(),
            LugarGano = request.LugarGano.Trim(),
            NombreVendedor = caso.VendedorQueReportoNavigation?.NombreCompleto
                ?? caso.Boleto?.Venta?.Usuario?.NombreCompleto
                ?? string.Empty,
            ValorTotalGanado = request.ValorTotalGanado,
            PersonaQueEntrega = observadorId,
            FechaEntrega = ahora
        };

        entrega.Evidencias.Add(new EvidenciaGanador
        {
            EvidenciaId = Guid.NewGuid(),
            EntregaId = entrega.EntregaId,
            TipoEvidencia = TipoEvidencia.TicketConQR,
            RutaImagen = fotoTicket.Data!.Ruta,
            FechaCaptura = ahora
        });
        entrega.Evidencias.Add(new EvidenciaGanador
        {
            EvidenciaId = Guid.NewGuid(),
            EntregaId = entrega.EntregaId,
            TipoEvidencia = TipoEvidencia.GanadorConTicket,
            RutaImagen = fotoGanador.Data!.Ruta,
            FechaCaptura = ahora
        });
        entrega.Evidencias.Add(new EvidenciaGanador
        {
            EvidenciaId = Guid.NewGuid(),
            EntregaId = entrega.EntregaId,
            TipoEvidencia = TipoEvidencia.CedulaIdentidad,
            RutaImagen = fotoCedulaFrente.Data!.Ruta,
            FechaCaptura = ahora
        });
        entrega.Evidencias.Add(new EvidenciaGanador
        {
            EvidenciaId = Guid.NewGuid(),
            EntregaId = entrega.EntregaId,
            TipoEvidencia = TipoEvidencia.CedulaReverso,
            RutaImagen = fotoCedulaReverso.Data!.Ruta,
            FechaCaptura = ahora
        });

        _db.EntregasGanadores.Add(entrega);
        caso.Estado = EstadoCasoGanador.Registrado;
        caso.FechaRegistro = ahora;
        caso.EntregaGanador = entrega;
        if (caso.Boleto is not null)
        {
            caso.Boleto.EstadoBoleto = EstadoBoleto.PremioEntregado;
            caso.Boleto.EstadoDelPremio = EstadoDelPremio.PremioEntregado;
            caso.Boleto.FechaEntregaPremio = ahora;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var actualizado = await Query().FirstAsync(c => c.CasoId == caso.CasoId, cancellationToken);
        return Result<CasoGanadorResponse>.Ok(Map(actualizado), SuccessMessages.EntregaPremioRegistrada);
    }

    private static bool DatosEntregaCompletos(RegistrarEntregaPremioRequest request) =>
        !string.IsNullOrWhiteSpace(request.NombreGanador)
        && !string.IsNullOrWhiteSpace(request.ApellidoGanador)
        && !string.IsNullOrWhiteSpace(request.NumeroContacto)
        && !string.IsNullOrWhiteSpace(request.LugarGano)
        && request.ValorTotalGanado > 0;

    private async Task<Result<FotoGuardada>> GuardarEvidenciaAsync(EvidenciaFotoRequest request, CancellationToken cancellationToken)
    {
        var lectura = FotoEvidencia.Leer(request.NombreArchivo, request.ContenidoBase64);
        if (!lectura.IsSuccess)
        {
            return Result<FotoGuardada>.Fail(lectura.Message);
        }

        var nombre = ChatAdjunto.NombreSeguro(request.NombreArchivo);
        await using var stream = new MemoryStream(lectura.Data!);
        var ruta = await _files.SaveAsync(stream, nombre, cancellationToken);
        return Result<FotoGuardada>.Ok(new FotoGuardada(ruta, nombre), SuccessMessages.OperacionExitosa);
    }

    private IQueryable<CasoGanador> Query() =>
        _db.CasosGanadores
            .Include(c => c.Boleto)
            .ThenInclude(b => b!.Venta)
            .ThenInclude(v => v!.Dispositivo)
            .Include(c => c.Boleto)
            .ThenInclude(b => b!.Venta)
            .ThenInclude(v => v!.Usuario)
            .Include(c => c.VendedorQueReportoNavigation)
            .Include(c => c.ObservadorAsignadoNavigation)
            .Include(c => c.EntregaGanador);

    private static CasoGanadorResponse Map(CasoGanador caso)
    {
        var entrega = caso.EntregaGanador;
        return new CasoGanadorResponse
        {
            CasoId = caso.CasoId,
            BoletoId = caso.BoletoId,
            Ticket = caso.TicketCode.Trim(),
            Vendedor = caso.VendedorQueReportoNavigation?.NombreCompleto
                ?? caso.Boleto?.Venta?.Usuario?.NombreCompleto
                ?? string.Empty,
            Pda = caso.Boleto?.Venta?.Dispositivo?.CodigoDispositivo ?? "No aplica",
            Estado = EstadoVisible(caso),
            Observador = string.IsNullOrWhiteSpace(caso.ObservadorAsignadoNavigation?.NombreCompleto)
                ? "Pendiente"
                : caso.ObservadorAsignadoNavigation!.NombreCompleto,
            FechaReporte = caso.FechaReporte,
            FechaValidacion = caso.FechaValidacionAdmin,
            FechaAsignacion = caso.FechaAsignacion,
            FechaRegistro = caso.FechaRegistro,
            NombreGanador = entrega?.NombreGanador,
            ApellidoGanador = entrega?.ApellidoGanador,
            NumeroContacto = entrega?.NumeroContacto,
            LugarGano = entrega?.LugarGano,
            ValorTotalGanado = entrega?.ValorTotalGanado,
            TieneFoto = !string.IsNullOrWhiteSpace(caso.FotoTicketRuta),
            NombreFoto = caso.FotoTicketNombre
        };
    }

    private async Task<Result<FotoGuardada?>> ResolverFotoAsync(
        ReportarCasoGanadorRequest request,
        bool obligatoria,
        CancellationToken cancellationToken)
    {
        var hay = !string.IsNullOrWhiteSpace(request.ContenidoBase64) || !string.IsNullOrWhiteSpace(request.NombreArchivo);
        if (!hay)
        {
            return obligatoria
                ? Result<FotoGuardada?>.Fail(PremioMessages.FotoQrRequerida)
                : Result<FotoGuardada?>.Ok(null, SuccessMessages.OperacionExitosa);
        }

        var lectura = FotoEvidencia.Leer(request.NombreArchivo, request.ContenidoBase64);
        if (!lectura.IsSuccess)
        {
            return Result<FotoGuardada?>.Fail(lectura.Message);
        }

        var nombre = ChatAdjunto.NombreSeguro(request.NombreArchivo);
        await using var stream = new MemoryStream(lectura.Data!);
        var ruta = await _files.SaveAsync(stream, nombre, cancellationToken);
        return Result<FotoGuardada?>.Ok(new FotoGuardada(ruta, nombre), SuccessMessages.OperacionExitosa);
    }

    private sealed record FotoGuardada(string Ruta, string Nombre);

    private static string EstadoVisible(CasoGanador caso)
    {
        if (caso.Boleto?.EstadoBoleto == EstadoBoleto.PremioEntregado || caso.Estado == EstadoCasoGanador.Registrado)
        {
            return "Premio entregado";
        }

        return caso.Estado == EstadoCasoGanador.EnProceso ? "En proceso" : caso.Estado.ToString();
    }
}
