using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Pda;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

/// <summary>
/// El administrador elige si registra un PDA de venta o un celular. Cada clase sigue su propio
/// camino de instalacion, de modo que un ajuste en uno no cambia el comportamiento del otro.
/// </summary>
public sealed class RegistroPorClaseEquipoTests
{
    private const string Serie = "TCDUAAVWV4KVGI8L";
    private const string CodigoGenerado = "PDA-4F2A9C10";
    private const string RutaApk = "C:\\apk\\NewRich.apk";
    private const string ComandoInstalacionDirecta = $"-s {Serie} install -r -t -d --no-incremental {RutaApk}";
    private const string BloqueoDelFabricante = "Failure [INSTALL_FAILED_USER_RESTRICTED: Install canceled by user]";

    [Fact]
    public void El_vendedor_solo_trabaja_en_un_pda_de_venta()
    {
        PerfilesPorClaseEquipo.Para(ClaseEquipoPda.Pda).Should().Equal(
            TipoDispositivo.Vendedor, TipoDispositivo.Recaudador, TipoDispositivo.Observador);
        PerfilesPorClaseEquipo.Para(ClaseEquipoPda.Celular).Should().Equal(
            TipoDispositivo.Recaudador, TipoDispositivo.Observador);
        PerfilesPorClaseEquipo.Permite(ClaseEquipoPda.Celular, TipoDispositivo.Vendedor).Should().BeFalse();
    }

    [Theory]
    [InlineData(ClaseEquipoPda.Pda)]
    [InlineData(ClaseEquipoPda.Celular)]
    public async Task El_recaudador_se_registra_igual_en_pda_y_en_celular(ClaseEquipoPda clase)
    {
        var api = ApiQueRegistra();
        var sut = CrearServicio(new AdbFalso().ConEquipoListo(Serie), api.Object);

        var resultado = await sut.RegistrarAsync(clase, TipoDispositivo.Recaudador, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeTrue();
        api.Verify(a => a.RegistrarDispositivoAutomaticoAsync(
            It.Is<RegistrarPdaAutomaticoRequest>(r => r.Tipo == TipoDispositivo.Recaudador),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Elegir_celular_para_un_vendedor_no_registra_ni_instala_nada()
    {
        var api = ApiQueRegistra();
        var adb = new AdbFalso().ConEquipoListo(Serie);
        var sut = CrearServicio(adb, api.Object);

        var resultado = await sut.RegistrarAsync(ClaseEquipoPda.Celular, TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Be(UiTexts.PdaPerfilNoDisponibleParaElEquipo);
        adb.Ejecutados.Should().BeEmpty();
        api.Verify(a => a.RegistrarDispositivoAutomaticoAsync(
            It.IsAny<RegistrarPdaAutomaticoRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task El_pda_bloqueado_no_toma_el_desvio_del_celular_y_explica_que_hacer()
    {
        var adb = new AdbFalso().ConEquipoListo(Serie);
        adb.Responder(ComandoInstalacionDirecta, string.Empty, 1, BloqueoDelFabricante);
        var sut = CrearServicio(adb, ApiQueRegistra().Object);

        var resultado = await sut.RegistrarAsync(ClaseEquipoPda.Pda, TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Be(UiTexts.PdaFalloInstalacionPda);
        adb.Ejecutados.Should().NotContain(c => c.Contains("push"));
        adb.Ejecutados.Should().NotContain(c => c.Contains("pm install"));
    }

    [Fact]
    public async Task El_celular_bloqueado_si_copia_el_apk_y_usa_el_instalador_del_sistema()
    {
        var adb = new AdbFalso().ConEquipoListo(Serie);
        adb.Responder(ComandoInstalacionDirecta, string.Empty, 1, BloqueoDelFabricante);
        adb.Responder($"-s {Serie} push {RutaApk} {InstalacionApk.RutaTemporal}", "1 file pushed");
        adb.Responder($"-s {Serie} shell pm install -r -t -d {InstalacionApk.RutaTemporal}", "Success");
        var sut = CrearServicio(adb, ApiQueRegistra().Object);

        var resultado = await sut.RegistrarAsync(ClaseEquipoPda.Celular, TipoDispositivo.Recaudador, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeTrue();
        adb.Ejecutados.Should().Contain(c => c == $"-s {Serie} shell pm install -r -t -d {InstalacionApk.RutaTemporal}");
    }

    [Theory]
    [InlineData(ClaseEquipoPda.Pda)]
    [InlineData(ClaseEquipoPda.Celular)]
    public async Task Las_dos_clases_dejan_la_identidad_y_el_puente_usb_listos(ClaseEquipoPda clase)
    {
        var adb = new AdbFalso().ConEquipoListo(Serie);
        var sut = CrearServicio(adb, ApiQueRegistra().Object);

        var resultado = await sut.RegistrarAsync(clase, TipoDispositivo.Recaudador, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeTrue();
        adb.Ejecutados.Should().Contain(c =>
            c == $"-s {Serie} shell run-as com.newrich.pda sh -c 'echo {CodigoGenerado} > files/identidad.txt'");
        adb.Ejecutados.Should().Contain(c => c == $"-s {Serie} reverse tcp:8090 tcp:8090");
    }

    private static IAvanceRegistroPda Silencio() => new AvanceSilencioso();

    private sealed class AvanceSilencioso : IAvanceRegistroPda
    {
        public Task ReportarAsync(AvanceRegistroPda avance, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static RegistroPdaService CrearServicio(AdbFalso adb, IAdminApiClient api) =>
        new(adb, new ApkFalso(RutaApk), api, Options.Create(new OpcionesRegistroPda
        {
            RutaApk = RutaApk
        }));

    private static Mock<IAdminApiClient> ApiQueRegistra()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.RegistrarDispositivoAutomaticoAsync(It.IsAny<RegistrarPdaAutomaticoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<DispositivoResponse>.Ok(new DispositivoResponse
            {
                DispositivoId = Guid.NewGuid(),
                CodigoDispositivo = CodigoGenerado,
                NumeroSerie = Serie,
                Modelo = "RMX3710",
                Tipo = TipoDispositivo.Recaudador,
                Estado = EstadoGeneral.Activo
            }, "Registro creado."));
        return api;
    }

    private sealed class ApkFalso : IApkPda
    {
        private readonly string? _ruta;

        public ApkFalso(string? ruta) => _ruta = ruta;

        public string? RutaDisponible() => _ruta;
    }

    private sealed class AdbFalso : IAdb
    {
        private readonly Dictionary<string, AdbResultado> _respuestas = new(StringComparer.Ordinal);

        public List<string> Ejecutados { get; } = [];

        public AdbFalso ConEquipoListo(string serie)
        {
            Responder("devices -l", $"List of devices attached\n{serie}\tdevice product:RMX3710 model:RMX3710 transport_id:1\n");
            Responder($"-s {serie} shell getprop ro.product.model", "RMX3710");
            Responder($"-s {serie} shell settings put global newrich_codigo_dispositivo {CodigoGenerado}", string.Empty);
            Responder(ComandoInstalacionDirecta, "Success");
            Responder($"-s {serie} shell pm path com.newrich.pda", "package:/data/app/com.newrich.pda/base.apk");
            Responder($"-s {serie} shell run-as com.newrich.pda sh -c 'echo {CodigoGenerado} > files/identidad.txt'", string.Empty);
            Responder($"-s {serie} shell run-as com.newrich.pda sh -c 'echo {serie} > files/serie.txt'", string.Empty);
            Responder($"-s {serie} reverse tcp:8090 tcp:8090", string.Empty);
            return this;
        }

        public void Responder(string comando, string salida, int codigo = 0, string error = "") =>
            _respuestas[comando] = new AdbResultado(codigo, salida, error);

        public Task<AdbResultado> EjecutarAsync(IReadOnlyList<string> argumentos, CancellationToken cancellationToken)
        {
            var comando = string.Join(" ", argumentos);
            Ejecutados.Add(comando);
            return Task.FromResult(_respuestas.TryGetValue(comando, out var respuesta)
                ? respuesta
                : new AdbResultado(1, string.Empty, $"Comando sin respuesta preparada: {comando}"));
        }
    }
}
