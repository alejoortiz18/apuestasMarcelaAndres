using NewRich.Application.Contracts.Recaudo;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Data;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Recaudador;

public sealed class RecaudadorSincronizacionPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly LocalDatabase _local;
    private readonly Label _resultado = new() { TextColor = Ui.Ink };

    public RecaudadorSincronizacionPage(NewRichApiClient api, LocalDatabase local)
    {
        _api = api;
        _local = local;
        Title = PdaTexts.SincronizarRecaudo;
        var boton = Ui.Primario(PdaTexts.SincronizarRecaudo);
        boton.Clicked += async (_, _) => await SincronizarAsync(boton);
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            BackgroundColor = Ui.Paper,
            Children =
            {
                new Label { Text = PdaTexts.SincronizarRecaudo, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                boton,
                _resultado
            }
        };
    }

    private async Task SincronizarAsync(Button boton)
    {
        boton.IsEnabled = false;
        try
        {
            var ping = await _api.ConectarAsync(
                PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
                CancellationToken.None);
            if (!ping.IsSuccess)
            {
                _resultado.Text = PdaTexts.SinConexion;
                return;
            }

            var pendientes = await _local.PagosRecaudoPendientesAsync();
            var quedan = new List<PagoPendienteRecaudo>();
            foreach (var pago in pendientes)
            {
                var envio = await _api.RegistrarPagoRecaudoAsync(RecaudoColaPagos.Solicitud(pago), CancellationToken.None);
                if (!envio.IsSuccess)
                {
                    quedan.Add(pago);
                }
            }

            await _local.GuardarPagosRecaudoAsync(quedan);
            var obligaciones = await _api.ObligacionesRecaudoAsync(null, CancellationToken.None);
            if (obligaciones.IsSuccess && obligaciones.Data is not null)
            {
                await _local.GuardarObligacionesRecaudoAsync(RecaudoListas.ConPendientes(obligaciones.Data, quedan, RecaudoListas.HoyEnColombia()));
            }

            _resultado.Text = quedan.Count == 0
                ? PdaTexts.SyncCompletado
                : string.Format(PdaTexts.SyncPagosPendientes, quedan.Count);
        }
        finally
        {
            boton.IsEnabled = true;
        }
    }
}
