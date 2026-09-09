using System.ComponentModel.DataAnnotations;
using NewRich.Admin.Constants;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Consultas;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Models;

public sealed class ResumenViewModel
{
    public decimal VentasDelDia { get; init; }
    public int BoletosEmitidos { get; init; }
    public int VendedoresActivos { get; init; }
    public int VendedoresTotales { get; init; }
    public int AlertasPendientes { get; init; }
    public IReadOnlyList<VentaResponse> UltimasVentas { get; init; } = [];
}

public sealed class UsuariosIndexViewModel
{
    public string? Busqueda { get; init; }
    public int? Rol { get; init; }
    public PagedViewModel<UsuarioResponse> Pagina { get; init; } = new();
    public IReadOnlyList<GrupoResponse> Grupos { get; init; } = [];
}

public sealed class UsuarioFormViewModel
{
    public Guid? UsuarioId { get; set; }

    [Display(Name = UiTexts.NombreCompleto)]
    [Required(ErrorMessage = ValidationMessages.NombreCompletoRequerido)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Display(Name = UiTexts.NombreUsuario)]
    [Required(ErrorMessage = ValidationMessages.NombreUsuarioRequerido)]
    public string Usuario { get; set; } = string.Empty;

    [Display(Name = UiTexts.Alias)]
    public string? Alias { get; set; }

    [Display(Name = UiTexts.Documento)]
    public string? Documento { get; set; }

    [Display(Name = UiTexts.Celular)]
    public string? Celular { get; set; }

    [Display(Name = UiTexts.Email)]
    public string? Email { get; set; }

    [Display(Name = UiTexts.Rol)]
    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;

    [Display(Name = UiTexts.Estado)]
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;

    [Display(Name = UiTexts.PdaAsociado)]
    public Guid? DispositivoId { get; set; }

    public IReadOnlyList<DispositivoResponse> Dispositivos { get; set; } = [];
}

public sealed class DispositivosIndexViewModel
{
    public string? Busqueda { get; init; }
    public int? Estado { get; init; }
    public PagedViewModel<DispositivoResponse> Pagina { get; init; } = new();
    public IReadOnlyList<UsuarioResponse> Usuarios { get; init; } = [];
}

public sealed class DispositivoFormViewModel
{
    [Display(Name = UiTexts.CodigoDispositivo)]
    [Required(ErrorMessage = ValidationMessages.CodigoDispositivoRequerido)]
    public string CodigoDispositivo { get; set; } = string.Empty;

    [Display(Name = UiTexts.TipoDispositivo)]
    public TipoDispositivo Tipo { get; set; } = TipoDispositivo.Vendedor;

    [Display(Name = UiTexts.Modelo)]
    public string? Modelo { get; set; }

    [Display(Name = UiTexts.NumeroSerie)]
    public string? NumeroSerie { get; set; }
}

public sealed class LoteriasIndexViewModel
{
    public string? Busqueda { get; init; }
    public int? Estado { get; init; }
    public PagedViewModel<LoteriaResponse> Pagina { get; init; } = new();
}

public sealed class LoteriaFormViewModel
{
    public Guid? LoteriaId { get; set; }

    [Display(Name = UiTexts.NombreLoteria)]
    [Required(ErrorMessage = ValidationMessages.CampoRequerido)]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = UiTexts.Estado)]
    public EstadoGeneral Estado { get; set; } = EstadoGeneral.Activo;
}

public sealed class GruposIndexViewModel
{
    public string? Busqueda { get; init; }
    public PagedViewModel<GrupoResponse> Pagina { get; init; } = new();
}

public sealed class GrupoFormViewModel
{
    public Guid? GrupoId { get; set; }

    [Display(Name = UiTexts.NombreGrupo)]
    [Required(ErrorMessage = ValidationMessages.CampoRequerido)]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = UiTexts.Descripcion)]
    [MaxLength(255)]
    public string? Descripcion { get; set; }
}

public sealed class GrupoDetalleViewModel
{
    public GrupoResponse Grupo { get; init; } = new();
    public IReadOnlyList<UsuarioResponse> VendedoresDisponibles { get; init; } = [];
    public PagedViewModel<GrupoVendedorResponse> Pagina { get; init; } = new();
}

public sealed class ResultadosIndexViewModel
{
    public DateOnly? Fecha { get; init; }
    public Guid? LoteriaId { get; init; }
    public PagedViewModel<ResultadoResponse> Pagina { get; init; } = new();
    public IReadOnlyList<LoteriaResponse> Loterias { get; init; } = [];
}

public sealed class ResultadoFormViewModel
{
    [Display(Name = UiTexts.Loteria)]
    [Required(ErrorMessage = ValidationMessages.CampoRequerido)]
    public Guid LoteriaId { get; set; }

    [Display(Name = UiTexts.FechaJuego)]
    [Required(ErrorMessage = ValidationMessages.FechaJuegoRequerida)]
    public DateOnly FechaJuego { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = UiTexts.NumeroGanador)]
    [Required(ErrorMessage = ValidationMessages.NumeroApuestaRequerido)]
    public string Numero { get; set; } = string.Empty;

    public IReadOnlyList<LoteriaResponse> Loterias { get; set; } = [];
}

public sealed class VentasIndexViewModel
{
    public string? Busqueda { get; init; }
    public string? Estado { get; init; }
    public PagedViewModel<BoletoListaResponse> Pagina { get; init; } = new();
}

public sealed class ConsultaBoletosViewModel
{
    public string? CodigoBoleto { get; init; }
    public string? Numero { get; init; }
    public string? Vendedor { get; init; }
    public DateTime? Fecha { get; init; }
    public string? Estado { get; init; }
    public Guid? LoteriaId { get; init; }
    public IReadOnlyList<LoteriaResponse> Loterias { get; init; } = [];
    public PagedViewModel<BusquedaAdministrativaResponse> Pagina { get; init; } = new();
}

public sealed class KpiIndexViewModel
{
    public Guid? VendedorId { get; init; }
    public DateTime? FechaInicial { get; init; }
    public DateTime? FechaFinal { get; init; }
    public KpiResponse? Kpi { get; init; }
    public IReadOnlyList<UsuarioResponse> Vendedores { get; init; } = [];
}

public sealed class NotificacionesIndexViewModel
{
    public string? Busqueda { get; init; }
    public bool? Leida { get; init; }
    public PagedViewModel<NotificacionItemResponse> Pagina { get; init; } = new();
    public int Pendientes { get; init; }
}

public sealed class SoporteIndexViewModel
{
    public IReadOnlyList<ConversacionResponse> Conversaciones { get; init; } = [];
    public ConversacionDetalleResponse? Detalle { get; init; }
    public Guid? ConversacionId { get; init; }
    public IReadOnlyList<UsuarioResponse> Destinatarios { get; init; } = [];
    public string? Busqueda { get; init; }
}

public sealed class ConfiguracionIndexViewModel
{
    public IReadOnlyList<ConfiguracionResponse> Items { get; init; } = [];
}

public sealed class TirillaViewModel
{
    public TirillaResponse Tirilla { get; init; } = new();
}
