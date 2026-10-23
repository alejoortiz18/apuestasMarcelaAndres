namespace NewRich.Application.Contracts.GastosVendedoresWeb;

public sealed record GastoVendedorWebResponse(
    Guid Id,
    DateOnly Fecha,
    Guid VendedorId,
    string Vendedor,
    string Tipo,
    string? Observacion,
    decimal Valor,
    string Estado);

public sealed record GastosVendedoresWebResponse(
    decimal TotalActivos,
    int CantidadActivos,
    IReadOnlyList<GastoVendedorWebResponse> Items);
