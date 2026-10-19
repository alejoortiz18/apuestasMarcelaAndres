namespace NewRich.Domain.Services;

public static class CatalogoTiposGasto
{
    public sealed record Semilla(Guid Id, string Nombre);

    public static readonly IReadOnlyList<Semilla> Inicial =
    [
        new(Guid.Parse("a1000001-0000-4000-8000-000000000001"), "Transporte"),
        new(Guid.Parse("a1000001-0000-4000-8000-000000000002"), "Alimentación"),
        new(Guid.Parse("a1000001-0000-4000-8000-000000000003"), "Entrega de premio"),
        new(Guid.Parse("a1000001-0000-4000-8000-000000000004"), "Servicios"),
        new(Guid.Parse("a1000001-0000-4000-8000-000000000005"), "Mensajería"),
        new(Guid.Parse("a1000001-0000-4000-8000-000000000006"), "Materiales"),
        new(Guid.Parse("a1000001-0000-4000-8000-000000000007"), "Mantenimiento"),
        new(Guid.Parse("a1000001-0000-4000-8000-000000000008"), "Comisiones y gastos bancarios"),
        new(Guid.Parse("a1000001-0000-4000-8000-000000000009"), "Otros")
    ];
}
