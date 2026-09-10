using NewRich.Application.Contracts.Android;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;
using NewRich.Domain.Enums;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Core.Auth;

public sealed class SesionPda
{
    public LoginAndroidResponse? Usuario { get; set; }
    public string CodigoDispositivo { get; set; } = string.Empty;
    public ConfiguracionOperativaResponse Limites { get; set; } = new();
    public TicketDraft? Borrador { get; set; }
    public TirillaVenta? Tirilla { get; set; }
    public bool HorarioCerrado { get; set; }
}

public sealed class TirillaVenta
{
    public required string CodigoImpreso { get; init; }
    public required TipoApuesta Tipo { get; init; }
    public required DateTime Fecha { get; init; }
    public required decimal Total { get; init; }
    public required IReadOnlyList<LineaBorrador> Lineas { get; init; }
    public int VigenciaDias { get; init; } = 30;
    public required string QrContenido { get; init; }
    public bool Offline { get; init; }
    public string LeyendaCompleta { get; init; } = string.Empty;

    public string Texto => NewRich.Pda.Core.TirillaTexto.De(
        CodigoImpreso,
        Tipo,
        Fecha,
        Total,
        Lineas,
        VigenciaDias,
        leyendaCompleta: string.IsNullOrWhiteSpace(LeyendaCompleta) ? null : LeyendaCompleta);

    public TirillaResponse ARespuesta() => new()
    {
        CodigoImpreso = CodigoImpreso,
        Fecha = Fecha,
        Total = Total,
        Qr = QrContenido,
        TipoApuesta = Tipo,
        VigenciaDias = VigenciaDias,
        Leyenda = string.IsNullOrWhiteSpace(LeyendaCompleta)
            ? TirillaCuerpo.Leyenda(VigenciaDias > 0 ? VigenciaDias : 30)
            : LeyendaCompleta,
        Juegos = Lineas.Select(l => new JuegoResponse
        {
            Numero = l.Numero,
            Valor = l.Valor,
            Total = l.TotalLinea,
            Loterias = l.LoteriaNombres
        }).ToList()
    };

    public static TirillaVenta DesdeVenta(
        VentaResponse venta,
        TipoApuesta tipo,
        int vigenciaDias,
        bool offline,
        string? cuerpoLeyenda = null)
    {
        var vigencia = vigenciaDias > 0 ? vigenciaDias : 30;
        return new TirillaVenta
        {
            CodigoImpreso = venta.CodigoImpreso,
            Tipo = tipo,
            Fecha = venta.FechaVenta.Kind == DateTimeKind.Utc ? venta.FechaVenta.ToLocalTime() : venta.FechaVenta,
            Total = venta.Total,
            Lineas = venta.Juegos.Select(j => new LineaBorrador
            {
                Numero = j.Numero,
                Valor = j.Valor,
                LoteriaIds = Enumerable.Repeat(Guid.Empty, Math.Max(1, j.Loterias.Count)).ToArray(),
                LoteriaNombres = j.Loterias,
                TotalExplicit = j.Total
            }).ToArray(),
            VigenciaDias = vigencia,
            QrContenido = venta.Qr,
            Offline = offline,
            LeyendaCompleta = TirillaCuerpo.Leyenda(vigencia, cuerpoLeyenda)
        };
    }
}
