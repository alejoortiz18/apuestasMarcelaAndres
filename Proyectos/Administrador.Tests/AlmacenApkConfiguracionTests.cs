using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.ApkStore;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Loterias;
using NewRich.Constants;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class AlmacenApkConfiguracionTests
{
    [Fact]
    public async Task El_superusuario_ve_la_llave_las_versiones_y_la_auditoria_paginada()
    {
        var api = Api();
        api.Setup(a => a.HuellaLlaveFirmaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<LlaveFirmaResponse>.Ok(new() { Alias = "newrich", Huella = "AB12" }, "ok"));
        api.Setup(a => a.ListarVersionesApkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<VersionApkResponse>>.Ok([new() { NumeroCompilacion = 2 }, new() { NumeroCompilacion = 1 }], "ok"));
        api.Setup(a => a.ListarAuditoriaApkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<AuditoriaApkResponse>>.Ok(
                [.. Enumerable.Range(0, 7).Select(_ => new AuditoriaApkResponse { Accion = AccionesApkStore.LlaveDescargar })], "ok"));

        var modelo = await Abrir(api, RolUsuario.Super, paginaAuditoria: 2);

        modelo.AlmacenApk.Should().NotBeNull();
        modelo.AlmacenApk!.Llave!.Huella.Should().Be("AB12");
        modelo.AlmacenApk.Versiones.Select(v => v.NumeroCompilacion).Should().Equal(2, 1);
        modelo.AlmacenApk.Auditoria.TotalCount.Should().Be(7);
        modelo.AlmacenApk.Auditoria.Items.Should().HaveCount(2);
        modelo.AlmacenApk.Aviso.Should().BeNull();
    }

    [Fact]
    public async Task Sin_llave_creada_el_superusuario_ve_la_seccion_sin_aviso_de_error()
    {
        var api = Api();
        api.Setup(a => a.HuellaLlaveFirmaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<LlaveFirmaResponse>.Fail("Todavía no hay llave.", 404));
        api.Setup(a => a.ListarVersionesApkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<VersionApkResponse>>.Ok([], "ok"));
        api.Setup(a => a.ListarAuditoriaApkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<AuditoriaApkResponse>>.Ok([], "ok"));

        var modelo = await Abrir(api, RolUsuario.Super);

        modelo.AlmacenApk!.Llave.Should().BeNull();
        modelo.AlmacenApk.Aviso.Should().BeNull();
    }

    [Fact]
    public async Task Un_administrador_no_ve_el_almacen_ni_consulta_la_llave()
    {
        var api = Api();

        var modelo = await Abrir(api, RolUsuario.Administrador);

        modelo.AlmacenApk.Should().BeNull();
        api.Verify(a => a.HuellaLlaveFirmaAsync(It.IsAny<CancellationToken>()), Times.Never);
        api.Verify(a => a.ListarAuditoriaApkAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task<ConfiguracionIndexViewModel> Abrir(Mock<IAdminApiClient> api, RolUsuario rol, int paginaAuditoria = 1)
    {
        var roles = rol == RolUsuario.Super
            ? new[] { new Claim(ClaimTypes.Role, nameof(RolUsuario.Super)), new Claim(ClaimTypes.Role, nameof(RolUsuario.Administrador)) }
            : [new Claim(ClaimTypes.Role, rol.ToString())];
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(roles, "prueba")) };
        var sut = new ConfiguracionController(api.Object, new MinutosInactividadSesion(api.Object, new MemoryCache(new MemoryCacheOptions())))
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        var resultado = await sut.Index(null, null, null, null, pageAuditoria: paginaAuditoria, pageSizeAuditoria: 5);
        return resultado.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<ConfiguracionIndexViewModel>().Subject;
    }

    private static Mock<IAdminApiClient> Api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarLoteriasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok([], "ok"));
        api.Setup(x => x.ObtenerConfiguracionOperativaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<ConfiguracionOperativaResponse>.Ok(null, "ok"));
        api.Setup(x => x.ListarNumerosRestringidosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<NumeroRestringidoResponse>>.Ok([], "ok"));
        return api;
    }
}
