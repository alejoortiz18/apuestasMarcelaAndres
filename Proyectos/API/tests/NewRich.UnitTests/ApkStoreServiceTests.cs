using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class ApkStoreServiceTests
{
    private const string Clave = "Clave-del-super-2026";
    private static readonly DateTime Ahora = new(2026, 10, 27, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Solo_el_superusuario_con_su_contrasena_crea_la_llave()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        var admin = Usuario(db, RolUsuario.Administrador);

        var deAdmin = await sut.CrearLlaveAsync(admin, Clave, "1.1.1.1", CancellationToken.None);
        var conClaveErrada = await sut.CrearLlaveAsync(super, "otra", "1.1.1.1", CancellationToken.None);
        var creada = await sut.CrearLlaveAsync(super, Clave, "1.1.1.1", CancellationToken.None);

        deAdmin.StatusCode.Should().Be(403);
        conClaveErrada.StatusCode.Should().Be(403);
        creada.IsSuccess.Should().BeTrue(creada.Message);
        creada.Data!.Alias.Should().Be(ApkStoreLimites.AliasLlave);
        creada.Data.Huella.Should().Be(GeneradorFijo.Llave.Huella);
        (await db.LlavesFirmaApk.CountAsync()).Should().Be(1);
        (await db.AuditoriaApk.Select(a => a.Accion).ToListAsync())
            .Should().Contain([AccionesApkStore.LlaveCrear, AccionesApkStore.Rechazada]);
    }

    [Fact]
    public async Task La_llave_existente_no_se_reemplaza()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);

        var otra = await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);

        otra.StatusCode.Should().Be(409);
        otra.Message.Should().Be(ApkStoreMessages.LlaveYaExiste);
        (await db.LlavesFirmaApk.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Descargar_la_llave_entrega_almacen_y_clave_y_queda_en_auditoria()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);

        var descarga = await sut.DescargarLlaveAsync(super, Clave, "2.2.2.2", CancellationToken.None);

        descarga.IsSuccess.Should().BeTrue(descarga.Message);
        Convert.FromBase64String(descarga.Data!.Almacen).Should().Equal(GeneradorFijo.Llave.Almacen);
        descarga.Data.Clave.Should().Be(GeneradorFijo.Llave.Clave);
        descarga.Data.Alias.Should().Be(ApkStoreLimites.AliasLlave);
        var auditoria = await db.AuditoriaApk.SingleAsync(a => a.Accion == AccionesApkStore.LlaveDescargar);
        auditoria.UsuarioId.Should().Be(super);
        auditoria.Ip.Should().Be("2.2.2.2");
    }

    [Fact]
    public async Task Descargar_la_llave_con_la_contrasena_errada_no_la_entrega()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);

        var descarga = await sut.DescargarLlaveAsync(super, "otra", null, CancellationToken.None);

        descarga.StatusCode.Should().Be(403);
        descarga.Data.Should().BeNull();
    }

    [Fact]
    public async Task La_huella_publica_se_consulta_sin_entregar_la_llave()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);

        (await sut.ObtenerHuellaAsync(CancellationToken.None)).StatusCode.Should().Be(404);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);

        var huella = await sut.ObtenerHuellaAsync(CancellationToken.None);

        huella.Data!.Huella.Should().Be(GeneradorFijo.Llave.Huella);
    }

    [Fact]
    public async Task Publicar_guarda_el_apk_firmado_con_la_llave_y_lo_deja_vigente()
    {
        var (sut, db, almacen) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);
        var apk = ApkFirmadoFalso.Firmado(GeneradorFijo.Certificado);

        var publicada = await Publicar(sut, super, apk, 1);
        var vigente = await sut.ObtenerVigenteAsync(CancellationToken.None);

        publicada.IsSuccess.Should().BeTrue(publicada.Message);
        vigente.Data!.NumeroCompilacion.Should().Be(1);
        vigente.Data.HuellaFirma.Should().Be(GeneradorFijo.Llave.Huella);
        vigente.Data.Sha256.Should().Be(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(apk)));
        vigente.Data.TamanoBytes.Should().Be(apk.Length);
        almacen.Archivos.Should().ContainKey(ApkStoreLimites.NombreArchivo(1)).WhoseValue.Should().Equal(apk);
        (await db.AuditoriaApk.CountAsync(a => a.Accion == AccionesApkStore.VersionPublicar)).Should().Be(1);
    }

    [Fact]
    public async Task Publicar_rechaza_un_apk_firmado_con_otra_llave()
    {
        var (sut, db, almacen) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);

        var otraFirma = await Publicar(sut, super, ApkFirmadoFalso.Firmado(ApkFirmadoFalso.CertificadoNuevo()), 1);
        var sinFirma = await Publicar(sut, super, ApkFirmadoFalso.SinFirma(), 1);

        otraFirma.StatusCode.Should().Be(400);
        otraFirma.Message.Should().Be(ApkStoreMessages.FirmaDistinta);
        sinFirma.Message.Should().Be(ApkStoreMessages.SinFirma);
        almacen.Archivos.Should().BeEmpty();
        (await db.VersionesApk.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Publicar_exige_un_numero_mayor_al_vigente()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);
        var apk = ApkFirmadoFalso.Firmado(GeneradorFijo.Certificado);
        await Publicar(sut, super, apk, 5);

        var igual = await Publicar(sut, super, apk, 5);
        var menor = await Publicar(sut, super, apk, 4);

        igual.StatusCode.Should().Be(409);
        menor.Message.Should().Be(ApkStoreMessages.NumeroNoMayor);
    }

    [Fact]
    public async Task Publicar_sin_llave_creada_no_guarda_nada()
    {
        var (sut, db, almacen) = Crear();
        var super = Usuario(db, RolUsuario.Super);

        var publicada = await Publicar(sut, super, ApkFirmadoFalso.Firmado(GeneradorFijo.Certificado), 1);

        publicada.StatusCode.Should().Be(409);
        publicada.Message.Should().Be(ApkStoreMessages.SinLlave);
        almacen.Archivos.Should().BeEmpty();
    }

    [Fact]
    public async Task Publicar_conserva_solo_las_ultimas_versiones_y_borra_el_archivo_viejo()
    {
        var (sut, db, almacen) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);
        var apk = ApkFirmadoFalso.Firmado(GeneradorFijo.Certificado);

        for (var numero = 1; numero <= ApkStoreLimites.MaximoConservadas + 1; numero++)
        {
            await Publicar(sut, super, apk, numero);
        }

        (await db.VersionesApk.Select(v => v.NumeroCompilacion).ToListAsync())
            .Should().BeEquivalentTo(Enumerable.Range(2, ApkStoreLimites.MaximoConservadas));
        almacen.Archivos.Should().NotContainKey(ApkStoreLimites.NombreArchivo(1));
    }

    [Fact]
    public async Task Publicar_avisa_en_vivo_a_los_equipos_con_la_version_nueva()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);

        await Publicar(sut, super, ApkFirmadoFalso.Firmado(GeneradorFijo.Certificado), 7, "1.2");

        Aviso.Avisadas.Should().ContainSingle().Which.NumeroCompilacion.Should().Be(7);
        Aviso.Avisadas[0].HuellaFirma.Should().Be(GeneradorFijo.Llave.Huella);
    }

    [Fact]
    public async Task Listar_entrega_las_versiones_de_la_mas_nueva_a_la_mas_vieja()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);
        var apk = ApkFirmadoFalso.Firmado(GeneradorFijo.Certificado);
        await Publicar(sut, super, apk, 1);
        await Publicar(sut, super, apk, 2);

        var lista = await sut.ListarVersionesAsync(CancellationToken.None);

        lista.Data!.Select(v => v.NumeroCompilacion).Should().Equal(2, 1);
    }

    [Fact]
    public async Task La_auditoria_muestra_lo_ultimo_primero_con_el_nombre_del_usuario()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, "1.1.1.1", CancellationToken.None);
        await sut.DescargarLlaveAsync(super, Clave, "2.2.2.2", CancellationToken.None);

        var auditoria = await sut.ListarAuditoriaAsync(CancellationToken.None);

        auditoria.Data!.Select(a => a.Accion).Should().BeEquivalentTo([AccionesApkStore.LlaveCrear, AccionesApkStore.LlaveDescargar]);
        auditoria.Data.Should().OnlyContain(a => a.Usuario == nameof(RolUsuario.Super));
    }

    [Fact]
    public async Task Abrir_la_vigente_entrega_el_archivo_publicado()
    {
        var (sut, db, _) = Crear();
        var super = Usuario(db, RolUsuario.Super);
        await sut.CrearLlaveAsync(super, Clave, null, CancellationToken.None);
        var apk = ApkFirmadoFalso.Firmado(GeneradorFijo.Certificado);
        await Publicar(sut, super, apk, 1, "1.0");

        var abierta = await sut.AbrirVigenteAsync(CancellationToken.None);

        abierta.Data!.NombreDescarga.Should().Be("newrich-1.0-1.apk");
        using var copia = new MemoryStream();
        await abierta.Data.Contenido.CopyToAsync(copia);
        copia.ToArray().Should().Equal(apk);
    }

    private static Task<NewRich.Shared.Results.Result<NewRich.Application.Contracts.ApkStore.VersionApkResponse>> Publicar(
        IApkStoreService sut, Guid usuario, byte[] apk, int numero, string nombre = "1.0") =>
        sut.PublicarAsync(usuario, new MemoryStream(apk), apk.Length, $"newrich-{nombre}-{numero}.apk", nombre, numero, "3.3.3.3", CancellationToken.None);

    private static Guid Usuario(NewRichDbContext db, RolUsuario rol)
    {
        var (hash, salt) = new Pbkdf2PasswordHasher().Hash(Clave);
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = rol.ToString(),
            NombreUsuario = rol.ToString().ToLowerInvariant() + Guid.NewGuid().ToString("N")[..6],
            Alias = rol.ToString(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Rol = rol,
            Estado = EstadoUsuario.Activo
        };
        db.Usuarios.Add(usuario);
        db.SaveChanges();
        return usuario.UsuarioId;
    }

    private AvisoEnMemoria Aviso { get; } = new();

    private (ApkStoreService Sut, NewRichDbContext Db, AlmacenEnMemoria Almacen) Crear()
    {
        var db = new NewRichDbContext(new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var reloj = Mock.Of<IClock>(c => c.UtcNow == Ahora && c.LocalNow == Ahora.AddHours(-5));
        var almacen = new AlmacenEnMemoria();
        return (new ApkStoreService(db, almacen, new GeneradorFijo(), new Pbkdf2PasswordHasher(), reloj, Aviso), db, almacen);
    }

    private sealed class AvisoEnMemoria : IVersionesTiempoReal
    {
        public List<NewRich.Application.Contracts.ApkStore.VersionApkResponse> Avisadas { get; } = [];

        public Task AvisarVersionPublicadaAsync(NewRich.Application.Contracts.ApkStore.VersionApkResponse version, CancellationToken cancellationToken)
        {
            Avisadas.Add(version);
            return Task.CompletedTask;
        }
    }

    private sealed class GeneradorFijo : IGeneradorLlaveFirma
    {
        public static readonly byte[] Certificado = ApkFirmadoFalso.CertificadoNuevo("CN=NewRich");
        public static readonly LlaveFirmaGenerada Llave = new([7, 7, 7], "clave-de-la-llave", ApkFirmadoFalso.Huella(Certificado));

        public LlaveFirmaGenerada Generar(string alias, DateTime ahoraUtc) => Llave;
    }

    private sealed class AlmacenEnMemoria : IApkAlmacen
    {
        public Dictionary<string, byte[]> Archivos { get; } = [];

        public async Task GuardarAsync(string nombreArchivo, Stream contenido, long tamano, CancellationToken cancellationToken)
        {
            using var copia = new MemoryStream();
            await contenido.CopyToAsync(copia, cancellationToken);
            Archivos[nombreArchivo] = copia.ToArray();
        }

        public Task EliminarAsync(string nombreArchivo, CancellationToken cancellationToken)
        {
            Archivos.Remove(nombreArchivo);
            return Task.CompletedTask;
        }

        public Task<Stream?> AbrirAsync(string nombreArchivo, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(Archivos.TryGetValue(nombreArchivo, out var datos) ? new MemoryStream(datos) : null);
    }
}
