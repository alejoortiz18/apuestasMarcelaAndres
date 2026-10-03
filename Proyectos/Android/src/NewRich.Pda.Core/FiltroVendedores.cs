using System.Globalization;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;

namespace NewRich.Pda.Core;

public enum EstadoFiltroVendedor
{
    Todos,
    Bloqueados,
    SinBloqueo
}

public sealed record OpcionGrupoVendedor(string Nombre, Guid? GrupoId, bool SinGrupo);

/// <summary>Filtros de la pantalla del observador para desbloquear o restablecer vendedores.</summary>
public static class FiltroVendedores
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    private static readonly CompareInfo Comparador = Cultura.CompareInfo;
    private static readonly StringComparer OrdenNombre = StringComparer.Create(Cultura, true);
    private const CompareOptions SinTildesNiMayusculas = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public static IReadOnlyList<UsuarioResponse> Aplicar(
        IEnumerable<UsuarioResponse> usuarios,
        string? texto,
        OpcionGrupoVendedor? grupo,
        EstadoFiltroVendedor estado)
    {
        var buscado = texto?.Trim() ?? string.Empty;
        return usuarios
            .Where(u => u.Rol == RolUsuario.Vendedor)
            .Where(u => buscado.Length == 0 || Coincide(u, buscado))
            .Where(u => grupo is null || (grupo.SinGrupo ? u.GrupoId is null : grupo.GrupoId is null || u.GrupoId == grupo.GrupoId))
            .Where(u => estado switch
            {
                EstadoFiltroVendedor.Bloqueados => u.EstadoBloqueado,
                EstadoFiltroVendedor.SinBloqueo => !u.EstadoBloqueado,
                _ => true
            })
            .OrderBy(u => u.NombreCompleto, OrdenNombre)
            .ToList();
    }

    public static IReadOnlyList<OpcionGrupoVendedor> Grupos(IEnumerable<UsuarioResponse> usuarios)
    {
        var vendedores = usuarios.Where(u => u.Rol == RolUsuario.Vendedor).ToList();
        var opciones = new List<OpcionGrupoVendedor> { new(PdaTexts.TodosLosGrupos, null, false) };
        opciones.AddRange(vendedores
            .Where(u => u.GrupoId is not null)
            .GroupBy(u => u.GrupoId!.Value)
            .Select(g => new OpcionGrupoVendedor(g.First().GrupoNombre ?? string.Empty, g.Key, false))
            .OrderBy(o => o.Nombre, OrdenNombre));
        if (vendedores.Any(u => u.GrupoId is null))
        {
            opciones.Add(new OpcionGrupoVendedor(PdaTexts.SinGrupo, null, true));
        }

        return opciones;
    }

    public static string Resumen(int mostrados, int total) =>
        string.Format(Cultura, PdaTexts.MostrandoVendedores, mostrados, total);

    private static bool Coincide(UsuarioResponse usuario, string buscado) =>
        Contiene(usuario.NombreCompleto, buscado)
        || Contiene(usuario.Usuario, buscado)
        || Contiene(usuario.Documento, buscado)
        || Contiene(usuario.Alias, buscado);

    private static bool Contiene(string? valor, string buscado) =>
        !string.IsNullOrEmpty(valor) && Comparador.IndexOf(valor, buscado, SinTildesNiMayusculas) >= 0;
}
