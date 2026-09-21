using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Llaves;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface ILlaveAdministradorService
{
    Task<Result<LlaveAdministradorEstadoResponse>> EstadoAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<UsuarioResponseMini>>> AdministradoresAsync(CancellationToken cancellationToken);
    Task<Result<GenerarLlaveAdministradorResponse>> GenerarAsync(GenerarLlaveAdministradorRequest request, CancellationToken cancellationToken);
}

public sealed class UsuarioResponseMini
{
    public Guid UsuarioId { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string NombreUsuario { get; init; } = string.Empty;
}

public sealed class LlaveAdministradorService : ILlaveAdministradorService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public LlaveAdministradorService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<LlaveAdministradorEstadoResponse>> EstadoAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null || usuario.Rol != RolUsuario.Administrador)
        {
            return Result<LlaveAdministradorEstadoResponse>.Fail(LlaveMessages.UsuarioDebeSerAdministrador);
        }

        var activa = await _db.LlavesAdministrador
            .Where(l => l.UsuarioId == usuarioId && l.Estado == EstadoLlaveAdministrador.Activa)
            .OrderByDescending(l => l.FechaActivacion)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<LlaveAdministradorEstadoResponse>.Ok(new LlaveAdministradorEstadoResponse
        {
            UsuarioId = usuarioId,
            TieneLlaveActiva = activa is not null,
            Codigo = activa?.Codigo,
            Estado = activa?.Estado
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IReadOnlyList<UsuarioResponseMini>>> AdministradoresAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Usuarios
            .Where(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo)
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new UsuarioResponseMini
            {
                UsuarioId = u.UsuarioId,
                NombreCompleto = u.NombreCompleto,
                NombreUsuario = u.NombreUsuario
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UsuarioResponseMini>>.Ok(items, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<GenerarLlaveAdministradorResponse>> GenerarAsync(GenerarLlaveAdministradorRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SerialUsb) || string.IsNullOrWhiteSpace(request.Volumen))
        {
            return Result<GenerarLlaveAdministradorResponse>.Fail(LlaveMessages.UsbNoDetectada);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == request.UsuarioId, cancellationToken);
        if (usuario is null || usuario.Rol != RolUsuario.Administrador)
        {
            return Result<GenerarLlaveAdministradorResponse>.Fail(LlaveMessages.UsuarioDebeSerAdministrador);
        }

        var activas = await _db.LlavesAdministrador
            .Where(l => l.UsuarioId == request.UsuarioId && l.Estado == EstadoLlaveAdministrador.Activa)
            .ToListAsync(cancellationToken);

        if (activas.Count > 0 && !request.ConfirmarReemplazo)
        {
            return Result<GenerarLlaveAdministradorResponse>.Fail(LlaveMessages.YaTieneLlaveActiva, 409);
        }

        foreach (var anterior in activas)
        {
            anterior.Estado = EstadoLlaveAdministrador.Revocada;
            anterior.FechaRevocacion = _clock.UtcNow;
            anterior.MotivoRevocacion = "Reemplazo por nueva llave";
        }

        var codigo = LlaveUsbCriptografia.NuevoCodigo();
        var material = LlaveUsbCriptografia.Generar(codigo, request.SerialUsb.Trim(), request.Volumen.Trim());
        var ahora = _clock.UtcNow;
        var llave = new LlaveAdministrador
        {
            LlaveId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            Codigo = material.Codigo,
            Estado = EstadoLlaveAdministrador.Activa,
            ClavePublica = material.ClavePublica,
            HuellaDispositivo = material.Huella,
            FechaCreacion = ahora,
            FechaActivacion = ahora
        };
        _db.LlavesAdministrador.Add(llave);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<GenerarLlaveAdministradorResponse>.Ok(new GenerarLlaveAdministradorResponse
        {
            LlaveId = llave.LlaveId,
            UsuarioId = usuario.UsuarioId,
            Codigo = material.Codigo,
            ClavePublica = material.ClavePublica,
            SecretoEnvuelto = material.SecretoEnvuelto,
            HuellaDispositivo = material.Huella,
            Reemplazo = activas.Count > 0
        }, SuccessMessages.LlaveGenerada);
    }
}
