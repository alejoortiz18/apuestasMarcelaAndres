using FluentAssertions;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ConfiguracionObservadorTests
{
    [Fact]
    public void Muestra_horario_del_pda_y_el_horario_de_cada_loteria()
    {
        var config = new ConfiguracionOperativaResponse
        {
            HoraApertura = "08:00:00",
            HoraCierre = "18:00:00",
            TopesLoterias =
            [
                new TopeLoteriaResponse { Nombre = "Medellín", HoraInicio = "09:00:00", HoraFin = "11:00:00" },
                new TopeLoteriaResponse { Nombre = "Bogotá", HoraInicio = "09:00:00", HoraFin = "14:00:00" }
            ]
        };

        var filas = ConfiguracionObservador.Filas(config);

        filas.Should().Contain(f => f.Titulo == PdaTexts.HoraAperturaPda && f.Valor == "08:00");
        filas.Should().Contain(f => f.Titulo == PdaTexts.HoraCierrePda && f.Valor == "18:00");
        filas.Should().Contain(f => f.Titulo == "Medellín" && f.Valor == "09:00 - 11:00");
        filas.Should().Contain(f => f.Titulo == "Bogotá" && f.Valor == "09:00 - 14:00");
    }
}
