using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class LoteriaServiceTests
{
    [Fact]
    public async Task ListarAsync_sin_ventas_muestra_cero_y_ciudad()
    {
        var (sut, db) = CreateSut();
        db.Configuraciones.Add(new Configuracion
        {
            ConfiguracionId = Guid.NewGuid(),
            Clave = "HoraCierre",
            Valor = "20:00:00",
            FechaActualizacion = DateTime.UtcNow
        });
        db.Loterias.Add(new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Bogota",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(l =>
            l.Nombre == "Bogota"
            && l.NumeroJugado == null
            && l.HoraCierre == "20:00"
            && l.BoletosVendidos == 0
            && l.TotalVendido == 0
            && l.TipoApuesta == null);
    }

    [Fact]
    public async Task ListarAsync_acumula_boletos_y_total_de_la_loteria()
    {
        var (sut, db) = CreateSut();
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Medellin",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Loterias.Add(loteria);
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Camila Rojas",
            NombreUsuario = "camila",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            FechaVenta = DateTime.UtcNow,
            Total = 5000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        db.Ventas.Add(venta);
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = "AOL1234",
            ClaveValidacionHash = "x",
            EstadoBoleto = EstadoBoleto.Jugado,
            FechaCreacion = DateTime.UtcNow,
            VigenciaDias = 30
        };
        db.Boletos.Add(boleto);
        var juego = new Juego
        {
            JuegoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            Numero = "1234",
            Valor = 5000,
            TipoJuego = TipoJuego.COMBINADA
        };
        db.Juegos.Add(juego);
        db.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteria.LoteriaId });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(l =>
            l.Nombre == "Medellin"
            && l.NumeroJugado == "1234"
            && l.BoletosVendidos == 1
            && l.TotalVendido == 5000
            && l.TipoApuesta == "Combo");
    }

    [Fact]
    public async Task CrearAsync_exige_al_menos_un_dia_de_juego()
    {
        var (sut, _) = CreateSut();

        var result = await sut.CrearAsync(new CrearLoteriaRequest { Nombre = "Boyaca" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Debe indicar al menos un día de juego para la lotería.");
    }

    [Fact]
    public async Task CrearAsync_exige_hora_inicio_y_fin()
    {
        var (sut, _) = CreateSut();

        var result = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Lunes]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.HorarioLoteriaRequerido);
    }

    [Fact]
    public async Task CrearAsync_rechaza_horario_fuera_del_pda()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "18:00:00");

        var result = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "07:00",
                HoraFin = "11:00"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.HorarioLoteriaFueraDePda);
    }

    [Fact]
    public async Task CrearAsync_guarda_el_horario_disponible()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "18:00:00");

        var creado = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "09:00",
                HoraFin = "11:00",
                JornadaId = JornadaManana
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        creado.IsSuccess.Should().BeTrue(creado.Message);
        listado.Data.Should().ContainSingle(l => l.Nombre == "Boyaca")
            .Which.Should().Match<LoteriaResponse>(l => l.HoraInicio == "09:00" && l.HoraFin == "11:00");
    }

    [Fact]
    public async Task CrearAsync_exige_la_jornada()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "18:00:00");

        var result = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "09:00",
                HoraFin = "11:00"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.JornadaRequerida);
        db.Loterias.Should().BeEmpty();
    }

    [Fact]
    public async Task CrearAsync_rechaza_una_jornada_que_no_existe()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "18:00:00");

        var result = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "09:00",
                HoraFin = "11:00",
                JornadaId = Guid.NewGuid()
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be(UsuarioMessages.JornadaNoEncontrada);
    }

    [Fact]
    public async Task CrearAsync_rechaza_hora_fin_fuera_de_la_jornada()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "20:00:00");

        var result = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Armenia",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "10:00",
                HoraFin = "17:00",
                JornadaId = JornadaManana
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.JornadaHoraFinNoCoincide);
        db.Loterias.Should().BeEmpty();
    }

    [Fact]
    public async Task CrearAsync_guarda_la_jornada_y_el_listado_la_muestra()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "20:00:00");

        var creada = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Armenia",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "10:00",
                HoraFin = "17:00",
                JornadaId = JornadaTarde
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        creada.IsSuccess.Should().BeTrue(creada.Message);
        creada.Data!.JornadaNombre.Should().Be("Tarde");
        listado.Data.Should().ContainSingle(l => l.Nombre == "Armenia")
            .Which.Should().Match<LoteriaResponse>(l => l.JornadaId == JornadaTarde && l.JornadaNombre == "Tarde");
    }

    [Fact]
    public async Task ActualizarAsync_exige_la_jornada()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "20:00:00");
        var creada = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);

        var result = await sut.ActualizarAsync(
            creada.Data!.LoteriaId,
            new ActualizarLoteriaRequest
            {
                Nombre = "Cali",
                Estado = EstadoGeneral.Activo,
                HoraInicio = "10:00",
                HoraFin = "13:00"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.JornadaRequerida);
    }

    [Fact]
    public async Task ActualizarAsync_no_cambia_nada_si_la_hora_fin_no_es_de_la_jornada()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "20:00:00");
        var creada = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);

        var result = await sut.ActualizarAsync(
            creada.Data!.LoteriaId,
            new ActualizarLoteriaRequest
            {
                Nombre = "Cali renombrada",
                Estado = EstadoGeneral.Activo,
                HoraInicio = "10:00",
                HoraFin = "13:00",
                JornadaId = JornadaNoche
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.JornadaHoraFinNoCoincide);
        listado.Data.Should().ContainSingle()
            .Which.Should().Match<LoteriaResponse>(l => l.Nombre == "Cali" && l.JornadaNombre == "Tarde");
    }

    [Fact]
    public async Task ActualizarAsync_cambia_la_jornada_junto_con_la_hora_fin()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "20:00:00");
        var creada = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);

        var result = await sut.ActualizarAsync(
            creada.Data!.LoteriaId,
            new ActualizarLoteriaRequest
            {
                Nombre = "Cali",
                Estado = EstadoGeneral.Activo,
                HoraInicio = "09:00",
                HoraFin = "11:30",
                JornadaId = JornadaManana
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.JornadaNombre.Should().Be("Mañana");
        db.Loterias.Single().JornadaId.Should().Be(JornadaManana);
    }

    [Fact]
    public async Task ActualizarHorariosAsync_pasa_la_loteria_a_la_jornada_de_su_nueva_hora_fin()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "20:00:00");
        var creada = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias =
                [
                    new HorarioLoteriaRequest { LoteriaId = creada.Data!.LoteriaId, HoraInicio = "18:30", HoraFin = "19:30" }
                ]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data.Should().ContainSingle()
            .Which.Should().Match<LoteriaResponse>(l => l.HoraFin == "19:30" && l.JornadaId == JornadaNoche && l.JornadaNombre == "Noche");
    }

    [Fact]
    public async Task ActualizarAsync_rechaza_horario_fuera_del_pda()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "08:00:00", "18:00:00");
        var creada = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "09:00",
                HoraFin = "11:00",
                JornadaId = JornadaManana
            },
            CancellationToken.None);

        var result = await sut.ActualizarAsync(
            creada.Data!.LoteriaId,
            new ActualizarLoteriaRequest
            {
                Nombre = "Boyaca",
                Estado = EstadoGeneral.Activo,
                HoraInicio = "09:00",
                HoraFin = "19:00"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.HorarioLoteriaFueraDePda);
    }

    [Fact]
    public async Task CrearAsync_guarda_los_dias_indicados()
    {
        var (sut, _) = CreateSut();

        var creado = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Sabado, DiaSemana.Sabado, DiaSemana.Jueves],
                HoraInicio = "10:00",
                HoraFin = "13:00",
                JornadaId = JornadaTarde
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        creado.IsSuccess.Should().BeTrue(creado.Message);
        listado.Data.Should().ContainSingle(l => l.Nombre == "Boyaca")
            .Which.DiasHabilitados.Should().Equal(DiaSemana.Jueves, DiaSemana.Sabado);
    }

    [Fact]
    public async Task ListarAsync_incluye_los_dias_ya_guardados()
    {
        var (sut, db) = CreateSut();
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Bogota",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Loterias.Add(loteria);
        db.Set<LoteriaDiaSemana>().AddRange(
            new LoteriaDiaSemana { LoteriaId = loteria.LoteriaId, DiaSemana = DiaSemana.Jueves, FechaActualizacion = DateTime.UtcNow },
            new LoteriaDiaSemana { LoteriaId = loteria.LoteriaId, DiaSemana = DiaSemana.Martes, FechaActualizacion = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.Data.Should().ContainSingle(l => l.Nombre == "Bogota")
            .Which.DiasHabilitados.Should().Equal(DiaSemana.Martes, DiaSemana.Jueves);
    }

    [Fact]
    public async Task ActualizarDiasAsync_reemplaza_los_dias_de_cada_loteria()
    {
        var (sut, _) = CreateSut();
        var creada = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Cundinamarca",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "10:00",
                HoraFin = "13:00",
                JornadaId = JornadaTarde
            },
            CancellationToken.None);

        var result = await sut.ActualizarDiasAsync(
            new ActualizarDiasLoteriasRequest
            {
                Loterias =
                [
                    new DiasLoteriaRequest
                    {
                        LoteriaId = creada.Data!.LoteriaId,
                        DiasHabilitados = [DiaSemana.Miercoles, DiaSemana.Sabado]
                    }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        listado.Data.Should().ContainSingle(l => l.Nombre == "Cundinamarca")
            .Which.DiasHabilitados.Should().Equal(DiaSemana.Miercoles, DiaSemana.Sabado);
    }

    [Fact]
    public async Task ActualizarTopesAsync_guarda_el_tope_de_todas_las_loterias()
    {
        var (sut, _) = CreateSut();
        var cali = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);
        var pasto = await sut.CrearAsync(RequestCrear("Pasto"), CancellationToken.None);

        var result = await sut.ActualizarTopesAsync(
            new ActualizarTopesLoteriasRequest
            {
                Loterias =
                [
                    new TopeLoteriaRequest { LoteriaId = cali.Data!.LoteriaId, Tope = 250000 },
                    new TopeLoteriaRequest { LoteriaId = pasto.Data!.LoteriaId, Tope = 0 }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        listado.Data!.Single(l => l.Nombre == "Cali").Tope.Should().Be(250000);
        listado.Data!.Single(l => l.Nombre == "Pasto").Tope.Should().Be(0);
    }

    [Fact]
    public async Task ActualizarTopesAsync_no_guarda_nada_si_un_tope_es_negativo()
    {
        var (sut, _) = CreateSut();
        var cali = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);
        var pasto = await sut.CrearAsync(RequestCrear("Pasto"), CancellationToken.None);

        var result = await sut.ActualizarTopesAsync(
            new ActualizarTopesLoteriasRequest
            {
                Loterias =
                [
                    new TopeLoteriaRequest { LoteriaId = cali.Data!.LoteriaId, Tope = 250000 },
                    new TopeLoteriaRequest { LoteriaId = pasto.Data!.LoteriaId, Tope = -1 }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(VentaMessages.TopeNegativo);
        listado.Data!.Should().OnlyContain(l => l.Tope == 1000);
    }

    [Fact]
    public async Task ActualizarHorariosAsync_guarda_el_horario_de_las_loterias()
    {
        var (sut, _) = CreateSut();
        var cali = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);
        var pasto = await sut.CrearAsync(RequestCrear("Pasto"), CancellationToken.None);

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias =
                [
                    new HorarioLoteriaRequest { LoteriaId = cali.Data!.LoteriaId, HoraInicio = "11:00", HoraFin = "14:00" },
                    new HorarioLoteriaRequest { LoteriaId = pasto.Data!.LoteriaId, HoraInicio = "12:00", HoraFin = "15:00" }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        listado.Data!.Single(l => l.Nombre == "Cali").HoraInicio.Should().Be("11:00");
        listado.Data!.Single(l => l.Nombre == "Cali").HoraFin.Should().Be("14:00");
        listado.Data!.Single(l => l.Nombre == "Pasto").HoraInicio.Should().Be("12:00");
        listado.Data!.Single(l => l.Nombre == "Pasto").HoraFin.Should().Be("15:00");
    }

    [Fact]
    public async Task ActualizarHorariosAsync_no_guarda_nada_si_el_inicio_no_es_menor_que_el_fin()
    {
        var (sut, _) = CreateSut();
        var cali = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);
        var pasto = await sut.CrearAsync(RequestCrear("Pasto"), CancellationToken.None);

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias =
                [
                    new HorarioLoteriaRequest { LoteriaId = cali.Data!.LoteriaId, HoraInicio = "11:00", HoraFin = "14:00" },
                    new HorarioLoteriaRequest { LoteriaId = pasto.Data!.LoteriaId, HoraInicio = "15:00", HoraFin = "14:00" }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.HorarioLoteriaInicioMayorQueFin);
        listado.Data!.Should().OnlyContain(l => l.HoraInicio == "10:00" && l.HoraFin == "13:00");
    }

    [Fact]
    public async Task ActualizarHorariosAsync_no_guarda_nada_si_un_horario_esta_fuera_del_pda()
    {
        var (sut, _) = CreateSut();
        var cali = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);
        var pasto = await sut.CrearAsync(RequestCrear("Pasto"), CancellationToken.None);

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias =
                [
                    new HorarioLoteriaRequest { LoteriaId = cali.Data!.LoteriaId, HoraInicio = "11:00", HoraFin = "14:00" },
                    new HorarioLoteriaRequest { LoteriaId = pasto.Data!.LoteriaId, HoraInicio = "09:00", HoraFin = "12:00" }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(string.Format(ValidationMessages.HorarioLoteriaFueraDePdaDe, "Pasto", "10:00", "20:00"));
        listado.Data!.Should().OnlyContain(l => l.HoraInicio == "10:00" && l.HoraFin == "13:00");
    }

    [Fact]
    public async Task ActualizarHorariosAsync_sin_cambios_no_dice_que_actualizo()
    {
        var vivo = new LoteriasVivoSpy();
        var (sut, _) = CreateSut(vivo);

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias = [new HorarioLoteriaRequest { LoteriaId = Guid.Empty, HoraInicio = "10:00", HoraFin = "11:00" }]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Message.Should().Be(SuccessMessages.HorariosSinCambios);
        vivo.Avisos.Should().Be(0);
    }

    [Fact]
    public async Task ActualizarHorariosAsync_mismo_horario_no_dice_que_actualizo()
    {
        var vivo = new LoteriasVivoSpy();
        var (sut, db) = CreateSut(vivo);
        var cali = await SembrarLoteriaAsync(db, "Cali", new TimeSpan(10, 0, 0), new TimeSpan(13, 0, 0));

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias = [new HorarioLoteriaRequest { LoteriaId = cali, HoraInicio = "10:00", HoraFin = "13:00" }]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Message.Should().Be(SuccessMessages.HorariosSinCambios);
        result.Data.Should().ContainSingle(l => l.Nombre == "Cali");
        vivo.Avisos.Should().Be(0);
    }

    [Fact]
    public async Task ActualizarHorariosAsync_no_valida_loterias_sin_cambio()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "06:00:00", "20:00:00");
        var manana = await SembrarLoteriaAsync(db, "Antioqueña Día", new TimeSpan(6, 0, 0), new TimeSpan(9, 30, 0));
        var bogota = await SembrarLoteriaAsync(db, "Bogotá", new TimeSpan(6, 0, 0), new TimeSpan(22, 30, 0));

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias =
                [
                    new HorarioLoteriaRequest { LoteriaId = manana, HoraInicio = "06:30", HoraFin = "09:30" },
                    new HorarioLoteriaRequest { LoteriaId = bogota, HoraInicio = "06:00", HoraFin = "22:30" }
                ]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        db.Loterias.Single(l => l.LoteriaId == manana).HoraInicio.Should().Be(new TimeSpan(6, 30, 0));
        db.Loterias.Single(l => l.LoteriaId == bogota).HoraFin.Should().Be(new TimeSpan(22, 30, 0));
    }

    [Fact]
    public async Task ActualizarHorariosAsync_cambia_el_inicio_aunque_el_fin_guardado_supere_el_cierre_del_pda()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "06:00:00", "20:00:00");
        var noche = await SembrarLoteriaAsync(db, "Dorado Noche", new TimeSpan(6, 0, 0), new TimeSpan(21, 30, 0));

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias = [new HorarioLoteriaRequest { LoteriaId = noche, HoraInicio = "06:30", HoraFin = "21:30" }]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        db.Loterias.Single(l => l.LoteriaId == noche).HoraInicio.Should().Be(new TimeSpan(6, 30, 0));
        db.Loterias.Single(l => l.LoteriaId == noche).HoraFin.Should().Be(new TimeSpan(21, 30, 0));
    }

    [Fact]
    public async Task ActualizarHorariosAsync_rechaza_un_fin_nuevo_fuera_del_pda_indicando_la_loteria()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "06:00:00", "20:00:00");
        var manana = await SembrarLoteriaAsync(db, "Antioqueña Día", new TimeSpan(6, 0, 0), new TimeSpan(9, 30, 0));

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias = [new HorarioLoteriaRequest { LoteriaId = manana, HoraInicio = "06:00", HoraFin = "21:00" }]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(string.Format(ValidationMessages.HorarioLoteriaFueraDePdaDe, "Antioqueña Día", "06:00", "20:00"));
        db.Loterias.Single(l => l.LoteriaId == manana).HoraFin.Should().Be(new TimeSpan(9, 30, 0));
    }

    [Fact]
    public async Task ActualizarHorariosAsync_rechaza_un_inicio_nuevo_antes_de_la_apertura_del_pda()
    {
        var (sut, db) = CreateSut();
        await SembrarHorarioPdaAsync(db, "06:00:00", "20:00:00");
        var noche = await SembrarLoteriaAsync(db, "Dorado Noche", new TimeSpan(6, 0, 0), new TimeSpan(21, 30, 0));

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias = [new HorarioLoteriaRequest { LoteriaId = noche, HoraInicio = "05:30", HoraFin = "21:30" }]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(string.Format(ValidationMessages.HorarioLoteriaFueraDePdaDe, "Dorado Noche", "06:00", "20:00"));
        db.Loterias.Single(l => l.LoteriaId == noche).HoraInicio.Should().Be(new TimeSpan(6, 0, 0));
    }

    [Fact]
    public async Task ActualizarHorariosAsync_avisa_a_los_pdas_en_tiempo_real()
    {
        var vivo = new LoteriasVivoSpy();
        var (sut, _) = CreateSut(vivo);
        var creada = await sut.CrearAsync(RequestCrear("Cali"), CancellationToken.None);
        vivo.Avisos = 0;

        var result = await sut.ActualizarHorariosAsync(
            new ActualizarHorariosLoteriasRequest
            {
                Loterias =
                [
                    new HorarioLoteriaRequest { LoteriaId = creada.Data!.LoteriaId, HoraInicio = "11:00", HoraFin = "14:00" }
                ]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        vivo.Avisos.Should().Be(1);
    }

    [Fact]
    public async Task CrearAsync_avisa_a_los_pdas_en_tiempo_real()
    {
        var vivo = new LoteriasVivoSpy();
        var (sut, db) = CreateSut(vivo);
        await SembrarHorarioPdaAsync(db, "08:00:00", "18:00:00");

        var result = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "09:00",
                HoraFin = "11:00",
                JornadaId = JornadaManana
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        vivo.Avisos.Should().Be(1);
    }

    [Fact]
    public async Task ActualizarDiasAsync_avisa_a_los_pdas_en_tiempo_real()
    {
        var vivo = new LoteriasVivoSpy();
        var (sut, db) = CreateSut(vivo);
        await SembrarHorarioPdaAsync(db, "08:00:00", "18:00:00");
        var creada = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Cundinamarca",
                DiasHabilitados = [DiaSemana.Lunes],
                HoraInicio = "10:00",
                HoraFin = "13:00",
                JornadaId = JornadaTarde
            },
            CancellationToken.None);
        vivo.Avisos = 0;

        var result = await sut.ActualizarDiasAsync(
            new ActualizarDiasLoteriasRequest
            {
                Loterias =
                [
                    new DiasLoteriaRequest
                    {
                        LoteriaId = creada.Data!.LoteriaId,
                        DiasHabilitados = [DiaSemana.Martes]
                    }
                ]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        vivo.Avisos.Should().Be(1);
    }

    private static readonly Guid JornadaManana = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid JornadaTarde = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid JornadaNoche = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static CrearLoteriaRequest RequestCrear(string nombre) => new()
    {
        Nombre = nombre,
        Tope = 1000,
        DiasHabilitados = [DiaSemana.Lunes],
        HoraInicio = "10:00",
        HoraFin = "13:00",
        JornadaId = JornadaTarde
    };

    private static async Task SembrarHorarioPdaAsync(NewRichDbContext db, string apertura, string cierre)
    {
        db.Configuraciones.AddRange(
            new Configuracion
            {
                ConfiguracionId = Guid.NewGuid(),
                Clave = "HoraApertura",
                Valor = apertura,
                FechaActualizacion = DateTime.UtcNow
            },
            new Configuracion
            {
                ConfiguracionId = Guid.NewGuid(),
                Clave = "HoraCierre",
                Valor = cierre,
                FechaActualizacion = DateTime.UtcNow
            });
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> SembrarLoteriaAsync(NewRichDbContext db, string nombre, TimeSpan inicio, TimeSpan fin)
    {
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = nombre,
            Estado = EstadoGeneral.Activo,
            Tope = 1000,
            HoraInicio = inicio,
            HoraFin = fin,
            JornadaId = JornadaPorHoraCierre.Nombre(fin) switch
            {
                JornadaPorHoraCierre.Manana => JornadaManana,
                JornadaPorHoraCierre.Tarde => JornadaTarde,
                _ => JornadaNoche
            },
            FechaCreacion = DateTime.UtcNow
        };
        db.Loterias.Add(loteria);
        await db.SaveChangesAsync();
        return loteria.LoteriaId;
    }

    private static (LoteriaService Sut, NewRichDbContext Db) CreateSut(ILoteriasTiempoReal? vivo = null)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        db.Jornadas.AddRange(
            new Jornada { JornadaId = JornadaManana, Nombre = "Mañana", FechaCreacion = DateTime.UtcNow },
            new Jornada { JornadaId = JornadaTarde, Nombre = "Tarde", FechaCreacion = DateTime.UtcNow },
            new Jornada { JornadaId = JornadaNoche, Nombre = "Noche", FechaCreacion = DateTime.UtcNow });
        db.SaveChanges();
        return (new LoteriaService(db, new FixedClock(DateTime.UtcNow), vivo ?? new LoteriasVivoSpy()), db);
    }

    private sealed class LoteriasVivoSpy : ILoteriasTiempoReal
    {
        public int Avisos { get; set; }

        public Task AvisarCatalogoActualizadoAsync(CancellationToken cancellationToken)
        {
            Avisos++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
