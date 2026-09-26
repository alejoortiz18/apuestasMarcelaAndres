using FluentAssertions;
using NewRich.Admin.Services.Pda;

namespace NewRich.Admin.Tests;

public sealed class RutaAdbTests
{
    [Fact]
    public void Resuelve_adb_relativo_dentro_del_proyecto()
    {
        var raiz = Path.Combine(Path.GetTempPath(), "nr-adb");

        var ruta = RutaEnContenido.Resolver("wwwroot/android/adb.exe", raiz);

        ruta.Should().Be(Path.GetFullPath(Path.Combine(raiz, "wwwroot", "android", "adb.exe")));
    }

    [Fact]
    public void Conserva_una_ruta_absoluta_de_adb()
    {
        RutaEnContenido.Resolver(@"C:\tools\adb.exe", @"D:\app").Should().Be(@"C:\tools\adb.exe");
    }
}
