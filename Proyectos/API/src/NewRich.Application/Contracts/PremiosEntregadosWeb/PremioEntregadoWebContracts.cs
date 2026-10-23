namespace NewRich.Application.Contracts.PremiosEntregadosWeb;

public sealed record RegistrarPremioWebRequest(
    string? Numero,
    decimal ValorApostado,
    Guid LoteriaId,
    DateOnly FechaPremio,
    decimal ValorPremio,
    string? Password);

public sealed record PremioEntregadoWebResponse(
    Guid Id,
    DateOnly FechaPremio,
    string Loteria,
    string Numero,
    decimal ValorApostado,
    decimal ValorPremio,
    DateTime FechaRegistro,
    Guid VendedorId,
    string Vendedor);

public sealed record PremiosEntregadosWebResponse(int TotalFilas, IReadOnlyList<PremioEntregadoWebResponse> Items);

public sealed record TotalPremiosEntregadosWebResponse(decimal Total, int Cantidad);
