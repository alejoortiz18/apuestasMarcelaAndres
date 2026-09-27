using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NewRich.Constants.Messages;
using NewRich.Infrastructure.Storage;

namespace NewRich.UnitTests;

public sealed class OpcionesAzureFilesTests
{
    [Fact]
    public void Si_falta_el_recurso_el_mensaje_nombra_la_clave_y_no_el_secreto()
    {
        var config = new ConfigIndice(new Dictionary<string, string?>
        {
            [OpcionesAzureFiles.ClaveConexion] = "DefaultEndpointsProtocol=https;AccountKey=SECRETO;"
        });

        var resultado = OpcionesAzureFiles.Leer(config);

        resultado.IsSuccess.Should().BeFalse();
        resultado.Message.Should().Be(string.Format(VersionAplicacionMessages.ConfiguracionFaltante, OpcionesAzureFiles.ClaveRecurso));
        resultado.Message.Should().NotContain("SECRETO");
        resultado.Message.Should().NotContain("AccountKey");
    }

    [Fact]
    public void Si_falta_la_conexion_el_mensaje_nombra_esa_clave()
    {
        var resultado = OpcionesAzureFiles.Leer(new ConfigIndice([]));

        resultado.IsSuccess.Should().BeFalse();
        resultado.Message.Should().Be(string.Format(VersionAplicacionMessages.ConfiguracionFaltante, OpcionesAzureFiles.ClaveConexion));
    }

    private sealed class ConfigIndice : IConfiguration
    {
        private readonly Dictionary<string, string?> _valores;

        public ConfigIndice(Dictionary<string, string?> valores) => _valores = valores;

        public string? this[string key]
        {
            get => _valores.GetValueOrDefault(key);
            set => _valores[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken() => throw new NotSupportedException();

        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }
}
