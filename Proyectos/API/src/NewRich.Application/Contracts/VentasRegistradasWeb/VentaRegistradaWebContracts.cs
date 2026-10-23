namespace NewRich.Application.Contracts.VentasRegistradasWeb;

public sealed record RegistrarVentaWebRequest(DateOnly FechaVenta, decimal Valor, string? Password);

public sealed record VentaRegistradaWebResponse(Guid Id, DateOnly FechaVenta, decimal Valor, DateTime FechaRegistro);

public sealed record VentasRegistradasWebResponse(int TotalFilas, IReadOnlyList<VentaRegistradaWebResponse> Items);

public sealed record TotalVentasRegistradasWebResponse(decimal Total, int Cantidad);
