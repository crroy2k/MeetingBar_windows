using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MeetingBar.SettingsApp;

public sealed partial class MainWindow
{
    private FrameworkElement ColorEditor(TextBox text)
    {
        text.Width = 112;
        text.MaxLength = 9;
        AutomationProperties.SetName(text, "Código hexadecimal del color del widget");
        var preview = new Border { Width = 32, Height = 24, CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Colors.Gray) };
        var button = new Button { Content = preview, Padding = new Thickness(6), VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(button, "Elegir color del widget");
        ToolTipService.SetToolTip(button, "Elegir color del widget");
        var picker = new ColorPicker { IsAlphaEnabled = false, IsColorSliderVisible = true, IsColorChannelTextInputVisible = true, IsHexInputVisible = true, Width = 300 };
        var flyout = new Flyout { Content = picker, Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedRight };
        button.Flyout = flyout;
        var changing = false;
        void Refresh()
        {
            if (changing) return;
            var hex = text.Text.Trim().TrimStart('#');
            if ((hex.Length != 6 && hex.Length != 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                ToolTipService.SetToolTip(text, "Introduce un color hexadecimal válido, por ejemplo #009F81.");
                preview.Opacity = 0.35;
                return;
            }
            var color = ColorHelper.FromArgb(hex.Length == 8 ? (byte)(value >> 24) : (byte)255, (byte)(value >> 16), (byte)(value >> 8), (byte)value);
            preview.Background = new SolidColorBrush(color); preview.Opacity = 1;
            changing = true; picker.Color = color; changing = false;
            ToolTipService.SetToolTip(text, "Color seleccionado: " + text.Text);
        }
        picker.ColorChanged += (_, args) => {
            if (changing) return;
            changing = true;
            var color = args.NewColor;
            text.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            preview.Background = new SolidColorBrush(color); preview.Opacity = 1;
            changing = false;
        };
        text.TextChanged += (_, _) => Refresh();
        Refresh();
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(button); row.Children.Add(text);
        return row;
    }

    private static string NormalizeWidgetColor(string value)
    {
        var hex = value.Trim().TrimStart('#');
        if ((hex.Length != 6 && hex.Length != 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            throw new InvalidOperationException("Elige un color del widget o escribe un código hexadecimal válido (#RRGGBB).");
        return "#" + hex.ToUpperInvariant();
    }
}
