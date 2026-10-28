using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MeetingBar.SettingsApp;

public sealed partial class MainWindow
{
    private StackPanel? _servicesPanel;

    private void BuildMeetingServices()
    {
        AddSection("Servicios para crear reuniones");
        AddCard("Elige el servicio al crear una reunión", "Agrega el nombre y la URL de programación de cada servicio. «Crear reunión» mostrará tus opciones; no crea ni envía invitaciones por sí solo.");
        _serviceEditors.Clear();
        _servicesPanel = new StackPanel { Spacing = 12 };
        PagePanel.Children.Add(_servicesPanel);
        foreach (var service in _settings.MeetingServices ?? []) AddServiceEditor(service);
        var add = new Button { Content = "Agregar servicio", Margin = new Thickness(0, 8, 0, 12) };
        add.Click += (_, _) => AddServiceEditor(new Bookmark());
        PagePanel.Children.Add(add);
    }

    private void AddServiceEditor(Bookmark service)
    {
        var panel = new StackPanel { Spacing = 8 };
        var name = new TextBox { Header = "Nombre del servicio", Text = service.Name ?? "", PlaceholderText = "Nombre que aparecerá en Crear reunión", MaxLength = 80 };
        var url = new TextBox { Header = "URL para programar reuniones", Text = service.Url ?? "", PlaceholderText = "https://…", MaxLength = 4096 };
        var remove = new Button { Content = "Quitar servicio" };
        var row = (Name: name, Url: url);
        _serviceEditors.Add(row);
        remove.Click += (_, _) => { _serviceEditors.Remove(row); _servicesPanel?.Children.Remove(panel); };
        panel.Children.Add(name); panel.Children.Add(url); panel.Children.Add(remove);
        _servicesPanel!.Children.Add(panel);
    }

    private List<Bookmark> ReadMeetingServices()
    {
        var result = new List<Bookmark>();
        foreach (var row in _serviceEditors)
        {
            var name = row.Name.Text.Trim(); var url = row.Url.Text.Trim();
            if (name.Length == 0 && url.Length == 0) continue;
            if (name.Length == 0 || !Uri.IsWellFormedUriString(url, UriKind.Absolute) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException("Cada servicio necesita un nombre y una URL HTTPS válida, sin credenciales en la dirección.");
            if (result.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Usa un nombre diferente para cada servicio de reuniones.");
            result.Add(new Bookmark { Name = name, Url = url });
        }
        return result;
    }
}
