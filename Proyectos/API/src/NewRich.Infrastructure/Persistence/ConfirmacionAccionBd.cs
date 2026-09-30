using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewRich.Application.Abstractions;
using NewRich.Constants;
using NewRich.Domain.Entities;

namespace NewRich.Infrastructure.Persistence;

public sealed class ConfirmacionAccionBd : IConfirmacionAccionStore
{
    private readonly IServiceScopeFactory _alcances;
    private readonly IClock _reloj;

    public ConfirmacionAccionBd(IServiceScopeFactory alcances, IClock reloj)
    {
        _alcances = alcances;
        _reloj = reloj;
    }

    public async Task<string> EmitirAsync(Guid usuarioId, string accion, int usos = 1, TimeSpan? vigencia = null, CancellationToken cancellationToken = default)
    {
        var cantidad = Math.Clamp(usos < 1 ? 1 : usos, 1, ConfirmacionAccion.MaxUsos);
        var duracion = vigencia is { } pedida && pedida > TimeSpan.Zero
            ? pedida
            : ConfirmacionAccion.Vigencia;
        var token = Guid.NewGuid().ToString("N");
        using var alcance = _alcances.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<NewRichDbContext>();
        await db.AsegurarEsquemaConfirmacionAsync(cancellationToken);
        db.ConfirmacionesAccion.Add(new ConfirmacionAccionPendiente
        {
            Token = token,
            UsuarioId = usuarioId,
            Accion = accion,
            Expira = _reloj.UtcNow.Add(duracion),
            UsosRestantes = cantidad
        });
        await db.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<bool> ConsumirAsync(string token, Guid usuarioId, string accion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        using var alcance = _alcances.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<NewRichDbContext>();
        await db.AsegurarEsquemaConfirmacionAsync(cancellationToken);
        var ahora = _reloj.UtcNow;
        if (db.Database.IsRelational())
        {
            var afectados = await db.ConfirmacionesAccion
                .Where(x => x.Token == token && x.UsuarioId == usuarioId && x.Accion == accion && x.Expira >= ahora && x.UsosRestantes > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsosRestantes, x => x.UsosRestantes - 1), cancellationToken);
            if (afectados != 1)
            {
                return false;
            }

            await db.ConfirmacionesAccion
                .Where(x => x.Token == token && x.UsosRestantes <= 0)
                .ExecuteDeleteAsync(cancellationToken);
            return true;
        }

        var fila = await db.ConfirmacionesAccion.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
        if (fila is null || fila.UsuarioId != usuarioId || fila.Accion != accion || fila.Expira < ahora || fila.UsosRestantes < 1)
        {
            return false;
        }

        fila.UsosRestantes--;
        if (fila.UsosRestantes <= 0)
        {
            db.ConfirmacionesAccion.Remove(fila);
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
