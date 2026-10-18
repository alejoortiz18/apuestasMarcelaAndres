namespace NewRich.Application.Contracts.Utilidades;

public sealed record RegistrarGastoRequest(
    Guid TipoGastoId,
    DateOnly FechaGasto,
    decimal Valor,
    string? Observacion,
    string? ClaveIdempotencia);

public sealed record GastoRegistradoResponse(
    Guid GastoId,
    Guid UsuarioRegistroId,
    string Tipo,
    DateOnly FechaGasto,
    decimal Valor,
    string? Observacion,
    string Estado);

public sealed record TipoGastoResponse(Guid Id, string Nombre, bool RequiereObservacion);

public sealed record ResumenUtilidadesResponse(
    DateOnly Desde,
    DateOnly Hasta,
    decimal Gastos,
    int Cantidad,
    decimal? Ingresos,
    decimal? Ganancia,
    IReadOnlyList<GastoPorTipoResponse> PorTipo);

public sealed record CorregirGastoRequest(decimal Valor, DateOnly FechaGasto, Guid TipoGastoId, string? Motivo);

public sealed record AnularGastoRequest(string? Motivo);

public sealed record MovimientoGastoResponse(
    Guid Id,
    DateOnly FechaGasto,
    Guid TipoGastoId,
    string Tipo,
    string? Observacion,
    decimal Valor,
    Guid UsuarioRegistroId,
    string Usuario,
    string Estado,
    string? ModificadoPor);

public sealed record MovimientosGastosResponse(
    DateOnly Desde,
    DateOnly Hasta,
    decimal TotalValor,
    int Cantidad,
    int TotalFilas,
    IReadOnlyList<MovimientoGastoResponse> Items);

public sealed record GuardarTipoGastoRequest(string? Nombre, string? Descripcion, bool RequiereObservacion);

public sealed record ActualizarTipoGastoRequest(string? Nombre, string? Descripcion, bool RequiereObservacion, bool Activo);

public sealed record TipoGastoCatalogoResponse(
    Guid Id,
    string Nombre,
    string? Descripcion,
    bool Activo,
    bool RequiereObservacion,
    int Gastos);

public sealed record GastoPropioResponse(
    Guid Id,
    DateOnly FechaGasto,
    string Tipo,
    string? Observacion,
    decimal Valor,
    string Estado);

public sealed record GastoPorTipoResponse(string Tipo, decimal Valor, int Cantidad);

public sealed record GastoPorDiaResponse(DateOnly Fecha, decimal Valor);

public sealed record InformeGastosPropiosResponse(
    DateOnly Desde,
    DateOnly Hasta,
    decimal TotalValor,
    int Cantidad,
    decimal Promedio,
    decimal? MayorValor,
    string? MayorTipo,
    DateOnly? MayorFecha,
    decimal AnuladosValor,
    int AnuladosCantidad,
    IReadOnlyList<GastoPorTipoResponse> PorTipo,
    IReadOnlyList<GastoPorDiaResponse> PorDia);

public sealed record MisGastosResponse(
    DateOnly Desde,
    DateOnly Hasta,
    decimal TotalValor,
    int Cantidad,
    int TotalFilas,
    IReadOnlyList<GastoPropioResponse> Items);
