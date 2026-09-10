using NewRich.Application.Contracts.Premios;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Vendedor;

public sealed class ValidarTicketPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly Entry _codigo = Ui.Entrada("AOL-0000001");
    private readonly Label _resultado = new() { FontSize = 13, TextColor = Ui.Ink };

    public ValidarTicketPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.ValidarTicket;
        BackgroundColor = Ui.Paper;
        var reportar = Ui.Primario(PdaTexts.ReportarCaso);
        reportar.Clicked += async (_, _) => await ReportarAsync();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 10,
                Children =
                {
                    new Label { Text = PdaTexts.ValidarTicketAyuda, TextColor = Ui.Muted, FontSize = 13 },
                    Ui.Campo(PdaTexts.TicketCode),
                    _codigo,
                    reportar,
                    _resultado
                }
            }
        };
    }

    private async Task ReportarAsync()
    {
        var resultado = await _api.ReportarPremioAsync(new ReportarCasoGanadorRequest
        {
            TicketCode = _codigo.Text?.Trim() ?? string.Empty
        }, CancellationToken.None);
        _resultado.Text = resultado.IsSuccess
            ? $"{resultado.Message} {resultado.Data?.Ticket} {resultado.Data?.Estado}"
            : resultado.Message;
        _resultado.TextColor = resultado.IsSuccess ? Ui.Green : Ui.Danger;
    }
}
