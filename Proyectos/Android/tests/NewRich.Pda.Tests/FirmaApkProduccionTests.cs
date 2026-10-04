using System.Security.Cryptography;
using FluentAssertions;

namespace NewRich.Pda.Tests;

public sealed class FirmaApkProduccionTests
{
    private const string HuellaKeystore =
        "4fb8d84d798df80c2969b3109471ac2e95114ca97bbc544e24de95b27de81b18";

    [Fact]
    public void La_compilación_usa_la_llave_de_producción()
    {
        var raiz = Raiz();
        var llave = Path.Combine(raiz, "Proyectos", "Android", "signing", "produccion.keystore");
        var respaldo = Path.Combine(raiz, "documentos", "llaves", "produccion.keystore");
        var props = Path.Combine(raiz, "Proyectos", "Android", "src", "NewRich.Maui", "Directory.Build.props");

        File.Exists(llave).Should().BeTrue("la llave de compilación debe estar en el proyecto");
        File.Exists(respaldo).Should().BeTrue("la llave debe quedar respaldada en documentos");
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(llave))).ToLowerInvariant()
            .Should().Be(HuellaKeystore);
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(respaldo))).ToLowerInvariant()
            .Should().Be(HuellaKeystore);

        var texto = File.ReadAllText(props);
        texto.Should().Contain("AndroidKeyStore");
        texto.Should().Contain("produccion.keystore");
        texto.Should().Contain("androiddebugkey");
    }

    private static string Raiz()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ".git"))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }
}
