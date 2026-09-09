using FluentAssertions;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;
using NewRich.Domain.Enums;

namespace NewRich.UnitTests;

public sealed class OfflineAgrupacionTests
{
    [Fact]
    public void Agrupar_une_por_usuario_y_pda_y_cuenta_todos_los_estados()
    {
        var usuarioA = Guid.NewGuid();
        var usuarioB = Guid.NewGuid();
        var pda1 = Guid.NewGuid();
        var pda2 = Guid.NewGuid();
        var items = new[]
        {
            Codigo(usuarioA, "Ana Perez", pda1, "PDA-01", EstadoCodigoOffline.Generado),
            Codigo(usuarioA, "Ana Perez", pda1, "PDA-01", EstadoCodigoOffline.Utilizado),
            Codigo(usuarioA, "Ana Perez", pda2, "PDA-02", EstadoCodigoOffline.Descargado),
            Codigo(usuarioB, "Luis Mora", pda1, "PDA-01", EstadoCodigoOffline.Registrado)
        };

        var grupos = OfflineAgrupacion.Agrupar(items);

        grupos.Should().HaveCount(3);
        grupos.Should().ContainSingle(g => g.UsuarioId == usuarioA && g.DispositivoId == pda1 && g.Cantidad == 2);
        grupos.Should().ContainSingle(g => g.UsuarioId == usuarioA && g.DispositivoId == pda2 && g.Cantidad == 1);
        grupos.Should().ContainSingle(g => g.UsuarioId == usuarioB && g.DispositivoId == pda1 && g.Cantidad == 1);
    }

    [Fact]
    public void Resumen_cuenta_vendidos_y_sin_usar_segun_estado()
    {
        var usuario = Guid.NewGuid();
        var pda = Guid.NewGuid();
        var items = new[]
        {
            Codigo(usuario, "Ana", pda, "PDA-01", EstadoCodigoOffline.Generado),
            Codigo(usuario, "Ana", pda, "PDA-01", EstadoCodigoOffline.Generado),
            Codigo(usuario, "Ana", pda, "PDA-01", EstadoCodigoOffline.Descargado),
            Codigo(usuario, "Ana", pda, "PDA-01", EstadoCodigoOffline.Utilizado),
            Codigo(usuario, "Ana", pda, "PDA-01", EstadoCodigoOffline.Registrado)
        };

        var resumen = OfflineAgrupacion.Resumen(items);

        resumen.Generados.Should().Be(2);
        resumen.Descargados.Should().Be(1);
        resumen.Vendidos.Should().Be(2);
        resumen.SinUsar.Should().Be(3);
    }

    [Fact]
    public void EnRango_filtra_por_fecha_de_creacion_local()
    {
        var usuario = Guid.NewGuid();
        var pda = Guid.NewGuid();
        var items = new[]
        {
            Codigo(usuario, "Ana", pda, "PDA-01", EstadoCodigoOffline.Generado, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Local)),
            Codigo(usuario, "Ana", pda, "PDA-01", EstadoCodigoOffline.Descargado, new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Local)),
            Codigo(usuario, "Ana", pda, "PDA-01", EstadoCodigoOffline.Utilizado, new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Local))
        };

        var filtrados = OfflineAgrupacion.EnRango(items, new DateTime(2026, 9, 8), new DateTime(2026, 9, 8));

        filtrados.Should().HaveCount(1);
        filtrados[0].Estado.Should().Be(nameof(EstadoCodigoOffline.Descargado));
    }

    private static CodigoOfflineResponse Codigo(
        Guid usuarioId,
        string usuario,
        Guid dispositivoId,
        string pda,
        EstadoCodigoOffline estado,
        DateTime? creacion = null) =>
        new()
        {
            CodigoId = Guid.NewGuid(),
            Consecutivo = Guid.NewGuid().ToString("N")[..8],
            UsuarioId = usuarioId,
            Usuario = usuario,
            DispositivoId = dispositivoId,
            Pda = pda,
            Estado = estado.ToString(),
            FechaCreacion = creacion ?? new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Local)
        };
}
