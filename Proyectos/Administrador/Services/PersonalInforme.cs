using System.Text.Json;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Services;

public static class PersonalInforme
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private const int NombresVisibles = 3;

    public static string ClaveGrupo(UsuarioResponse usuario) =>
        usuario.GrupoId is { } grupoId ? $"g:{grupoId}" : $"r:{(int)usuario.Rol}";

    public static FiltrosVendedoresInforme LeerFiltros(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new FiltrosVendedoresInforme();
        try
        {
            return JsonSerializer.Deserialize<FiltrosVendedoresInforme>(json, Json) ?? new FiltrosVendedoresInforme();
        }
        catch (JsonException)
        {
            return new FiltrosVendedoresInforme();
        }
    }

    public static IReadOnlyList<UsuarioResponse> Aplicar(IEnumerable<UsuarioResponse> usuarios, FiltrosVendedoresInforme filtros)
    {
        var grupos = filtros.Grupos?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var elegidos = filtros.Usuarios?.ToHashSet();
        var tipos = filtros.Tipos?.ToHashSet();
        return usuarios
            .Where(u => grupos is null || grupos.Contains(ClaveGrupo(u)))
            .Where(u => elegidos is null || elegidos.Contains(u.UsuarioId))
            .Where(u => tipos is null || tipos.Contains(u.Rol))
            .ToList();
    }

    public static IReadOnlyList<OpcionInforme> OpcionesGrupo(IEnumerable<GrupoResponse> grupos, IEnumerable<UsuarioResponse> usuarios)
    {
        var reales = grupos
            .OrderBy(g => g.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new OpcionInforme { Valor = $"g:{g.GrupoId}", Texto = g.Nombre });
        var porRol = usuarios
            .Where(u => u.GrupoId is null)
            .Select(u => u.Rol)
            .Distinct()
            .OrderBy(r => (int)r)
            .Select(r => new OpcionInforme { Valor = $"r:{(int)r}", Texto = RolSinGrupo(r) });
        return reales.Concat(porRol).ToList();
    }

    public static IReadOnlyList<OpcionInforme> OpcionesUsuario(IEnumerable<UsuarioResponse> usuarios) => usuarios
        .OrderBy(u => u.NombreCompleto, StringComparer.CurrentCultureIgnoreCase)
        .Select(u => new OpcionInforme
        {
            Valor = u.UsuarioId.ToString(),
            Texto = u.NombreCompleto,
            Detalle = $"{u.Usuario} · {RolUi.Nombre(u.Rol)}"
        })
        .ToList();

    public static IReadOnlyList<OpcionInforme> OpcionesTipo() => Enum.GetValues<RolUsuario>()
        .OrderBy(r => (int)r)
        .Select(r => new OpcionInforme { Valor = ((int)r).ToString(), Texto = RolUi.Nombre(r) })
        .ToList();

    public static ResumenPersonalInforme Resumir(
        IReadOnlyList<UsuarioResponse> usuarios,
        IReadOnlyList<GrupoResponse> grupos,
        FiltrosVendedoresInforme filtros)
    {
        var incluidos = Aplicar(usuarios, filtros);
        var nombresGrupo = NombresGrupo(grupos, usuarios);
        var tipos = filtros.Tipos?.Distinct().ToList()
            ?? incluidos.Select(u => u.Rol).Distinct().ToList();
        var orden = OpcionesGrupo(grupos, usuarios).Select((o, i) => (o.Valor, i)).ToDictionary(x => x.Valor, x => x.i);

        return new ResumenPersonalInforme
        {
            TotalRegistrados = usuarios.Count,
            Cumplen = incluidos.Count,
            PorTipo = tipos
                .OrderBy(r => (int)r)
                .Select(r => new ConteoInforme { Texto = RolUi.Nombre(r), Cantidad = incluidos.Count(u => u.Rol == r) })
                .ToList(),
            VendedoresPorGrupo = incluidos
                .Where(u => u.Rol == RolUsuario.Vendedor)
                .GroupBy(ClaveGrupo)
                .OrderBy(g => orden.GetValueOrDefault(g.Key, int.MaxValue))
                .Select(g => new ConteoInforme { Texto = nombresGrupo.GetValueOrDefault(g.Key, g.Key), Cantidad = g.Count() })
                .ToList(),
            Usuarios = incluidos
                .OrderBy(u => u.NombreCompleto, StringComparer.CurrentCultureIgnoreCase)
                .Select(u => Persona(u, nombresGrupo))
                .ToList()
        };
    }

    public static InformeVendedoresViewModel Informe(
        IReadOnlyList<UsuarioResponse> usuarios,
        IReadOnlyList<GrupoResponse> grupos,
        FiltrosVendedoresInforme filtros,
        DateTime corte,
        string? orden,
        string? direccion,
        int page,
        int pageSize)
    {
        var incluidos = Aplicar(usuarios, filtros);
        var nombresGrupo = NombresGrupo(grupos, usuarios);
        var vendedores = incluidos.Where(u => u.Rol == RolUsuario.Vendedor).ToList();
        var activos = incluidos.Count(u => u.Estado == EstadoUsuario.Activo);

        var porTipo = incluidos
            .GroupBy(u => u.Rol)
            .Select(g => (Texto: RolUi.Nombre(g.Key), Cantidad: g.Count()))
            .ToList();
        var porGrupo = vendedores
            .GroupBy(ClaveGrupo)
            .Select(g => (Texto: nombresGrupo.GetValueOrDefault(g.Key, g.Key), Cantidad: g.Count()))
            .ToList();

        return new InformeVendedoresViewModel
        {
            Corte = corte,
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
            Personal = PagingHelper.Paginate(
                InformeVendedoresOrden.Ordenar(incluidos.Select(u => Persona(u, nombresGrupo)), orden, direccion),
                page,
                pageSize),
            Orden = InformeVendedoresOrden.Normalizar(orden),
            Direccion = InformeVendedoresOrden.Direccion(direccion)
        };
    }

    private static IReadOnlyList<SegmentoInforme> Segmentos(IReadOnlyList<(string Texto, int Cantidad)> grupos, int total)
    {
        var mayor = grupos.Count == 0 ? 0 : grupos.Max(g => g.Cantidad);
        return grupos
            .OrderByDescending(g => g.Cantidad)
            .ThenBy(g => g.Texto, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new SegmentoInforme
            {
                Texto = g.Texto,
                Cantidad = g.Cantidad,
                Porcentaje = Porcentaje(g.Cantidad, total),
                Ancho = Porcentaje(g.Cantidad, mayor)
            })
            .ToList();
    }

    private static decimal Porcentaje(int parte, int total) =>
        total == 0 ? 0 : Math.Round(parte * 100m / total, 1, MidpointRounding.AwayFromZero);

    private static PersonaInforme Persona(UsuarioResponse u, IReadOnlyDictionary<string, string> nombresGrupo) => new()
    {
        Nombre = u.NombreCompleto,
        Usuario = u.Usuario,
        Documento = u.Documento ?? string.Empty,
        Tipo = RolUi.Nombre(u.Rol),
        Grupo = nombresGrupo.GetValueOrDefault(ClaveGrupo(u), string.Empty),
        Estado = u.Estado.ToString()
    };

    public static VendedoresInformeViewModel Construir(
        IReadOnlyList<UsuarioResponse> usuarios,
        IReadOnlyList<GrupoResponse> grupos,
        FiltrosVendedoresInforme filtros,
        bool configurada)
    {
        var opcionesGrupo = OpcionesGrupo(grupos, usuarios);
        var opcionesUsuario = OpcionesUsuario(usuarios);
        var opcionesTipo = OpcionesTipo();
        var resumen = Resumir(usuarios, grupos, filtros);
        var nombresGrupo = NombresGrupo(grupos, usuarios);

        return new VendedoresInformeViewModel
        {
            Configurada = configurada,
            Filtros = filtros,
            Grupos = opcionesGrupo,
            Usuarios = opcionesUsuario,
            Tipos = opcionesTipo,
            Resumen = resumen,
            ResumenCorto = ResumenCorto(filtros, resumen.Cumplen),
            Detalle =
            [
                new() { Etiqueta = UiTexts.InformeEtiquetaGrupos, Valor = Nombres(filtros.Grupos?.Select(g => nombresGrupo.GetValueOrDefault(g, g)), UiTexts.InformeTodosGrupos) },
                new() { Etiqueta = UiTexts.InformeEtiquetaUsuarios, Valor = Nombres(filtros.Usuarios?.Select(id => usuarios.FirstOrDefault(u => u.UsuarioId == id)?.NombreCompleto ?? id.ToString()), UiTexts.InformeTodosUsuarios) },
                new() { Etiqueta = UiTexts.InformeEtiquetaTipos, Valor = Nombres(filtros.Tipos?.OrderBy(r => (int)r).Select(RolUi.Nombre), UiTexts.InformeTodosTipos) }
            ],
            DatosJson = JsonSerializer.Serialize(new
            {
                usuarios = usuarios.Select(u => new
                {
                    id = u.UsuarioId,
                    nombre = u.NombreCompleto,
                    usuario = u.Usuario,
                    tipo = (int)u.Rol,
                    grupo = ClaveGrupo(u)
                }),
                vendedor = (int)RolUsuario.Vendedor,
                tipos = opcionesTipo.ToDictionary(t => t.Valor, t => t.Texto),
                grupos = nombresGrupo,
                orden = opcionesGrupo.Select(o => o.Valor)
            }, Json)
        };
    }

    public static string ResumenCorto(FiltrosVendedoresInforme filtros, int cumplen)
    {
        var grupos = Contar(filtros.Grupos?.Count, UiTexts.InformeTodosGrupos, UiTexts.InformeUnGrupo, UiTexts.InformeVariosGrupos);
        var usuarios = Contar(filtros.Usuarios?.Count, UiTexts.InformeTodosUsuarios, UiTexts.InformeUnUsuario, UiTexts.InformeVariosUsuarios);
        var tipos = Contar(filtros.Tipos?.Count, UiTexts.InformeTodosTipos, UiTexts.InformeUnTipo, UiTexts.InformeVariosTipos);
        var personas = cumplen == 1 ? UiTexts.InformeUnaPersona : string.Format(UiTexts.InformeVariasPersonas, cumplen);
        return $"{grupos} · {usuarios} · {tipos}. {personas}";
    }

    private static Dictionary<string, string> NombresGrupo(IEnumerable<GrupoResponse> grupos, IEnumerable<UsuarioResponse> usuarios)
    {
        var nombres = OpcionesGrupo(grupos, usuarios).ToDictionary(o => o.Valor, o => o.Texto, StringComparer.OrdinalIgnoreCase);
        foreach (var u in usuarios.Where(u => u.GrupoId is not null))
            nombres.TryAdd(ClaveGrupo(u), u.GrupoNombre ?? string.Empty);
        return nombres;
    }

    private static string RolSinGrupo(RolUsuario rol) => string.Format(UiTexts.InformeRolSinGrupo, RolUi.Nombre(rol));

    private static string Contar(int? cantidad, string todos, string uno, string varios) => cantidad switch
    {
        null => todos,
        1 => uno,
        _ => string.Format(varios, cantidad)
    };

    private static string Nombres(IEnumerable<string>? nombres, string todos)
    {
        if (nombres is null) return todos;
        var lista = nombres.ToList();
        return lista.Count <= NombresVisibles
            ? string.Join(", ", lista)
            : string.Format(UiTexts.InformeYMas, string.Join(", ", lista.Take(NombresVisibles)), lista.Count - NombresVisibles);
    }
}
