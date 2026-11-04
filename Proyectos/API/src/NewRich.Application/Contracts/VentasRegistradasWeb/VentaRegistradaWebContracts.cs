namespace NewRich.Application.Contracts.VentasRegistradasWeb;

public sealed record RegistrarVentaWebRequest(DateOnly FechaVenta, decimal Valor, string? Password);

public sealed record VentaRegistradaWebResponse(Guid Id, DateOnly FechaVenta, decimal Valor, DateTime FechaRegistro);

public sealed record VentasRegistradasWebResponse(int TotalFilas, IReadOnlyList<VentaRegistradaWebResponse> Items);

/// <summary>Total y Cantidad son de Registrar venta; TotalJuegos y CantidadJuegos, de Registrar juego (Individual y Combo).</summary>
public sealed record TotalVentasRegistradasWebResponse(decimal Total, int Cantidad, decimal TotalJuegos = 0m, int CantidadJuegos = 0);
