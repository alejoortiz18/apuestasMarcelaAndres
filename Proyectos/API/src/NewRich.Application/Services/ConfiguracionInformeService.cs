using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Informes;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IConfiguracionInformeService
{
    Task<Result<IReadOnlyList<ConfiguracionInformeResponse>>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<ConfiguracionInformeResponse>> GuardarAsync(Guid usuarioId, string categoria, GuardarConfiguracionInformeRequest request, CancellationToken cancellationToken);
    Task<Result> QuitarAsync(Guid usuarioId, string categoria, CancellationToken cancellationToken);
}

public sealed class ConfiguracionInformeService : IConfiguracionInformeService
{
    private readonly INewRichDbContext _db;

    public ConfiguracionInformeService(INewRichDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ConfiguracionInformeResponse>>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var filas = await _db.ConfiguracionesInforme.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.Categoria)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ConfiguracionInformeResponse>>.Ok(filas.Select(Mapa).ToList(), InformeMessages.Consultado);
    }

    public async Task<Result<ConfiguracionInformeResponse>> GuardarAsync(Guid usuarioId, string categoria, GuardarConfiguracionInformeRequest request, CancellationToken cancellationToken)
    {
        if (!CategoriasInforme.Habilitadas.Contains(categoria))
        {
            return Result<ConfiguracionInformeResponse>.Fail(InformeMessages.CategoriaNoHabilitada);
        }

        if (!EsObjetoJson(request.Filtros))
        {
            return Result<ConfiguracionInformeResponse>.Fail(InformeMessages.FiltrosNoValidos);
        }

        var fila = await _db.ConfiguracionesInforme
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Categoria == categoria, cancellationToken);
        if (fila is null)
        {
            fila = new ConfiguracionInforme
            {
                ConfiguracionId = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Categoria = categoria
            };
            _db.ConfiguracionesInforme.Add(fila);
        }

        fila.Filtros = request.Filtros;
        fila.FechaActualizacionUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ConfiguracionInformeResponse>.Ok(Mapa(fila), InformeMessages.Guardado);
    }

    public async Task<Result> QuitarAsync(Guid usuarioId, string categoria, CancellationToken cancellationToken)
    {
        var fila = await _db.ConfiguracionesInforme
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Categoria == categoria, cancellationToken);
        if (fila is not null)
        {
            _db.ConfiguracionesInforme.Remove(fila);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok(InformeMessages.Quitado);
    }

    private static bool EsObjetoJson(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > CategoriasInforme.FiltrosLongitudMaxima)
        {
            return false;
        }

        try
        {
            using var documento = JsonDocument.Parse(texto);
            return documento.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static ConfiguracionInformeResponse Mapa(ConfiguracionInforme fila) => new()
    {
        Categoria = fila.Categoria,
        Filtros = fila.Filtros,
        FechaActualizacionUtc = fila.FechaActualizacionUtc
    };
}
