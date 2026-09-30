using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewRich.Application.Abstractions;
using NewRich.Constants;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ConfirmacionAccionBdTests
{
    [Fact]
    public async Task Un_token_emitido_en_otra_instancia_sirve_para_publicar_el_apk()
    {
        var reloj = new Reloj(new DateTime(2026, 9, 29, 23, 0, 0, DateTimeKind.Utc));
        using var proveedor = Proveedor(reloj);
        var emisor = NuevaInstancia(proveedor, reloj);
        var receptor = NuevaInstancia(proveedor, reloj);
        var usuarioId = Guid.NewGuid();

        var token = await emisor.EmitirAsync(usuarioId, AccionesProtegidas.ConfiguracionVersionAplicacion, vigencia: ConfirmacionAccion.VigenciaCargaArchivo);
        reloj.Avanzar(TimeSpan.FromMinutes(3));

        (await receptor.ConsumirAsync(token, usuarioId, AccionesProtegidas.ConfiguracionVersionAplicacion)).Should().BeTrue();
        (await receptor.ConsumirAsync(token, usuarioId, AccionesProtegidas.ConfiguracionVersionAplicacion)).Should().BeFalse();
    }

    [Fact]
    public async Task Una_confirmacion_normal_caduca_aunque_la_lea_otra_instancia()
    {
        var reloj = new Reloj(new DateTime(2026, 9, 29, 23, 0, 0, DateTimeKind.Utc));
        using var proveedor = Proveedor(reloj);
        var emisor = NuevaInstancia(proveedor, reloj);
        var receptor = NuevaInstancia(proveedor, reloj);
        var usuarioId = Guid.NewGuid();

        var token = await emisor.EmitirAsync(usuarioId, AccionesProtegidas.UsuariosEliminar);
        reloj.Avanzar(ConfirmacionAccion.Vigencia + TimeSpan.FromSeconds(1));

        (await receptor.ConsumirAsync(token, usuarioId, AccionesProtegidas.UsuariosEliminar)).Should().BeFalse();
    }

    private static ServiceProvider Proveedor(Reloj reloj)
    {
        var servicios = new ServiceCollection();
        var nombre = Guid.NewGuid().ToString();
        servicios.AddDbContext<NewRichDbContext>(opciones => opciones.UseInMemoryDatabase(nombre));
        servicios.AddSingleton<IClock>(reloj);
        return servicios.BuildServiceProvider();
    }

    private static ConfirmacionAccionBd NuevaInstancia(ServiceProvider proveedor, Reloj reloj) =>
        new(proveedor.GetRequiredService<IServiceScopeFactory>(), reloj);

    private sealed class Reloj : IClock
    {
        public Reloj(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; private set; }
        public DateTime LocalNow => UtcNow;
        public void Avanzar(TimeSpan cuanto) => UtcNow = UtcNow.Add(cuanto);
    }
}
