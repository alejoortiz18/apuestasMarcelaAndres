using System.Globalization;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Informes;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Constants;
using NewRich.Domain.Enums;

namespace NewRich.Application.Informes;

/// <summary>
/// Arma el informe de personas con los mismos conteos que el administrador:
/// personal incluido, vendedores, grupos, estado, distribución y detalle.
/// </summary>
public static class InformePersonasLectura
{
    private static readonly StringComparer Texto = StringComparer.Create(
        CultureInfo.GetCultureInfo("es-CO"),
        CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);

    public static InformePersonasObservadorResponse Armar(
        IReadOnlyList<UsuarioResponse> usuarios,
        IReadOnlyList<GrupoResponse> grupos,
        Guid? grupoId)
    {
        var nombres = NombresGrupo(grupos, usuarios);
        var incluidos = grupoId is null
            ? usuarios.ToList()
            : usuarios.Where(u => u.GrupoId == grupoId).ToList();
        var vendedores = incluidos.Where(u => u.Rol == RolUsuario.Vendedor).ToList();
        var activos = incluidos.Count(u => u.Estado == EstadoUsuario.Activo);
        var porTipo = incluidos
            .GroupBy(u => u.Rol)
            .Select(g => (Texto: NombreRol(g.Key), Cantidad: g.Count()))
            .ToList();
        var porGrupo = vendedores
            .GroupBy(u => ClaveGrupo(u))
            .Select(g => (Texto: nombres.GetValueOrDefault(g.Key, g.Key), Cantidad: g.Count()))
            .ToList();

        return new InformePersonasObservadorResponse
        {
            TotalRegistrados = usuarios.Count,
            Incluidos = incluidos.Count,
            PorcentajeIncluidos = Porcentaje(incluidos.Count, usuarios.Count),
            Vendedores = vendedores.Count,
            PorcentajeVendedores = Porcentaje(vendedores.Count, incluidos.Count),
            GruposConVendedores = vendedores.Where(u => u.GrupoId is not null).Select(u => u.GrupoId).Distinct().Count(),
            VendedoresSinGrupo = vendedores.Count(u => u.GrupoId is null),
            Activos = activos,
            Inactivos = incluidos.Count - activos,
            PorcentajeActivos = Porcentaje(activos, incluidos.Count),
            PorTipo = Segmentos(porTipo, incluidos.Count),
            PorGrupo = Segmentos(porGrupo, vendedores.Count),
            Personal = incluidos
                .Select(u => Persona(u, nombres))
                .OrderBy(p => p.Nombre, Texto)
                .ToList()
        };
    }

    private static List<BarraInformeResponse> Segmentos(IReadOnlyList<(string Texto, int Cantidad)> grupos, int total)
    {
        var mayor = grupos.Count == 0 ? 0 : grupos.Max(g => g.Cantidad);
        return grupos
            .OrderByDescending(g => g.Cantidad)
            .ThenBy(g => g.Texto, Texto)
            .Select(g => new BarraInformeResponse
            {
                Texto = g.Texto,
                Cantidad = g.Cantidad,
                Valor = g.Cantidad,
                Porcentaje = Porcentaje(g.Cantidad, total),
                Ancho = Porcentaje(g.Cantidad, mayor)
            })
            .ToList();
    }

    private static decimal Porcentaje(int parte, int total) =>
        total == 0 ? 0 : Math.Round(parte * 100m / total, 1, MidpointRounding.AwayFromZero);

    private static PersonaInformeResponse Persona(UsuarioResponse usuario, IReadOnlyDictionary<string, string> nombres) => new()
    {
        Nombre = usuario.NombreCompleto,
        Usuario = usuario.Usuario,
        Documento = usuario.Documento ?? string.Empty,
        Tipo = NombreRol(usuario.Rol),
        Grupo = nombres.GetValueOrDefault(ClaveGrupo(usuario), string.Empty),
        Estado = usuario.Estado.ToString()
    };

    private static Dictionary<string, string> NombresGrupo(IEnumerable<GrupoResponse> grupos, IEnumerable<UsuarioResponse> usuarios)
    {
        var nombres = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var grupo in grupos)
        {
            nombres[$"g:{grupo.GrupoId}"] = grupo.Nombre;
        }

        foreach (var usuario in usuarios)
        {
            if (usuario.GrupoId is not null)
            {
                nombres.TryAdd(ClaveGrupo(usuario), usuario.GrupoNombre ?? string.Empty);
            }
            else
            {
                nombres.TryAdd(ClaveGrupo(usuario), string.Format(InformeTextos.RolSinGrupo, NombreRol(usuario.Rol)));
            }
        }

        return nombres;
    }

    private static string ClaveGrupo(UsuarioResponse usuario) =>
        usuario.GrupoId is { } grupoId ? $"g:{grupoId}" : $"r:{(int)usuario.Rol}";

    private static string NombreRol(RolUsuario rol) =>
        rol == RolUsuario.VendedorWeb ? InformeTextos.RolVendedorWeb : rol.ToString();
}
