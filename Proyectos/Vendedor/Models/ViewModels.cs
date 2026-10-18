using System.ComponentModel.DataAnnotations;
using System.Globalization;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Vendedor.Constants;

namespace NewRich.Vendedor.Models;

public sealed class LoginViewModel
{
    [Display(Name = UiTexts.Usuario)]
    [Required(ErrorMessage = ValidationMessages.CampoRequerido)]
    public string Usuario { get; set; } = string.Empty;

    [Display(Name = UiTexts.Contrasena)]
    [Required(ErrorMessage = ValidationMessages.CampoRequerido)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }
}

public sealed class CambiarPasswordViewModel
{
    [Display(Name = UiTexts.ContrasenaActual)]
    [Required(ErrorMessage = ValidationMessages.CampoRequerido)]
    [DataType(DataType.Password)]
    public string PasswordActual { get; set; } = string.Empty;

    [Display(Name = UiTexts.NuevaContrasena)]
    [Required(ErrorMessage = ValidationMessages.CampoRequerido)]
    [DataType(DataType.Password)]
    public string PasswordNuevo { get; set; } = string.Empty;

    [Display(Name = UiTexts.ConfirmarContrasena)]
    [Required(ErrorMessage = ValidationMessages.CampoRequerido)]
    [DataType(DataType.Password)]
    public string PasswordConfirmacion { get; set; } = string.Empty;
}

public sealed class InicioViewModel
{
    public string Total { get; set; } = "0";
    public int Boletos { get; set; }
    public bool HorarioCerrado { get; set; }
    public string? Aviso { get; set; }
}

public sealed class FilaJuegoForm
{
    public Guid LoteriaId { get; set; }
    public string Valor { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
}

public sealed class RegistrarJuegoViewModel
{
    public string Clave { get; set; } = Guid.NewGuid().ToString("N");
    public string VentaId { get; set; } = Guid.NewGuid().ToString("N");
    public bool HorarioCerrado { get; set; }
    public List<LoteriaResponse> Loterias { get; set; } = [];
    public List<LoteriaResponse> LoteriasHoy { get; set; } = [];
    public List<string> Restringidos { get; set; } = [];
    public List<FilaJuegoForm> Filas { get; set; } = [new()];
    public List<Guid> LoteriasNumeros { get; set; } = [];
    public string ValorJugado { get; set; } = string.Empty;
    public List<string> Numeros { get; set; } = [string.Empty];
    public ResultadoRegistrarJuego? Resultado { get; set; }
}

public sealed class ResultadoRegistrarJuego
{
    public ResultadoFilasJuego? Filas { get; set; }
    public ResultadoNumerosJuego? Numeros { get; set; }
}

public sealed class ResultadoFilasJuego
{
    public string Codigo { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public IReadOnlyList<FilaResultadoJuego> Filas { get; set; } = [];
}

public sealed class FilaResultadoJuego
{
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}

public sealed class ResultadoNumerosJuego
{
    public string Codigo { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Qr { get; set; } = string.Empty;
    public IReadOnlyList<string> Loterias { get; set; } = [];
    public IReadOnlyList<string> Numeros { get; set; } = [];
    public string ValorJugado { get; set; } = string.Empty;
    public int CantidadNumeros { get; set; }
    public int CantidadLoterias { get; set; }
    public int CantidadApuestas { get; set; }
    public string ValorPorApuesta { get; set; } = string.Empty;
    public int ApuestasConSobrante { get; set; }
}

public sealed class VentaConfirmadaViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public IReadOnlyList<JuegoResponse> Juegos { get; set; } = [];
}

public sealed class HistoricoFilaViewModel
{
    public DateTime Fecha { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public sealed class HistoricoViewModel
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Total { get; set; }
    public string Orden { get; set; } = "fecha";
    public string Direccion { get; set; } = "desc";
    public IReadOnlyList<HistoricoFilaViewModel> Items { get; set; } = [];
}

public sealed class AlertasViewModel
{
    public bool HorarioCerrado { get; set; }
    public IReadOnlyList<string> NumerosRestringidos { get; set; } = [];
    public IReadOnlyList<NotificacionItemResponse> Items { get; set; } = [];
}

public sealed class ValidarViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string? Resultado { get; set; }
    public string? Mensaje { get; set; }
    public string? Tono { get; set; }
}

public sealed class ResultadosViewModel
{
    public DateOnly? Fecha { get; set; }
    public Guid? LoteriaId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Total { get; set; }
    public List<LoteriaResponse> Loterias { get; set; } = [];
    public IReadOnlyList<ResultadoResponse> Items { get; set; } = [];
}

public sealed class SoporteViewModel
{
    public bool Tecnico { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string CodigoTicket { get; set; } = string.Empty;
    public IReadOnlyList<ConversacionResponse> Conversaciones { get; set; } = [];
    public ConversacionDetalleResponse? Hilo { get; set; }
}

public sealed class EntregaPremioViewModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Total { get; set; }
    public IReadOnlyList<CasoGanadorResponse> Items { get; set; } = [];
}

public sealed class PaginacionViewModel
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public string Accion { get; init; } = "Index";
    public string Controlador { get; init; } = string.Empty;
}

public static class Paginas
{
    public static readonly int[] Tamanos = [5, 10, 15];

    public static int Tamano(int pageSize) => Tamanos.Contains(pageSize) ? pageSize : 10;

    public static int Numero(int page, int total, int pageSize)
    {
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return Math.Clamp(page < 1 ? 1 : page, 1, paginas);
    }
}

public static class Montos
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    public static string De(decimal valor) => valor.ToString("N0", Colombia);
}
