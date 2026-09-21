using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ResolucionResultadosServiceTests
{
    private static readonly TimeSpan Colombia = TimeSpan.FromHours(-5);
    private static readonly DateOnly FechaJuego = new(2026, 9, 13);

    /// <summary>13/09/2026 20:10 en Colombia, que en UTC ya es el día siguiente.</summary>
    private static readonly DateTime VentaNocturna = new(2026, 9, 14, 1, 10, 7, DateTimeKind.Utc);

    [Fact]
    public async Task Registrar_el_resultado_marca_ganador_al_boleto_que_acerto()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "5432", loterias["Armenia"]);

        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);

        (await db.Boletos.FindAsync(boleto.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.Ganador);
    }

    [Fact]
    public async Task Registrar_el_resultado_marca_no_ganador_al_boleto_de_una_sola_loteria_que_fallo()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "7854", loterias["Armenia"]);

        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);

        (await db.Boletos.FindAsync(boleto.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.NoGanador);
    }

    [Fact]
    public async Task Un_boleto_con_loterias_sin_publicar_sigue_jugado()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "9845", loterias["Armenia"], loterias["Cali"]);

        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);

        (await db.Boletos.FindAsync(boleto.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.Jugado);
    }

    [Fact]
    public async Task Al_publicar_la_ultima_loteria_pendiente_el_boleto_pasa_a_no_ganador()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "9845", loterias["Armenia"], loterias["Cali"]);

        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);
        await resultados.RegistrarAsync(Solicitud(loterias["Cali"], "1111"), CancellationToken.None);

        (await db.Boletos.FindAsync(boleto.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.NoGanador);
    }

    [Fact]
    public async Task Un_boleto_declarado_no_ganador_vuelve_a_ganador_si_acierta_en_otra_loteria()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "1111", loterias["Armenia"], loterias["Cali"]);

        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);
        await resultados.RegistrarAsync(Solicitud(loterias["Cali"], "1111"), CancellationToken.None);

        (await db.Boletos.FindAsync(boleto.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.Ganador);
    }

    [Fact]
    public async Task Recalcular_encuentra_el_resultado_aunque_la_fecha_guardada_tenga_hora()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "5432", loterias["Armenia"]);
        db.NumerosGanadores.Add(new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = loterias["Armenia"],
            FechaJuego = new DateTime(2026, 9, 13, 5, 0, 0),
            Numero = "5432",
            FechaRegistro = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await resultados.RecalcularAsync(CancellationToken.None);

        (await db.Boletos.FindAsync(boleto.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.Ganador);
    }

    [Fact]
    public async Task Registrar_el_resultado_no_toca_boletos_de_otra_fecha_de_juego()
    {
        var (resultados, db, loterias) = CreateSut();
        var otroDia = await CrearBoletoAsync(db, "5432", VentaNocturna.AddDays(-1), loterias["Armenia"]);

        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);

        (await db.Boletos.FindAsync(otroDia.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.Jugado);
    }

    [Fact]
    public async Task Registrar_el_resultado_no_toca_un_boleto_ya_pagado()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "7854", loterias["Armenia"]);
        boleto.EstadoBoleto = EstadoBoleto.PagadoCobrado;
        await db.SaveChangesAsync();

        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);

        (await db.Boletos.FindAsync(boleto.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.PagadoCobrado);
    }

    [Fact]
    public async Task Recalcular_corrige_los_boletos_guardados_antes_de_la_correccion()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "5432", loterias["Armenia"]);
        db.NumerosGanadores.Add(new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = loterias["Armenia"],
            FechaJuego = FechaJuego.ToDateTime(TimeOnly.MinValue),
            Numero = "5432",
            FechaRegistro = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var recalculados = await resultados.RecalcularAsync(CancellationToken.None);

        recalculados.Data.Should().Be(1);
        (await db.Boletos.FindAsync(boleto.BoletoId))!.EstadoBoleto.Should().Be(EstadoBoleto.Ganador);
    }

    [Fact]
    public async Task Listar_cuenta_el_ganador_vendido_despues_de_medianoche_utc()
    {
        var (resultados, db, loterias) = CreateSut();
        await CrearBoletoAsync(db, "3221", new DateTime(2026, 9, 14, 0, 18, 1, DateTimeKind.Utc), loterias["Armenia"]);

        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "3221"), CancellationToken.None);
        var listado = await resultados.ListarAsync(FechaJuego, loterias["Armenia"], CancellationToken.None);

        listado.Data![0].CantidadGanadores.Should().Be(1);
        listado.Data[0].TieneGanadores.Should().BeTrue();
        (await db.Boletos.SingleAsync()).EstadoBoleto.Should().Be(EstadoBoleto.Ganador);
    }

    [Fact]
    public async Task Listar_incluye_la_cantidad_de_boletos_ganadores_del_resultado()
    {
        var (resultados, db, loterias) = CreateSut();
        await CrearBoletoAsync(db, "5432", loterias["Armenia"]);
        await CrearBoletoAsync(db, "5432", loterias["Armenia"]);
        await CrearBoletoAsync(db, "9999", loterias["Armenia"]);
        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);

        var listado = await resultados.ListarAsync(FechaJuego, loterias["Armenia"], CancellationToken.None);

        listado.IsSuccess.Should().BeTrue();
        listado.Data.Should().ContainSingle();
        listado.Data![0].Numero.Should().Be("5432");
        listado.Data[0].CantidadGanadores.Should().Be(2);
        listado.Data[0].TieneGanadores.Should().BeTrue();
    }

    [Fact]
    public async Task Listar_cuenta_cero_cuando_nadie_acerto_el_numero()
    {
        var (resultados, db, loterias) = CreateSut();
        await CrearBoletoAsync(db, "9999", loterias["Armenia"]);
        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);

        var listado = await resultados.ListarAsync(FechaJuego, loterias["Armenia"], CancellationToken.None);

        listado.Data![0].CantidadGanadores.Should().Be(0);
        listado.Data[0].TieneGanadores.Should().BeFalse();
    }

    [Fact]
    public async Task Listar_sigue_contando_un_ganador_aunque_el_premio_ya_se_haya_entregado()
    {
        var (resultados, db, loterias) = CreateSut();
        var boleto = await CrearBoletoAsync(db, "5432", loterias["Armenia"]);
        await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);
        boleto.EstadoBoleto = EstadoBoleto.PremioEntregado;
        await db.SaveChangesAsync();

        var listado = await resultados.ListarAsync(FechaJuego, loterias["Armenia"], CancellationToken.None);

        listado.Data![0].CantidadGanadores.Should().Be(1);
    }

    [Fact]
    public async Task ListarGanadores_devuelve_los_boletos_que_acertaron_el_resultado()
    {
        var (resultados, db, loterias) = CreateSut();
        var ganador1 = await CrearBoletoAsync(db, "5432", loterias["Armenia"]);
        var ganador2 = await CrearBoletoAsync(db, "5432", loterias["Armenia"]);
        await CrearBoletoAsync(db, "9999", loterias["Armenia"]);
        var registro = await resultados.RegistrarAsync(Solicitud(loterias["Armenia"], "5432"), CancellationToken.None);

        var lista = await resultados.ListarGanadoresAsync(registro.Data!.NumeroGanadorId, CancellationToken.None);

        lista.IsSuccess.Should().BeTrue();
        lista.Data.Should().HaveCount(2);
        lista.Data!.Select(b => b.BoletoId).Should().BeEquivalentTo(new[] { ganador1.BoletoId, ganador2.BoletoId });
        lista.Data.Should().OnlyContain(b => b.Estado == BoletoMessages.BoletoGanador);
    }

    private static RegistrarResultadoRequest Solicitud(Guid loteriaId, string numero) => new()
    {
        LoteriaId = loteriaId,
        FechaJuego = FechaJuego,
        Numero = numero
    };

    private static (ResultadoService Sut, NewRichDbContext Db, Dictionary<string, Guid> Loterias) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var loterias = new[] { "Armenia", "Cali", "Medellín" }
            .Select(nombre => new Loteria
            {
                LoteriaId = Guid.NewGuid(),
                Nombre = nombre,
                Estado = EstadoGeneral.Activo,
                FechaCreacion = DateTime.UtcNow
            })
            .ToList();
        db.Loterias.AddRange(loterias);
        db.SaveChanges();
        var clock = new FixedClock(new DateTime(2026, 9, 14, 2, 0, 0, DateTimeKind.Utc), Colombia);
        return (new ResultadoService(db, clock), db, loterias.ToDictionary(l => l.Nombre, l => l.LoteriaId));
    }

    private static Task<Boleto> CrearBoletoAsync(NewRichDbContext db, string numero, params Guid[] loteriaIds) =>
        CrearBoletoAsync(db, numero, VentaNocturna, loteriaIds);

    private static async Task<Boleto> CrearBoletoAsync(NewRichDbContext db, string numero, DateTime fechaVenta, params Guid[] loteriaIds)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "el cejas",
            NombreUsuario = $"cejas{Guid.NewGuid():N}",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            FechaVenta = fechaVenta,
            Total = 4000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = Random.Shared.Next(1_000_000, 9_999_999).ToString(),
            ClaveValidacionHash = "hash",
            EstadoBoleto = EstadoBoleto.Jugado,
            FechaCreacion = fechaVenta,
            VigenciaDias = 30,
            QrCifrado = "qr-guardado"
        };
        var juego = new Juego
        {
            JuegoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            Numero = numero,
            Valor = 2000,
            TipoJuego = TipoJuego.COMBINADA
        };
        foreach (var loteriaId in loteriaIds)
        {
            juego.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteriaId });
        }

        boleto.Juegos.Add(juego);
        db.Usuarios.Add(usuario);
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync();
        return boleto;
    }

    private sealed class FixedClock(DateTime now, TimeSpan desfaseLocal) : IClock
    {
        public DateTime UtcNow => now;
        public DateTime LocalNow => now.Add(desfaseLocal);
    }
}
