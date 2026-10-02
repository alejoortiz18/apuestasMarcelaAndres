using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class VersionAplicacionServiceTests
{
    private static readonly DateTime Inicio = new(2026, 9, 27, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void El_dispositivo_pide_descarga_cuando_su_compilacion_difiere_de_la_vigente()
    {
        ActualizacionAplicacion.RequiereDescarga(77, 78).Should().BeTrue();
        ActualizacionAplicacion.RequiereDescarga(78, 78).Should().BeFalse();
        ActualizacionAplicacion.RequiereDescarga(80, 78).Should().BeTrue();
        ActualizacionAplicacion.PermiteIngresar(descargando: true).Should().BeFalse();
        ActualizacionAplicacion.PermiteIngresar(descargando: false).Should().BeTrue();
    }

    [Fact]
    public async Task La_cuarta_version_elimina_la_primera_y_deja_las_tres_ultimas()
    {
        var (sut, db, almacen) = CreateSut();

        await Publicar(sut, 1);
        await Publicar(sut, 2);
        await Publicar(sut, 3);
        var cuarta = await Publicar(sut, 4);

        cuarta.IsSuccess.Should().BeTrue();
        db.VersionesAplicacion.Should().HaveCount(3);
        db.VersionesAplicacion.Select(v => v.NumeroCompilacion).Should().BeEquivalentTo(new[] { 2, 3, 4 });
        almacen.Archivos.Keys.Should().BeEquivalentTo("2.apk", "3.apk", "4.apk");
    }

    [Fact]
    public async Task La_vigente_es_la_ultima_publicada()
    {
        var (sut, _, _) = CreateSut();
        await Publicar(sut, 10);
        await Publicar(sut, 8);

        var vigente = await sut.ObtenerVigenteAsync(CancellationToken.None);

        vigente.IsSuccess.Should().BeTrue();
        vigente.Data!.NumeroCompilacion.Should().Be(8);
        vigente.Data.Vigente.Should().BeTrue();
    }

    [Fact]
    public async Task Rechaza_una_compilacion_repetida()
    {
        var (sut, db, almacen) = CreateSut();
        await Publicar(sut, 77);

        var repetida = await Publicar(sut, 77);

        repetida.IsSuccess.Should().BeFalse();
        repetida.StatusCode.Should().Be(409);
        repetida.Message.Should().Be(VersionAplicacionMessages.CompilacionRepetida);
        db.VersionesAplicacion.Should().HaveCount(1);
        almacen.Archivos.Should().ContainKey("77.apk");
    }

    [Fact]
    public async Task Rechaza_archivo_vacio_extension_ajena_nombre_invalido_y_tamano_excesivo()
    {
        var (sut, db, _) = CreateSut();

        var vacio = await sut.PublicarAsync(Stream.Null, 0, "app.apk", "1.0", 1, CancellationToken.None);
        var ajeno = await sut.PublicarAsync(new MemoryStream([1, 2, 3]), 3, "app.txt", "1.0", 2, CancellationToken.None);
        var ruta = await sut.PublicarAsync(new MemoryStream([1]), 1, "../app.apk", "1.0", 3, CancellationToken.None);
        var nombre = await sut.PublicarAsync(new MemoryStream([1]), 1, "app.apk", "../1.0", 4, CancellationToken.None);
        var grande = await sut.PublicarAsync(
            new MemoryStream([1]),
            VersionAplicacionLimites.TamanoMaximoBytes + 1,
            "app.apk",
            "1.0",
            5,
            CancellationToken.None);

        vacio.IsSuccess.Should().BeFalse();
        vacio.Message.Should().Be(VersionAplicacionMessages.ArchivoVacio);
        ajeno.Message.Should().Be(VersionAplicacionMessages.NoEsApk);
        ruta.Message.Should().Be(VersionAplicacionMessages.NoEsApk);
        nombre.Message.Should().Be(VersionAplicacionMessages.NombreInvalido);
        grande.Message.Should().Be(VersionAplicacionMessages.ArchivoDemasiadoGrande);
        db.VersionesAplicacion.Should().BeEmpty();
    }

    [Fact]
    public async Task Publicar_guarda_el_archivo_con_el_numero_de_compilacion_y_avisa()
    {
        var (sut, _, almacen, vivo) = CreateSutConAviso();
        var contenido = new byte[] { 1, 2, 3, 4 };

        var resultado = await sut.PublicarAsync(new MemoryStream(contenido), contenido.Length, "com.newrich.pda-Signed.apk", "1.1", 78, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Data!.NombreVersion.Should().Be("1.1");
        almacen.Archivos["78.apk"].Should().Equal(contenido);
        vivo.Avisos.Should().Be(1);
        vivo.Ultima!.NumeroCompilacion.Should().Be(78);
    }

    [Fact]
    public async Task Abrir_la_vigente_devuelve_el_archivo_y_sin_versiones_responde_no_encontrado()
    {
        var (sut, _, _) = CreateSut();
        var ausente = await sut.AbrirVigenteAsync(CancellationToken.None);
        await Publicar(sut, 78);

        var abierto = await sut.AbrirVigenteAsync(CancellationToken.None);

        ausente.IsSuccess.Should().BeFalse();
        ausente.StatusCode.Should().Be(404);
        abierto.IsSuccess.Should().BeTrue();
        abierto.Data!.NombreDescarga.Should().Be("newrich-1.0-78.apk");
        abierto.Data.Contenido.Length.Should().BeGreaterThan(0);
    }

    private static async Task<NewRich.Shared.Results.Result<NewRich.Application.Contracts.Versiones.VersionAplicacionResponse>> Publicar(
        VersionAplicacionService sut,
        int numero)
    {
        var bytes = new byte[] { (byte)numero };
        return await sut.PublicarAsync(new MemoryStream(bytes), bytes.Length, "app.apk", "1.0", numero, CancellationToken.None);
    }

    private static (VersionAplicacionService Sut, NewRichDbContext Db, AlmacenMemoria Almacen) CreateSut()
    {
        var (sut, db, almacen, _) = CreateSutConAviso();
        return (sut, db, almacen);
    }

    private static (VersionAplicacionService Sut, NewRichDbContext Db, AlmacenMemoria Almacen, AvisoSpy Vivo) CreateSutConAviso()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var almacen = new AlmacenMemoria();
        var reloj = new RelojAvance(Inicio);
        var vivo = new AvisoSpy();
        return (new VersionAplicacionService(db, almacen, reloj, vivo), db, almacen, vivo);
    }

    private sealed class RelojAvance : IClock
    {
        private DateTime _utc;
        public RelojAvance(DateTime utcNow) => _utc = utcNow;
        public DateTime UtcNow
        {
            get
            {
                var actual = _utc;
                _utc = _utc.AddMinutes(1);
                return actual;
            }
        }
        public DateTime LocalNow => UtcNow;
    }

    private sealed class AvisoSpy : IVersionesTiempoReal
    {
        public int Avisos { get; private set; }
        public NewRich.Application.Contracts.Versiones.VersionAplicacionResponse? Ultima { get; private set; }

        public Task AvisarVersionPublicadaAsync(
            NewRich.Application.Contracts.Versiones.VersionAplicacionResponse version,
            CancellationToken cancellationToken)
        {
            Avisos++;
            Ultima = version;
            return Task.CompletedTask;
        }
    }

    private sealed class AlmacenMemoria : IApkAlmacen
    {
        public Dictionary<string, byte[]> Archivos { get; } = new(StringComparer.Ordinal);

        public async Task GuardarAsync(string nombreArchivo, Stream contenido, long tamano, CancellationToken cancellationToken)
        {
            using var memoria = new MemoryStream();
            await contenido.CopyToAsync(memoria, cancellationToken);
            Archivos[nombreArchivo] = memoria.ToArray();
        }

        public Task EliminarAsync(string nombreArchivo, CancellationToken cancellationToken)
        {
            Archivos.Remove(nombreArchivo);
            return Task.CompletedTask;
        }

        public Task<Stream?> AbrirAsync(string nombreArchivo, CancellationToken cancellationToken)
        {
            if (!Archivos.TryGetValue(nombreArchivo, out var bytes))
            {
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new MemoryStream(bytes));
        }
    }
}
