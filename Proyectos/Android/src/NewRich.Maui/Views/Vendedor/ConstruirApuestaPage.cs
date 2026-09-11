using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Pda.Core.Ventas;
using NewRich.Maui.Data;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Vendedor;

public sealed class ConstruirApuestaPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _offline;
    private readonly IServiceProvider _services;
    private IReadOnlyList<LoteriaResponse> _loterias = [];

    public ConstruirApuestaPage(NewRichApiClient api, SesionPda sesion, LocalDatabase offline, IServiceProvider services)
    {
        _api = api;
        _sesion = sesion;
        _offline = offline;
        _services = services;
        Title = PdaTexts.JuegoNuevo;
        BackgroundColor = Ui.Paper;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var loterias = await _api.LoteriasAsync(CancellationToken.None);
        _loterias = loterias.IsSuccess && loterias.Data is not null
            ? loterias.Data.Where(l => l.Estado == EstadoGeneral.Activo).ToArray()
            : [];
        Render();
    }

    private void Render()
    {
        var draft = _sesion.Borrador;
        if (draft is null)
        {
            Content = new Label { Text = PdaTexts.TipoApuestaAyuda, Padding = 16 };
            return;
        }

        var combinada = draft.Tipo == TipoApuesta.COMBINADO;
        var numero = Ui.Entero("1234", 4);
        var valor = Ui.Entero("1000");
        var checks = new Dictionary<Guid, CheckBox>();
        var loteriasBox = new VerticalStackLayout { Spacing = 6 };
        Picker? picker = null;
        if (combinada)
        {
            foreach (var loteria in _loterias)
            {
                var check = new CheckBox();
                checks[loteria.LoteriaId] = check;
                loteriasBox.Children.Add(new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        check,
                        new Label { Text = loteria.Nombre, VerticalOptions = LayoutOptions.Center, TextColor = Ui.Ink }
                    }
                });
            }
        }
        else
        {
            picker = new Picker { ItemsSource = _loterias.Select(l => l.Nombre).ToList(), Title = PdaTexts.Loteria };
            if (_loterias.Count > 0)
            {
                picker.SelectedIndex = 0;
            }
        }

        var agregar = Ui.Primario(draft.AlMaximo
            ? (combinada ? PdaTexts.MaximoJuegos : PdaTexts.MaximoLineas)
            : (combinada ? PdaTexts.AgregarJuego : PdaTexts.AgregarLinea));
        agregar.IsEnabled = !draft.AlMaximo;
        agregar.Clicked += async (_, _) =>
        {
            if (!decimal.TryParse(EntradaEntera.SoloDigitos(valor.Text), out var monto) || monto != decimal.Truncate(monto))
            {
                await this.AvisoAsync(PdaTexts.JuegoNuevo, ValidationMessages.ValorApuestaMayorCero, PdaTexts.Cerrar);
                return;
            }

            IReadOnlyList<Guid> ids;
            IReadOnlyList<string> nombres;
            if (combinada)
            {
                ids = checks.Where(c => c.Value.IsChecked).Select(c => c.Key).ToArray();
                nombres = _loterias.Where(l => ids.Contains(l.LoteriaId)).Select(l => l.Nombre).ToArray();
            }
            else
            {
                var sel = picker!.SelectedIndex;
                if (sel < 0 || sel >= _loterias.Count)
                {
                    await this.AvisoAsync(PdaTexts.JuegoNuevo, ValidationMessages.LoteriasRequeridas, PdaTexts.Cerrar);
                    return;
                }

                ids = [_loterias[sel].LoteriaId];
                nombres = [_loterias[sel].Nombre];
            }

            var resultado = draft.AgregarLinea(EntradaEntera.SoloDigitos(numero.Text), monto, ids, nombres);
            if (!resultado.IsSuccess)
            {
                await this.AvisoAsync(PdaTexts.JuegoNuevo, resultado.Message, PdaTexts.Cerrar);
                return;
            }

            Render();
        };

        var tabla = new VerticalStackLayout { Spacing = 6 };
        if (draft.Lineas.Count == 0)
        {
            tabla.Children.Add(new Label { Text = PdaTexts.SinJuegos, TextColor = Ui.Muted });
        }
        else
        {
            for (var i = 0; i < draft.Lineas.Count; i++)
            {
                var indice = i;
                var linea = draft.Lineas[i];
                var quitar = new Button { Text = PdaTexts.Quitar, BackgroundColor = Ui.Danger, TextColor = Colors.White, FontSize = 12 };
                quitar.Clicked += async (_, _) =>
                {
                    var ok = await this.ConfirmarAsync(PdaTexts.EliminarJuego, $"¿Está seguro de eliminar este juego? Se descontará {FormatoDinero.Pesos(linea.TotalLinea)} del total.", PdaTexts.SiEliminar, PdaTexts.No);
                    if (!ok)
                    {
                        return;
                    }

                    draft.QuitarLinea(indice);
                    Render();
                };
                tabla.Children.Add(new Frame
                {
                    BorderColor = Ui.Line,
                    BackgroundColor = Colors.White,
                    Content = new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            new Label { Text = $"{linea.Numero}  {FormatoDinero.Pesos(linea.Valor)}  {string.Join(", ", linea.LoteriaNombres)}", VerticalOptions = LayoutOptions.Center, TextColor = Ui.Ink },
                            quitar
                        }
                    }
                });
            }
        }

        var jugar = Ui.Primario(PdaTexts.Jugar);
        jugar.IsEnabled = draft.Lineas.Count > 0;
        jugar.Clicked += async (_, _) => await JugarAsync(draft);

        var cancelar = Ui.Secundario(PdaTexts.CancelarBoleto);
        cancelar.Clicked += async (_, _) =>
        {
            if (await this.ConfirmarAsync(PdaTexts.CancelarBoleto, PdaTexts.CancelarBoletoConfirma, PdaTexts.SiCancelar, PdaTexts.No))
            {
                _sesion.Borrador = null;
                await Shell.Current.GoToAsync("//inicio");
            }
        };

        var form = new VerticalStackLayout { Spacing = 8 };
        form.Children.Add(Ui.Campo(PdaTexts.NumeroApostado));
        form.Children.Add(numero);
        form.Children.Add(Ui.Campo(PdaTexts.ValorApostado));
        form.Children.Add(valor);
        form.Children.Add(Ui.Campo(combinada ? PdaTexts.Loterias : PdaTexts.Loteria));
        if (combinada)
        {
            form.Children.Add(loteriasBox);
        }
        else if (picker is not null)
        {
            form.Children.Add(picker);
        }

        form.Children.Add(agregar);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    Ui.Banner($"{PdaTexts.TipoApuestaAyuda} {TipoApuestaEtiqueta.Texto(draft.Tipo)}.", Ui.InfoBg, Ui.Info),
                    Ui.Tarjeta(form),
                    new Label { Text = PdaTexts.JuegosDelBoleto, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    tabla,
                    new Frame
                    {
                        BackgroundColor = Ui.Dark,
                        BorderColor = Ui.Dark,
                        CornerRadius = 12,
                        Padding = 14,
                        Content = new HorizontalStackLayout
                        {
                            HorizontalOptions = LayoutOptions.Fill,
                            Children =
                            {
                                new Label { Text = PdaTexts.TotalApostado, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center },
                                new Label { Text = FormatoDinero.Pesos(draft.Total), FontAttributes = FontAttributes.Bold, FontSize = 19, TextColor = Colors.White }
                            }
                        }
                    },
                    jugar,
                    cancelar
                }
            }
        };
    }

    private async Task JugarAsync(TicketDraft draft)
    {
        var ok = await this.ConfirmarAsync(PdaTexts.ConfirmarVenta, $"{PdaTexts.ValorTotalPagar}\n{FormatoDinero.Pesos(draft.Total)}\n\n{PdaTexts.SinDatosComprador}", PdaTexts.AceptarYPagar, PdaTexts.Cancelar);
        if (!ok)
        {
            return;
        }

        if (PoliticaVentaPda.IntentarServidorAunqueAndroidReporteSinRed)
        {
            var conexion = await _api.ConectarAsync(
                PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
                CancellationToken.None);
            if (conexion.IsSuccess)
            {
                try
                {
                    var venta = await _api.ConfirmarVentaAsync(draft.ARequest(), Guid.NewGuid().ToString("N"), CancellationToken.None);
                    if (!venta.IsSuccess || venta.Data is null)
                    {
                        if (venta.Message == VentaMessages.VentaFueraDeHorario)
                        {
                            _sesion.HorarioCerrado = true;
                        }

                        await this.AvisoAsync(PdaTexts.JuegoNuevo, venta.Message, PdaTexts.Cerrar);
                        return;
                    }

                    var vigencia = _sesion.Limites.VigenciaPremiosDias > 0 ? _sesion.Limites.VigenciaPremiosDias : 30;
                    _sesion.Tirilla = TirillaVenta.DesdeVenta(venta.Data, draft.Tipo, vigencia, false, _sesion.Limites.LeyendaTirilla);
                    _sesion.Borrador = null;
                    await Navigation.PushAsync(_services.GetRequiredService<TirillaVendidaPage>());
                    return;
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        var disponibles = await _offline.ContarDisponiblesAsync();
        if (PoliticaVentaPda.TrasFalloDeRed(disponibles) == CanalVenta.Bloqueado)
        {
            await this.AvisoAsync(PdaTexts.SinCodigosOffline, PdaTexts.BorradorConservado, PdaTexts.Entendido);
            return;
        }

        var continuar = await this.ConfirmarAsync(PdaTexts.ConexionNoDisponible, PdaTexts.SinConexionServidor, PdaTexts.ContinuarOffline, PdaTexts.EsperarConexion);
        if (!continuar)
        {
            return;
        }

        var codigo = await _offline.ConsumirAsync();
        var payload = EvidenciaOffline.ContenidoQr(codigo?.Payload ?? string.Empty);
        await MostrarTirillaAsync(codigo?.Consecutivo ?? string.Empty, draft, true, payload);
    }

    private async Task MostrarTirillaAsync(string codigo, TicketDraft draft, bool offline, string? qr = null)
    {
        var vigencia = _sesion.Limites.VigenciaPremiosDias > 0 ? _sesion.Limites.VigenciaPremiosDias : 30;
        _sesion.Tirilla = new TirillaVenta
        {
            CodigoImpreso = codigo,
            Tipo = draft.Tipo,
            Fecha = DateTime.Now,
            Total = draft.Total,
            Lineas = draft.Lineas.ToArray(),
            VigenciaDias = vigencia,
            QrContenido = qr ?? string.Empty,
            Offline = offline,
            LeyendaCompleta = TirillaCuerpo.Leyenda(vigencia, _sesion.Limites.LeyendaTirilla)
        };
        _sesion.Borrador = null;
        await Navigation.PushAsync(_services.GetRequiredService<TirillaVendidaPage>());
    }
}
