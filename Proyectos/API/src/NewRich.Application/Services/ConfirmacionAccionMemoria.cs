using System.Collections.Concurrent;
using NewRich.Application.Abstractions;
using NewRich.Constants;

namespace NewRich.Application.Services;

public sealed class ConfirmacionAccionMemoria : IConfirmacionAccionStore
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<string, Entrada> _tokens = new();

    public string Emitir(Guid usuarioId, string accion, int usos = 1)
    {
        var cantidad = Math.Clamp(usos < 1 ? 1 : usos, 1, ConfirmacionAccion.MaxUsos);
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new Entrada(usuarioId, accion, DateTime.UtcNow.Add(Vigencia), cantidad);
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

        if (entrada.Expira < DateTime.UtcNow)
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

    private sealed record Entrada(Guid UsuarioId, string Accion, DateTime Expira, int UsosRestantes);
}
