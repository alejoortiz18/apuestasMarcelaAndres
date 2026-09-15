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
/// Registro guiado del PDA: el administrador solo ve el avance y el resultado. El codigo unico lo
/// genera el sistema, se graba en el equipo y nunca se muestra ni se pide en pantalla.
/// </summary>
public sealed class RegistroGuiadoPdaTests
{
    private const string Serie = "TCDUAAVWV4KVGI8L";
    private const string CodigoGenerado = "PDA-4F2A9C10";

    [Fact]
    public async Task Verificar_confirma_el_equipo_listo_antes_de_continuar()
    {
        var sut = CrearServicio(out _, out _);

        var verificacion = await sut.VerificarAsync(CancellationToken.None);

        verificacion.Listo.Should().BeTrue();
        verificacion.Modelo.Should().Be("RMX3710");
        verificacion.Mensaje.Should().Be(UiTexts.PdaListoParaRegistrar);
    }

    [Fact]
    public async Task Verificar_avisa_cuando_no_hay_ningun_equipo_conectado()
    {
        var adb = new AdbFalso();
        adb.Responder("devices -l", "List of devices attached\n\n");
        var sut = CrearServicio(adb, ApiQueRegistra().Object);

        var verificacion = await sut.VerificarAsync(CancellationToken.None);

        verificacion.Listo.Should().BeFalse();
        verificacion.Mensaje.Should().Be(UiTexts.PdaSinDispositivoConectado);
    }

    [Fact]
    public async Task Verificar_avisa_cuando_falta_permitir_la_depuracion_usb()
    {
        var adb = new AdbFalso();
        adb.Responder("devices -l", $"List of devices attached\n{Serie}\tunauthorized\n");
        var sut = CrearServicio(adb, ApiQueRegistra().Object);

        var verificacion = await sut.VerificarAsync(CancellationToken.None);

        verificacion.Listo.Should().BeFalse();
        verificacion.Mensaje.Should().Be(UiTexts.PdaSinAutorizacionUsb);
    }

    [Fact]
    public async Task Verificar_pide_dejar_un_solo_equipo_conectado()
    {
        var adb = new AdbFalso();
        adb.Responder("devices -l", $"List of devices attached\n{Serie}\tdevice model:RMX3710\nOTRA-SERIE\tdevice model:H10\n");
        var sut = CrearServicio(adb, ApiQueRegistra().Object);

        var verificacion = await sut.VerificarAsync(CancellationToken.None);

        verificacion.Listo.Should().BeFalse();
        verificacion.Mensaje.Should().Be(UiTexts.PdaVariosDispositivos);
    }

    [Fact]
    public async Task El_registro_informa_el_avance_de_cero_a_cien_en_orden()
    {
        var sut = CrearServicio(out _, out _);
        var avance = new AvanceRegistrado();

        var resultado = await sut.RegistrarAsync(TipoDispositivo.Vendedor, avance, CancellationToken.None);

        resultado.Exitoso.Should().BeTrue();
        var avances = avance.Reportados;
        avances.Select(a => a.Mensaje).Should().Equal(
            UiTexts.PdaProgresoDetectando,
            UiTexts.PdaProgresoConectado,
            UiTexts.PdaProgresoValidando,
            UiTexts.PdaProgresoGenerando,
            UiTexts.PdaProgresoRegistrando,
            UiTexts.PdaProgresoInstalando,
            UiTexts.PdaProgresoVerificando,
            UiTexts.PdaProgresoFinalizando,
            UiTexts.PdaRegistroCompletado);
        avances.Select(a => a.Porcentaje).Should().BeInAscendingOrder();
        avances[0].Porcentaje.Should().Be(0);
        avances[^1].Porcentaje.Should().Be(100);
    }

    [Fact]
    public async Task El_administrador_nunca_ve_el_codigo_unico_del_dispositivo()
    {
        var sut = CrearServicio(out _, out _);
        var avance = new AvanceRegistrado();

        var resultado = await sut.RegistrarAsync(TipoDispositivo.Vendedor, avance, CancellationToken.None);

        var textoVisible = string.Join(" ", avance.Reportados.Select(a => a.Mensaje).Append(resultado.Mensaje));
        textoVisible.Should().NotContain(CodigoGenerado);
        typeof(ResultadoRegistroPda).GetProperties().Should().NotContain(p => p.Name.Contains("Codigo"));
    }

    [Fact]
    public async Task El_registro_graba_en_el_equipo_el_codigo_que_genero_el_sistema()
    {
        var sut = CrearServicio(out var adb, out _);

        await sut.RegistrarAsync(TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        adb.Ejecutados.Should().ContainSingle(c =>
            c == $"-s {Serie} shell settings put global newrich_codigo_dispositivo {CodigoGenerado}");
    }

    [Fact]
    public async Task El_registro_instala_la_aplicacion_en_el_equipo_detectado()
    {
        var sut = CrearServicio(out var adb, out _);

        await sut.RegistrarAsync(TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        adb.Ejecutados.Should().ContainSingle(c => c == $"-s {Serie} install -r C:\\apk\\NewRich.apk");
    }

    [Fact]
    public async Task El_registro_identifica_el_equipo_por_su_numero_de_serie()
    {
        var api = ApiQueRegistra();
        var sut = CrearServicio(new AdbFalso().ConEquipoListo(Serie), api.Object);

        await sut.RegistrarAsync(TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        api.Verify(a => a.RegistrarDispositivoAutomaticoAsync(
            It.Is<RegistrarPdaAutomaticoRequest>(r => r.NumeroSerie == Serie && r.Modelo == "RMX3710"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TipoDispositivo.Vendedor)]
    [InlineData(TipoDispositivo.Observador)]
    public async Task El_tipo_de_usuario_que_elige_el_administrador_queda_en_el_dispositivo(TipoDispositivo tipo)
    {
        var api = ApiQueRegistra();
        var sut = CrearServicio(new AdbFalso().ConEquipoListo(Serie), api.Object);

        await sut.RegistrarAsync(tipo, Silencio(), CancellationToken.None);

        api.Verify(a => a.RegistrarDispositivoAutomaticoAsync(
            It.Is<RegistrarPdaAutomaticoRequest>(r => r.Tipo == tipo),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sin_aplicacion_disponible_no_se_registra_el_dispositivo()
    {
        var api = ApiQueRegistra();
        var sut = CrearServicio(new AdbFalso().ConEquipoListo(Serie), api.Object, new ApkFalso(null));

        var resultado = await sut.RegistrarAsync(TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Be(UiTexts.PdaSinAplicacionDisponible);
        api.Verify(a => a.RegistrarDispositivoAutomaticoAsync(
            It.IsAny<RegistrarPdaAutomaticoRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Si_la_api_rechaza_el_registro_no_se_instala_la_aplicacion()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.RegistrarDispositivoAutomaticoAsync(It.IsAny<RegistrarPdaAutomaticoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<DispositivoResponse>.Fail("El numero de serie es obligatorio.", 400));
        var adb = new AdbFalso().ConEquipoListo(Serie);
        var sut = CrearServicio(adb, api.Object);

        var resultado = await sut.RegistrarAsync(TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Be("El numero de serie es obligatorio.");
        adb.Ejecutados.Should().NotContain(c => c.Contains("install"));
    }

    [Fact]
    public async Task Una_instalacion_que_no_queda_en_el_equipo_se_reporta_como_fallida()
    {
        var adb = new AdbFalso().ConEquipoListo(Serie);
        adb.Responder($"-s {Serie} shell pm path com.newrich.pda", string.Empty);
        var sut = CrearServicio(adb, ApiQueRegistra().Object);

        var resultado = await sut.RegistrarAsync(TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Be(UiTexts.PdaInstalacionNoVerificada);
    }

    [Fact]
    public async Task Si_el_equipo_se_desconecta_al_iniciar_no_se_registra_nada()
    {
        var api = ApiQueRegistra();
        var adb = new AdbFalso();
        adb.Responder("devices -l", "List of devices attached\n\n");
        var sut = CrearServicio(adb, api.Object);

        var resultado = await sut.RegistrarAsync(TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Be(UiTexts.PdaSinDispositivoConectado);
        api.Verify(a => a.RegistrarDispositivoAutomaticoAsync(
            It.IsAny<RegistrarPdaAutomaticoRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task El_registro_deja_el_puente_usb_listo_para_que_la_app_alcance_la_api()
    {
        var sut = CrearServicio(out var adb, out _);

        await sut.RegistrarAsync(TipoDispositivo.Vendedor, Silencio(), CancellationToken.None);

        adb.Ejecutados.Should().ContainSingle(c => c == $"-s {Serie} reverse tcp:5295 tcp:5295");
    }

    private static IAvanceRegistroPda Silencio() => new AvanceRegistrado();

    /// <summary>Recibe el avance de forma sincrona para poder afirmar el orden exacto.</summary>
    private sealed class AvanceRegistrado : IAvanceRegistroPda
    {
        public List<AvanceRegistroPda> Reportados { get; } = [];

        public Task ReportarAsync(AvanceRegistroPda avance, CancellationToken cancellationToken)
        {
            Reportados.Add(avance);
            return Task.CompletedTask;
        }
    }

    private static RegistroPdaService CrearServicio(out AdbFalso adb, out Mock<IAdminApiClient> api)
    {
        adb = new AdbFalso().ConEquipoListo(Serie);
        api = ApiQueRegistra();
        return CrearServicio(adb, api.Object);
    }

    private static RegistroPdaService CrearServicio(AdbFalso adb, IAdminApiClient api, IApkPda? apk = null) =>
        new(adb, apk ?? new ApkFalso("C:\\apk\\NewRich.apk"), api, Options.Create(new OpcionesRegistroPda
        {
            RutaApk = "C:\\apk\\NewRich.apk"
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
                Tipo = TipoDispositivo.Vendedor,
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

    /// <summary>Doble de adb: guarda cada comando y devuelve respuestas preparadas.</summary>
    private sealed class AdbFalso : IAdb
    {
        private readonly Dictionary<string, AdbResultado> _respuestas = new(StringComparer.Ordinal);

        public List<string> Ejecutados { get; } = [];

        public AdbFalso ConEquipoListo(string serie)
        {
            Responder("devices -l", $"List of devices attached\n{serie}\tdevice product:RMX3710 model:RMX3710 transport_id:1\n");
            Responder($"-s {serie} shell getprop ro.product.model", "RMX3710");
            Responder($"-s {serie} shell settings put global newrich_codigo_dispositivo {CodigoGenerado}", string.Empty);
            Responder($"-s {serie} install -r C:\\apk\\NewRich.apk", "Success");
            Responder($"-s {serie} shell pm path com.newrich.pda", "package:/data/app/com.newrich.pda/base.apk");
            Responder($"-s {serie} reverse tcp:5295 tcp:5295", string.Empty);
            return this;
        }

        public void Responder(string comando, string salida) =>
            _respuestas[comando] = new AdbResultado(0, salida, string.Empty);

        public Task<AdbResultado> EjecutarAsync(IReadOnlyList<string> argumentos, CancellationToken cancellationToken)
        {
            var comando = string.Join(" ", argumentos);
            Ejecutados.Add(comando);
            return Task.FromResult(_respuestas.TryGetValue(comando, out var resultado)
                ? resultado
                : new AdbResultado(1, string.Empty, $"Comando sin respuesta preparada: {comando}"));
        }
    }
}
