using NewRich.Pda.Core;

namespace NewRich.Maui.Views;

public static class VersionInstaladaPie
{
    public static Label Crear()
    {
        var compilacion = int.TryParse(AppInfo.Current.BuildString, out var numero) ? numero : 0;
        return new Label
        {
            Text = PdaTexts.VersionInstalada(AppInfo.Current.VersionString, compilacion),
            TextColor = Ui.Muted,
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 4, 16, 16)
        };
    }
}
