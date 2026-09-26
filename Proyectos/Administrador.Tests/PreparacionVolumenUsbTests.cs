using FluentAssertions;
using NewRich.Admin.Services.Usb;

namespace NewRich.Admin.Tests;

public sealed class PreparacionVolumenUsbTests
{
    [Fact]
    public void Volumen_ntfs_no_se_vuelve_a_formatear()
    {
        PreparacionVolumenUsb.DebeFormatear("NTFS").Should().BeFalse();
        PreparacionVolumenUsb.DebeFormatear("ntfs").Should().BeFalse();
    }

    [Theory]
    [InlineData("FAT32")]
    [InlineData("exFAT")]
    [InlineData("")]
    [InlineData(null)]
    public void Volumen_que_no_es_ntfs_si_se_formatea(string? sistema)
    {
        PreparacionVolumenUsb.DebeFormatear(sistema).Should().BeTrue();
    }

    [Fact]
    public void Vaciar_elimina_archivos_y_carpetas_y_conserva_las_del_sistema()
    {
        var raiz = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "newrich-usb-" + Guid.NewGuid().ToString("N")));
        try
        {
            File.WriteAllText(Path.Combine(raiz.FullName, "dato.txt"), "x");
            var anidada = raiz.CreateSubdirectory("fotos");
            var soloLectura = new FileInfo(Path.Combine(anidada.FullName, "img.bin"));
            File.WriteAllBytes(soloLectura.FullName, [1, 2, 3]);
            soloLectura.Attributes = FileAttributes.ReadOnly;
            raiz.CreateSubdirectory("System Volume Information");
            raiz.CreateSubdirectory("$RECYCLE.BIN");

            var error = PreparacionVolumenUsb.Vaciar(raiz.FullName);

            error.Should().BeNull();
            File.Exists(Path.Combine(raiz.FullName, "dato.txt")).Should().BeFalse();
            Directory.Exists(anidada.FullName).Should().BeFalse();
            Directory.Exists(Path.Combine(raiz.FullName, "System Volume Information")).Should().BeTrue();
            Directory.Exists(Path.Combine(raiz.FullName, "$RECYCLE.BIN")).Should().BeTrue();
        }
        finally
        {
            raiz.Attributes = FileAttributes.Normal;
            foreach (var entrada in raiz.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                entrada.Attributes = FileAttributes.Normal;
            }

            raiz.Delete(true);
        }
    }
}
