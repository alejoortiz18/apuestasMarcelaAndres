using System.Collections.Concurrent;
using NewRich.Application.Abstractions;
using NewRich.Constants;

namespace NewRich.Application.Services;

public sealed class ConfirmacionAccionMemoria : IConfirmacionAccionStore
{
    private readonly IClock? _reloj;
    private readonly ConcurrentDictionary<string, Entrada> _tokens = new();

    public ConfirmacionAccionMemoria()
    {
    }

    public ConfirmacionAccionMemoria(IClock reloj)
    {
        _reloj = reloj;
    }

    public string Emitir(Guid usuarioId, string accion, int usos = 1, TimeSpan? vigencia = null)
    {
        var cantidad = Math.Clamp(usos < 1 ? 1 : usos, 1, ConfirmacionAccion.MaxUsos);
        var duracion = vigencia is { } pedida && pedida > TimeSpan.Zero
            ? pedida
            : ConfirmacionAccion.Vigencia;
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new Entrada(usuarioId, accion, Ahora().Add(duracion), cantidad);
        return token;
    }

    public bool Consumir(string token, Guid usuarioId, string accion)
    {
        if (string.IsNullOrWhiteSpace(token) || !_tokens.TryGetValue(token, out var entrada))
        {
            return false;
        }

        if (entrada.UsuarioId != usuarioId
            || !string.Equals(entrada.Accion, accion, StringComparison.Ordinal))
        {
            return false;
        }

        if (entrada.Expira < Ahora())
        {
            _tokens.TryRemove(token, out _);
            return false;
        }

        if (entrada.UsosRestantes <= 1)
        {
            return _tokens.TryRemove(token, out _);
        }

        return _tokens.TryUpdate(token, entrada with { UsosRestantes = entrada.UsosRestantes - 1 }, entrada);
    }

    public Task<string> EmitirAsync(Guid usuarioId, string accion, int usos = 1, TimeSpan? vigencia = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Emitir(usuarioId, accion, usos, vigencia));

    public Task<bool> ConsumirAsync(string token, Guid usuarioId, string accion, CancellationToken cancellationToken = default) =>
        Task.FromResult(Consumir(token, usuarioId, accion));

    private DateTime Ahora() => _reloj?.UtcNow ?? DateTime.UtcNow;

    private sealed record Entrada(Guid UsuarioId, string Accion, DateTime Expira, int UsosRestantes);
}
