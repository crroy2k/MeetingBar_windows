using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.Storage.Pickers;

namespace MeetingBar.SettingsApp;

public sealed partial class MainWindow : Window
{
    private static readonly string[] TimelineDatePatterns = { "dd.MM.yyyy", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "dd MMM yyyy" };
    private static readonly string[] TimelineDateChoices = { "31.12.2026  (dd.MM.yyyy)", "12/31/2026  (MM/dd/yyyy)", "31/12/2026  (dd/MM/yyyy)", "2026-12-31  (yyyy-MM-dd)", "31 dic 2026  (dd MMM yyyy)" };
    private readonly DispatcherTimer _stateTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Dictionary<string, CheckBox> _calendarChecks = new();
    private readonly Dictionary<string, CalendarSourceEditor> _calendarSourceEditors = new();
    private readonly Dictionary<string, InfoBar> _googleStatuses = new();
    private readonly List<(TextBox Name, TextBox Url)> _bookmarkEditors = new();
    private AppSettings _settings = SettingsStore.Load();
    private string _page = "";
    private string _stateSignature = "";

    private ComboBox? _widgetMonitor;
    private ComboBox? _widgetMode, _titleMode, _timeMode, _endOfDayBehavior, _highlightStyle, _timelineDateFormat, _mapProvider;
    private Slider? _widgetWidth;
    private NumberBox? _offsetX, _offsetY, _titleLength, _highlightBefore, _highlightAfter, _refreshMinutes, _autoJoinBefore, _pastMeetingHours, _futureMeetingHours, _timelinePastDays, _timelineFutureDays;
    private TextBox? _widgetColor, _endOfDayCustomMessage, _googleClientId, _browserPath, _browserArgs, _createUrl, _debugTitle, _debugColor;
    private PasswordBox? _googleClientSecret;
    private ToggleSwitch? _autoWidgetColor, _ongoingTime, _showIcon, _winWWidget, _highlightMeetings, _highlightWholeMeeting, _allDay, _widgetAllDay, _declined, _tentative, _free, _ignoreFreeTaskbar, _requireLink, _includePastDay, _includeNextDay, _fullDayEventList, _use24HourClock, _showTimelineDate;
    private ToggleSwitch? _notify, _fullScreen, _notifyEnd, _autoJoin, _hotkey, _startWithWindows;
    private ToggleSwitch? _hideWidgetInFullscreen;
    private ComboBox? _runningFullscreenApp;
    private Button? _addRunningFullscreenApp;
    private StackPanel? _fullscreenAppList;
    private ComboBox? _debugService;
    private readonly Dictionary<int, CheckBox> _notificationChecks = new();
    private NumberBox? _debugStartMinutes, _debugDurationMinutes;
    private InfoBar? _debugStatus;

    private sealed class CalendarSourceEditor
    {
        public required TextBox Name { get; init; }
        public required ToggleSwitch Enabled { get; init; }
        public TextBox? IcsSources { get; init; }
    }

    private sealed class RunningAppOption
    {
        public required string Name { get; init; }
        public required string Path { get; init; }
        public string Display => Name + "  ·  " + System.IO.Path.GetFileName(Path);
    }

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
        ShowPage("Widget");
        Navigation.Loaded += (_, _) => DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            Navigation.SelectedItem = WidgetItem;
            _page = "";
            ShowPage("Widget");
        });
        _stateTimer.Tick += StateTimer_Tick;
        _stateTimer.Start();
        Closed += (_, _) => _stateTimer.Stop();
    }

    private void ConfigureWindow()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        try { SystemBackdrop = new MicaBackdrop(); } catch { }

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        appWindow.Title = "Configuración de MeetingBar";
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
        var display = DisplayArea.GetFromWindowId(id, DisplayAreaFallback.Primary);
        if (display != null)
        {
            var area = display.WorkArea;
            var width = Math.Min(1080, area.Width - 32);
            var height = Math.Min(900, area.Height - 32);
            appWindow.Resize(new SizeInt32(width, height));
            appWindow.Move(new PointInt32(area.X + Math.Max(0, (area.Width - width) / 2), area.Y + Math.Max(0, (area.Height - height) / 2)));
        }
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string page) ShowPage(page);
    }

    private void ShowPage(string page)
    {
        if (_page == page && PagePanel.Children.Count > 0) return;
        CaptureCurrentPage();
        _page = page;
        PagePanel.Children.Clear();
        PageScroller.ChangeView(null, 0, null, true);

        switch (page)
        {
            case "Calendars": BuildCalendarsPage(); break;
            case "Events": BuildEventsPage(); break;
            case "Meetings": BuildMeetingsPage(); break;
            case "Reminders": BuildRemindersPage(); break;
            case "General": BuildGeneralPage(); break;
            case "Debug": BuildDebugPage(); break;
            default: BuildWidgetPage(); break;
        }
    }

    private void AddHeader(string title, string description, string glyph)
    {
        var titleRow = new Grid { Margin = new Thickness(0, 2, 0, 8), ColumnSpacing = 18 };
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition());
        var icon = new FontIcon { Glyph = glyph, FontSize = 48, Foreground = ThemeBrush("AccentFillColorDefaultBrush"), Width = 72, Height = 72 };
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
        text.Children.Add(new TextBlock { Text = title, Style = (Style)Application.Current.Resources["PageTitleStyle"] });
        text.Children.Add(new TextBlock { Text = description, Foreground = ThemeBrush("TextFillColorSecondaryBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
        Grid.SetColumn(text, 1);
        titleRow.Children.Add(icon);
        titleRow.Children.Add(text);
        PagePanel.Children.Add(titleRow);
    }

    private void AddSection(string title) => PagePanel.Children.Add(new TextBlock
    {
        Text = title,
        Style = (Style)Application.Current.Resources["SectionHeaderStyle"]
    });

    private void AddCard(string title, string description, FrameworkElement? control = null)
    {
        var card = new Border { Style = (Style)Application.Current.Resources["SettingsCardStyle"] };
        var row = new Grid { ColumnSpacing = 24 };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, MaxWidth = 490 };
        copy.Children.Add(new TextBlock { Text = title, FontSize = 14, TextWrapping = TextWrapping.Wrap });
        if (!string.IsNullOrWhiteSpace(description))
            copy.Children.Add(new TextBlock { Text = description, FontSize = 12, Foreground = ThemeBrush("TextFillColorSecondaryBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
        row.Children.Add(copy);
        if (control != null)
        {
            control.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(control, 1);
            row.Children.Add(control);
        }
        card.Child = row;
        PagePanel.Children.Add(card);
    }

    private void AddWideCard(string title, string description, FrameworkElement control)
    {
        var card = new Border { Style = (Style)Application.Current.Resources["SettingsCardStyle"] };
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock { Text = title, FontSize = 14, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = description, FontSize = 12, Foreground = ThemeBrush("TextFillColorSecondaryBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, -8, 0, 0) });
        content.Children.Add(control);
        card.Child = content;
        PagePanel.Children.Add(card);
    }

    private static Brush ThemeBrush(string key)
    {
        if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush) return brush;
        return new SolidColorBrush(Colors.Transparent);
    }

    private static ComboBox Choice(IEnumerable<string> values, int selected)
    {
        var combo = new ComboBox { Width = 260, SelectedIndex = selected, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var value in values) combo.Items.Add(value);
        combo.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' TextWrapping='Wrap' MaxWidth='224'/></DataTemplate>");
        return combo;
    }

    private static NumberBox Number(double value, double minimum, double maximum, double step = 1) => new()
    {
        Value = value,
        Minimum = minimum,
        Maximum = maximum,
        SmallChange = step,
        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
        Width = 150
    };

    private static ToggleSwitch Toggle(bool value) => new() { IsOn = value, OnContent = "Activado", OffContent = "Desactivado", HorizontalAlignment = HorizontalAlignment.Right };

    private static TextBox TextEntry(string value, double width = 310) => new() { Text = value ?? "", MinWidth = width };

    private void BuildWidgetPage()
    {
        AddHeader("Widget", "Elige dónde aparece MeetingBar y qué información muestra.", "\uE7F4");
        AddSection("Barra de tareas");
        _widgetMode = Choice(new[] { "Solo icono en la bandeja", "Lado derecho", "Lado izquierdo" }, (int)_settings.WidgetMode);
        AddCard("Ubicación", "Muestra la próxima cita junto a la bandeja del sistema o en el lado izquierdo de la barra.", _widgetMode);

        _widgetMonitor = new ComboBox { MinWidth = 310 };
        _widgetMonitor.Items.Add(new ComboBoxItem { Content = "Monitor principal (automático)", Tag = "" });
        _widgetMonitor.SelectedIndex = 0;
        foreach (var monitor in MeetingBar.Display.MonitorCatalog.GetAll())
        {
            var item = new ComboBoxItem { Content = monitor.ToString(), Tag = monitor.DeviceName };
            _widgetMonitor.Items.Add(item);
            if (string.Equals(monitor.DeviceName, _settings.WidgetMonitorDevice, StringComparison.OrdinalIgnoreCase)) _widgetMonitor.SelectedItem = item;
        }
        if (!string.IsNullOrEmpty(_settings.WidgetMonitorDevice) && _widgetMonitor.SelectedIndex == 0)
        {
            var missing = new ComboBoxItem { Content = _settings.WidgetMonitorDevice + " · Desconectado (usa el principal)", Tag = _settings.WidgetMonitorDevice };
            _widgetMonitor.Items.Add(missing); _widgetMonitor.SelectedItem = missing;
        }
        AddCard("Monitor", "Elige la pantalla del widget. Si se desconecta, se usa la principal hasta que vuelva a conectarse.", _widgetMonitor);

        _widgetWidth = new Slider { Minimum = 140, Maximum = 900, Value = _settings.WidgetWidth, Width = 280, StepFrequency = 10 };
        AddCard("Ancho máximo del widget", "El ancho se adapta al contenido hasta este límite.", _widgetWidth);

        _offsetX = Number(_settings.WidgetOffsetX, -1000, 1000);
        AddCard("Desplazamiento horizontal", "Ajusta la posición horizontal del widget.", _offsetX);
        _offsetY = Number(_settings.WidgetOffsetY, -500, 500);
        AddCard("Desplazamiento vertical", "Ajusta la posición vertical respecto a la barra de tareas.", _offsetY);

        _autoWidgetColor = Toggle(_settings.UseAutomaticWidgetColor);
        AddCard("Usar el color de la barra de Windows", "Se adapta al tema claro u oscuro y al color de la barra de tareas.", _autoWidgetColor);
        _widgetColor = TextEntry(_settings.WidgetBackgroundColor, 170);
        AddCard("Color personalizado del widget", "Se usa cuando el color automático está desactivado. Introduce un valor #RRGGBB.", _widgetColor);

        AddSection("Pantalla completa");
        _hideWidgetInFullscreen = Toggle(_settings.HideWidgetInFullscreen);
        AddCard("Ocultar en pantalla completa", "Oculta el widget mientras una aplicación ocupa toda su pantalla. El icono de la bandeja sigue disponible.", _hideWidgetInFullscreen);
        var fullscreenApps = BuildFullscreenAppEditor();
        fullscreenApps.IsEnabled = _hideWidgetInFullscreen.IsOn;
        _hideWidgetInFullscreen.Toggled += (_, _) => fullscreenApps.IsEnabled = _hideWidgetInFullscreen.IsOn;
        AddWideCard("Ocultar también con estas aplicaciones", "Oculta el widget cuando una aplicación de esta lista esté en primer plano. Útil para juegos sin bordes o en ventana.", fullscreenApps);

        AddSection("Contenido");
        _titleMode = Choice(new[] { "Título completo", "Etiqueta genérica", "Punto", "Oculto" }, (int)_settings.TitleMode);
        AddCard("Título de la reunión", "Muestra el título completo, una etiqueta genérica, un punto o ningún título.", _titleMode);
        _timeMode = Choice(new[] { "Cuenta regresiva", "Hora de inicio", "Oculto", "Tiempo restante u hora de inicio" }, (int)_settings.TimeMode);
        AddCard("Hora de la reunión", "Elige cómo se muestra la hora en el widget.", _timeMode);
        _ongoingTime = Toggle(_settings.ShowOngoingTimeRemaining);
        AddCard("Tiempo restante", "Muestra cuánto falta para que termine la reunión en curso.", _ongoingTime);
        _titleLength = Number(_settings.TitleMaxLength, 8, 200);
        AddCard("Longitud máxima del mensaje", "Limita los caracteres del mensaje personalizado de fin de jornada. Los títulos de las citas se ajustan al ancho del widget.", _titleLength);
        _showIcon = Toggle(_settings.ShowServiceIcon);
        AddCard("Icono del servicio de reunión", "Muestra el icono del servicio junto al título.", _showIcon);
        _endOfDayBehavior = Choice(new[] { "Mostrar la próxima cita", "Ocultar el widget", "Mostrar que no hay más citas hoy", "Mostrar un mensaje personalizado" }, (int)_settings.EndOfDayWidgetBehavior);
        AddCard("Al terminar las citas de hoy", "Elige qué muestra el widget cuando ya no quedan citas para hoy.", _endOfDayBehavior);
        _endOfDayCustomMessage = TextEntry(MeetingBar.Localization.SpanishText.EndOfDayMessage(_settings.EndOfDayCustomMessage), 240);
        _endOfDayCustomMessage.MaxLength = 200;
        AddCard("Mensaje de fin de jornada", "Se usa al seleccionar «Mostrar un mensaje personalizado».", _endOfDayCustomMessage);

        AddSection("Resaltado de reuniones");
        _highlightMeetings = Toggle(_settings.HighlightStartingMeetings);
        AddCard("Resaltar reuniones al comenzar", "Resalta el widget cuando se acerca el inicio de una reunión.", _highlightMeetings);
        _highlightBefore = Number(_settings.HighlightMinutesBefore, 0, 60);
        AddCard("Anticipación del resaltado", "Minutos antes del inicio programado.", _highlightBefore);
        _highlightAfter = Number(_settings.HighlightMinutesAfterStart, 1, 120);
        AddCard("Resaltado después del inicio", "Minutos de resaltado después del inicio, si no se resalta toda la reunión.", _highlightAfter);
        _highlightWholeMeeting = Toggle(_settings.HighlightWholeMeeting);
        AddCard("Resaltar durante toda la reunión", "Mantiene el resaltado hasta la hora de finalización.", _highlightWholeMeeting);
        _highlightStyle = Choice(new[] { "Como al pasar el cursor", "Brillo animado" }, (int)_settings.WidgetHighlightStyle);
        AddCard("Estilo del resaltado", "Elige un fondo resaltado o una animación de brillo suave.", _highlightStyle);

        AddSection("Panel de widgets de Windows (beta)");
        _winWWidget = Toggle(_settings.EnableWinWWidget);
        AddCard("Widget de Win+W", "Agrega MeetingBar al panel de widgets de Windows 11. Después de guardar, pulsa Win+W y fija MeetingBar desde «Agregar widgets». Esta versión requiere el modo de desarrollador.", _winWWidget);
    }

    private Control BuildFullscreenAppEditor()
    {
        _settings.FullscreenAppBlacklist ??= [];
        var content = new StackPanel { Spacing = 10 };
        var pickerRow = new Grid { ColumnSpacing = 8 };
        pickerRow.ColumnDefinitions.Add(new ColumnDefinition());
        pickerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pickerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _runningFullscreenApp = new ComboBox { PlaceholderText = "Elige una aplicación abierta", DisplayMemberPath = nameof(RunningAppOption.Display), HorizontalAlignment = HorizontalAlignment.Stretch };
        _runningFullscreenApp.SelectionChanged += (_, _) => { if (_addRunningFullscreenApp != null) _addRunningFullscreenApp.IsEnabled = _runningFullscreenApp.SelectedItem != null; };
        pickerRow.Children.Add(_runningFullscreenApp);

        _addRunningFullscreenApp = new Button { Content = "Agregar", IsEnabled = false, MinWidth = 72 };
        _addRunningFullscreenApp.Click += (_, _) =>
        {
            if (_runningFullscreenApp?.SelectedItem is RunningAppOption app) AddFullscreenApp(app.Path);
        };
        Grid.SetColumn(_addRunningFullscreenApp, 1);
        pickerRow.Children.Add(_addRunningFullscreenApp);

        var refresh = new Button { Content = new FontIcon { Glyph = "\uE72C", FontSize = 14 }, Width = 40 };
        AutomationProperties.SetName(refresh, "Actualizar la lista de aplicaciones");
        ToolTipService.SetToolTip(refresh, "Actualizar la lista de aplicaciones");
        refresh.Click += (_, _) => RefreshRunningApps();
        Grid.SetColumn(refresh, 2);
        pickerRow.Children.Add(refresh);
        content.Children.Add(pickerRow);

        var browse = new Button { Content = "Buscar un archivo ejecutable", HorizontalAlignment = HorizontalAlignment.Left };
        browse.Click += BrowseFullscreenApp_Click;
        content.Children.Add(browse);

        _fullscreenAppList = new StackPanel { Spacing = 6, Margin = new Thickness(0, 2, 0, 0) };
        content.Children.Add(_fullscreenAppList);
        RefreshRunningApps();
        RebuildFullscreenAppList();
        return new ContentControl { Content = content };
    }

    private void RefreshRunningApps()
    {
        if (_runningFullscreenApp == null) return;
        var apps = new Dictionary<string, RunningAppOption>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.MainWindowHandle == IntPtr.Zero || process.ProcessName.StartsWith("MeetingBar", StringComparison.OrdinalIgnoreCase)) continue;
                    var path = process.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;
                    var details = FileVersionInfo.GetVersionInfo(path);
                    var name = details.FileDescription;
                    if (string.IsNullOrWhiteSpace(name)) name = System.IO.Path.GetFileNameWithoutExtension(path);
                    apps[path] = new RunningAppOption { Name = name, Path = path };
                }
                catch { }
            }
        }
        _runningFullscreenApp.Items.Clear();
        foreach (var app in apps.Values.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)) _runningFullscreenApp.Items.Add(app);
        _runningFullscreenApp.SelectedIndex = -1;
        if (_addRunningFullscreenApp != null) _addRunningFullscreenApp.IsEnabled = false;
    }

    private async void BrowseFullscreenApp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder, ViewMode = PickerViewMode.List };
            picker.FileTypeFilter.Add(".exe");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            var file = await picker.PickSingleFileAsync();
            if (file != null) AddFullscreenApp(file.Path);
        }
        catch (Exception error)
        {
            SaveStatus.Severity = InfoBarSeverity.Error;
            SaveStatus.Message = "No se pudo seleccionar la aplicación: " + error.Message;
            SaveStatus.IsOpen = true;
        }
    }

    private void AddFullscreenApp(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try { path = System.IO.Path.GetFullPath(path); } catch { }
        _settings.FullscreenAppBlacklist ??= [];
        if (!_settings.FullscreenAppBlacklist.Contains(path, StringComparer.OrdinalIgnoreCase)) _settings.FullscreenAppBlacklist.Add(path);
        RebuildFullscreenAppList();
        if (_runningFullscreenApp != null) _runningFullscreenApp.SelectedIndex = -1;
    }

    private void RebuildFullscreenAppList()
    {
        if (_fullscreenAppList == null) return;
        _fullscreenAppList.Children.Clear();
        var paths = (_settings.FullscreenAppBlacklist ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(System.IO.Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
        if (paths.Count == 0)
        {
            _fullscreenAppList.Children.Add(new TextBlock { Text = "No hay aplicaciones agregadas. La detección automática de pantalla completa sigue activa.", FontSize = 12, Foreground = ThemeBrush("TextFillColorSecondaryBrush") });
            return;
        }

        foreach (var path in paths)
        {
            var row = new Border { Background = ThemeBrush("SubtleFillColorSecondaryBrush"), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 7, 7, 7) };
            var grid = new Grid { ColumnSpacing = 10 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(new FontIcon { Glyph = "\uE7C5", FontSize = 18, VerticalAlignment = VerticalAlignment.Center });
            var label = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            label.Children.Add(new TextBlock { Text = System.IO.Path.GetFileNameWithoutExtension(path), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            label.Children.Add(new TextBlock { Text = path, FontSize = 11, Foreground = ThemeBrush("TextFillColorSecondaryBrush"), TextTrimming = TextTrimming.CharacterEllipsis });
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);
            var remove = new Button { Content = new FontIcon { Glyph = "\uE711", FontSize = 12 }, Width = 36, Height = 32, Padding = new Thickness(0) };
            AutomationProperties.SetName(remove, "Quitar " + System.IO.Path.GetFileNameWithoutExtension(path));
            ToolTipService.SetToolTip(remove, "Quitar");
            remove.Click += (_, _) =>
            {
                (_settings.FullscreenAppBlacklist ??= []).RemoveAll(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
                RebuildFullscreenAppList();
            };
            Grid.SetColumn(remove, 2);
            grid.Children.Add(remove);
            row.Child = grid;
            _fullscreenAppList.Children.Add(row);
        }
    }

    private void BuildEventsPage()
    {
        AddHeader("Eventos", "Elige qué eventos del calendario aparecen en MeetingBar.", "\uE8FD");
        AddSection("Filtros de eventos");
        _widgetAllDay = Toggle(_settings.IncludeAllDayEventsInWidget); AddCard("Incluir eventos de todo el día en la barra", "Las citas con hora tienen prioridad. Los feriados y recordatorios siguen visibles en la agenda.", _widgetAllDay);
        _allDay = Toggle(_settings.ShowAllDayEvents); AddCard("Eventos de todo el día", "Muestra eventos que no tienen una hora específica.", _allDay);
        _declined = Toggle(_settings.ShowDeclinedEvents); AddCard("Eventos rechazados", "Incluye las reuniones que has rechazado.", _declined);
        _tentative = Toggle(_settings.ShowTentativeEvents); AddCard("Eventos provisionales y pendientes", "Incluye invitaciones provisionales o que aún no has aceptado.", _tentative);
        _free = Toggle(_settings.ShowFreeEvents); AddCard("Eventos con disponibilidad libre", "Incluye eventos que no marcan ese horario como ocupado.", _free);
        _ignoreFreeTaskbar = Toggle(_settings.IgnoreFreeEventsInTaskbar); AddCard("Omitir eventos libres en la barra y los avisos", "Conserva los eventos libres en la agenda, pero los omite en el widget, la bandeja y los recordatorios.", _ignoreFreeTaskbar);
        _requireLink = Toggle(_settings.RequireMeetingLink); AddCard("Mostrar solo eventos con enlace de reunión", "Oculta eventos que no tienen un enlace reconocido para unirse.", _requireLink);
        AddSection("Período de la agenda");
        _fullDayEventList = Toggle(_settings.ShowFullDayEventList); AddCard("Mostrar días completos", "Muestra todos los eventos de los días habilitados. Las horas de abajo se aplican entonces solo a la línea de tiempo.", _fullDayEventList);
        _includePastDay = Toggle(_settings.IncludePastDay); AddCard("Incluir días anteriores", "Permite consultar días anteriores en la agenda y la línea de tiempo.", _includePastDay);
        _includeNextDay = Toggle(_settings.IncludeNextDay); AddCard("Incluir días siguientes", "Permite consultar días siguientes en la agenda y la línea de tiempo.", _includeNextDay);
        AddSection("Período de la línea de tiempo");
        _timelinePastDays = Number(_settings.TimelinePastDays, 0, 30); AddCard("Días anteriores", "Cantidad máxima de días anteriores que se pueden consultar.", _timelinePastDays);
        _timelineFutureDays = Number(_settings.TimelineFutureDays, 0, 30); AddCard("Días siguientes", "Cantidad máxima de días siguientes que se pueden consultar.", _timelineFutureDays);
        _pastMeetingHours = Number(_settings.PastMeetingHours, 0, 168); AddCard("Horas anteriores", "Horas anteriores a la actual que se muestran en la línea de tiempo de hoy.", _pastMeetingHours);
        _futureMeetingHours = Number(_settings.FutureMeetingHours, 1, 168); AddCard("Horas siguientes", "Horas posteriores a la actual que se muestran en la línea de tiempo de hoy.", _futureMeetingHours);
        AddSection("Formato de fecha y hora");
        _use24HourClock = Toggle(_settings.Use24HourClock); AddCard("Formato de 24 horas", "Muestra 13:30 en lugar de 1:30 p. m. en toda la aplicación.", _use24HourClock);
        _showTimelineDate = Toggle(_settings.ShowTimelineDate); AddCard("Mostrar la fecha en la línea de tiempo", "Muestra la fecha consultada en la esquina superior derecha.", _showTimelineDate);
        _timelineDateFormat = Choice(TimelineDateChoices, Math.Max(0, Array.IndexOf(TimelineDatePatterns, _settings.TimelineDateFormat))); AddCard("Formato de fecha", "Elige el orden y los separadores de la fecha.", _timelineDateFormat);
        _refreshMinutes = Number(_settings.RefreshMinutes, 1, 120); AddCard("Intervalo de actualización", "Minutos entre las actualizaciones automáticas del calendario.", _refreshMinutes);
    }

    private void BuildRemindersPage()
    {
        AddHeader("Recordatorios", "Configura los avisos y el acceso automático a las reuniones.", "\uEA8F");
        AddSection("Notificaciones");
        _notify = Toggle(_settings.NotifyBeforeMeeting); AddCard("Avisos de reuniones", "Muestra una notificación de Windows en cada momento seleccionado.", _notify);
        _notificationChecks.Clear();
        IEnumerable<int> configured = _settings.NotificationScheduleVersion < 1 ? new[] { _settings.NotifyMinutesBefore } : _settings.NotificationMinutesBefore ?? new List<int>();
        var notificationTimes = new[] { 60, 30, 15, 10, 5, 0 }.Concat(configured).Where(x => x >= 0 && x <= 120).Distinct().OrderByDescending(x => x);
        var notificationChoices = new StackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var minutes in notificationTimes)
        {
            var choice = new CheckBox { Content = minutes == 0 ? "Al comenzar la reunión" : minutes + " min antes", IsChecked = configured?.Contains(minutes) == true, HorizontalAlignment = HorizontalAlignment.Right };
            _notificationChecks[minutes] = choice;
            notificationChoices.Children.Add(choice);
        }
        AddCard("Momentos de aviso", "Puedes elegir varios avisos, por ejemplo 5 minutos antes y al comenzar.", notificationChoices);
        _fullScreen = Toggle(_settings.FullscreenReminder); AddCard("Recordatorio en pantalla completa", "Muestra un recordatorio destacado con un botón para unirse.", _fullScreen);
        _notifyEnd = Toggle(_settings.NotifyAtEnd); AddCard("Aviso al finalizar", "Avisa cuando llega la hora de finalización programada.", _notifyEnd);
        AddSection("Acciones automáticas");
        _autoJoin = Toggle(_settings.AutoJoin); AddCard("Unirse automáticamente a las reuniones", "Abre el enlace de reunión a la hora configurada.", _autoJoin);
        _autoJoinBefore = Number(_settings.AutoJoinMinutesBefore, 0, 30); AddCard("Anticipación para unirse", "Minutos antes del inicio de la reunión.", _autoJoinBefore);
        _hotkey = Toggle(_settings.GlobalJoinHotkey); AddCard("Atajo de teclado para unirse", "Pulsa Ctrl+Alt+J para unirte a la reunión actual o siguiente.", _hotkey);
    }

    private void BuildGeneralPage()
    {
        AddHeader("General", "Inicio de la aplicación, privacidad e integración con Windows.", "\uE713");
        AddSection("Inicio");
        _startWithWindows = Toggle(_settings.StartWithWindows);
        AddCard("Iniciar MeetingBar con Windows", "Muestra MeetingBar en la bandeja y la barra de tareas al iniciar sesión.", _startWithWindows);
        AddSection("Privacidad");
        AddCard("Datos del calendario en este equipo", "Los tokens de Google se cifran para el usuario de Windows que ha iniciado sesión.");
        AddCard("Diagnóstico", "Los registros de diagnóstico se guardan en este equipo y no incluyen los títulos ni los enlaces de las reuniones.");
    }

    private void BuildDebugPage()
    {
        AddHeader("Pruebas", "Prueba la interfaz y los avisos con eventos locales, sin modificar tu calendario.", "\uE9D9");
        AddSection("Evento de prueba");
        _debugTitle = TextEntry("Reunión de prueba");
        AddCard("Título", "Se muestra en el widget, la agenda y los recordatorios.", _debugTitle);
        _debugService = Choice(new[] { "Google Meet", "Microsoft Teams", "Zoom", "Sin enlace de reunión" }, 0);
        AddCard("Servicio de reunión", "Elige el servicio y el enlace del evento de prueba.", _debugService);
        _debugStartMinutes = Number(5, -1440, 1440);
        AddCard("Comienza dentro de", "Minutos desde ahora. Usa un valor negativo para simular un evento iniciado o pasado.", _debugStartMinutes);
        _debugDurationMinutes = Number(30, 1, 1440);
        AddCard("Duración", "Duración de la reunión en minutos.", _debugDurationMinutes);
        _debugColor = TextEntry("#0078D4", 180);
        AddCard("Color del calendario", "Color hexadecimal usado por el evento y los indicadores de la aplicación.", _debugColor);

        AddSection("Acciones");
        var actions = new StackPanel { Spacing = 8 };
        var inject = new Button { Content = "Agregar evento de prueba", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        inject.Click += DebugInject_Click;
        var notification = new Button { Content = "Probar notificación" };
        notification.Click += DebugNotification_Click;
        var reminder = new Button { Content = "Probar recordatorio" };
        reminder.Click += DebugReminder_Click;
        var clear = new Button { Content = "Quitar eventos de prueba" };
        clear.Click += DebugClear_Click;
        actions.Children.Add(inject); actions.Children.Add(notification); actions.Children.Add(reminder); actions.Children.Add(clear);
        AddWideCard("Acciones de prueba", "Los eventos de prueba se conservan hasta cerrar MeetingBar o pulsar «Quitar eventos de prueba».", actions);
        _debugStatus = new InfoBar { IsOpen = true, IsClosable = false, Severity = InfoBarSeverity.Informational, Message = "Todo listo. Los eventos de prueba no se guardan en tu calendario.", Margin = new Thickness(0, 10, 0, 0) };
        PagePanel.Children.Add(_debugStatus);
    }

    private DebugRequest CurrentDebugRequest() => new()
    {
        Title = _debugTitle?.Text.Trim() ?? "Reunión de prueba",
        StartMinutes = (int)Finite(_debugStartMinutes?.Value ?? 5, 5),
        DurationMinutes = (int)Finite(_debugDurationMinutes?.Value ?? 30, 30),
        Service = _debugService?.SelectedItem?.ToString() ?? "Google Meet",
        Color = _debugColor?.Text.Trim() ?? "#0078D4"
    };

    private async void DebugInject_Click(object sender, RoutedEventArgs e) => await RunDebugCommand("debug-inject", "Evento de prueba agregado al widget y a la agenda.", true);
    private async void DebugNotification_Click(object sender, RoutedEventArgs e) => await RunDebugCommand("debug-notification", "Notificación de prueba enviada.", false);
    private async void DebugReminder_Click(object sender, RoutedEventArgs e) => await RunDebugCommand("debug-reminder", "Recordatorio de prueba en pantalla completa activado.", false);
    private async void DebugClear_Click(object sender, RoutedEventArgs e) => await RunDebugCommand("debug-clear", "Se quitaron todos los eventos de prueba.", false);

    private async Task RunDebugCommand(string command, string success, bool saveRequest)
    {
        try
        {
            if (saveRequest) SettingsStore.SaveDebugRequest(CurrentDebugRequest());
            await SettingsStore.SendCommandAsync(command);
            if (_debugStatus != null) { _debugStatus.Severity = InfoBarSeverity.Success; _debugStatus.Message = success; }
        }
        catch (Exception error)
        {
            if (_debugStatus != null) { _debugStatus.Severity = InfoBarSeverity.Error; _debugStatus.Message = error.Message; }
        }
    }

    private void BuildMeetingsPage()
    {
        AddHeader("Reuniones", "Configura cómo se abren los enlaces y guarda tus salas habituales.", "\uE714");
        AddSection("Apertura de enlaces");
        _browserPath = TextEntry(_settings.PreferredBrowserPath); AddCard("Navegador preferido", "Ruta del ejecutable. Déjala vacía para usar el navegador predeterminado de Windows.", _browserPath);
        _browserArgs = TextEntry(_settings.PreferredBrowserArguments); AddCard("Argumentos del navegador", "Usa {url} donde deba insertarse el enlace de reunión.", _browserArgs);
        _createUrl = TextEntry(_settings.CreateMeetingUrl); AddCard("Enlace para crear reuniones", "Se abre al pulsar «Crear reunión».", _createUrl);
        _mapProvider = Choice(new[] { "Google Maps", "Bing Maps", "OpenStreetMap" }, Math.Max(0, Array.IndexOf(new[] { "Google Maps", "Bing Maps", "OpenStreetMap" }, _settings.MapProvider)));
        AddCard("Servicio de mapas", "Se usa para abrir la ubicación desde los detalles del evento.", _mapProvider);

        var heading = new Grid { Margin = new Thickness(0, 24, 0, 9) };
        heading.ColumnDefinitions.Add(new ColumnDefinition()); heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heading.Children.Add(new TextBlock { Text = "Favoritos", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        var add = new Button { Content = "Agregar favorito" }; add.Click += AddBookmark_Click; Grid.SetColumn(add, 1); heading.Children.Add(add); PagePanel.Children.Add(heading);
        _bookmarkEditors.Clear();
        foreach (var bookmark in _settings.Bookmarks) AddBookmarkCard(bookmark);
        if (_settings.Bookmarks.Count == 0) AddCard("No hay favoritos", "Agrega una sala habitual para abrirla con un clic.");
    }

    private void AddBookmarkCard(Bookmark bookmark)
    {
        var name = TextEntry(bookmark.Name, 180);
        var url = TextEntry(bookmark.Url, 300);
        var remove = new Button { Content = "Quitar", Tag = bookmark };
        remove.Click += RemoveBookmark_Click;
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        controls.Children.Add(name); controls.Children.Add(url); controls.Children.Add(remove);
        AddWideCard("Reunión favorita", "Nombre y enlace para unirse", controls);
        _bookmarkEditors.Add((name, url));
    }

    private void AddBookmark_Click(object sender, RoutedEventArgs e)
    {
        CaptureCurrentPage();
        _settings.Bookmarks.Add(new Bookmark { Name = "Nueva reunión", Url = "https://" });
        _page = "";
        ShowPage("Meetings");
    }

    private void RemoveBookmark_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Bookmark bookmark) return;
        CaptureCurrentPage();
        _settings.Bookmarks.Remove(bookmark);
        _page = "";
        ShowPage("Meetings");
    }

    private void BuildCalendarsPage()
    {
        AddHeader("Calendarios", "Combina cuentas y fuentes, y elige qué calendarios se muestran.", "\uE787");
        AddSection("Fuentes de calendario");
        var addButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var addOutlook = new Button { Content = "Agregar Outlook", IsEnabled = !_settings.CalendarSources.Any(x => x.Provider == CalendarProviderKind.Outlook) };
        addOutlook.Click += AddOutlookSource_Click;
        var addIcs = new Button { Content = "Agregar ICS" }; addIcs.Click += AddIcsSource_Click;
        var addGoogle = new Button { Content = "Agregar cuenta de Google", Style = (Style)Application.Current.Resources["AccentButtonStyle"] }; addGoogle.Click += AddGoogleSource_Click;
        addButtons.Children.Add(addOutlook); addButtons.Children.Add(addIcs); addButtons.Children.Add(addGoogle);
        AddWideCard("Agregar una fuente de calendario", "Las cuentas de Google comparten el cliente OAuth, con tokens cifrados independientes. Outlook clásico se agrega una sola vez e incluye los calendarios de sus cuentas locales.", addButtons);

        _calendarSourceEditors.Clear();
        _googleStatuses.Clear();
        var state = SettingsStore.LoadState();
        foreach (var source in _settings.CalendarSources) BuildCalendarSourceCard(source, state);
        if (_settings.CalendarSources.Count == 0) AddCard("No hay fuentes de calendario", "Agrega Outlook, un calendario ICS o una cuenta de Google para comenzar.");

        AddSection("Configuración OAuth de Google");
        BuildGoogleSetup();

        AddSection("Calendarios visibles");
        _calendarChecks.Clear();
        if (state.Calendars.Count == 0)
        {
            AddCard("Aún no se han cargado calendarios", "Guarda los cambios, conecta las cuentas de Google que correspondan y actualiza MeetingBar.");
        }
        else
        {
            foreach (var group in state.Calendars.GroupBy(x => x.SourceId))
            {
                var source = _settings.CalendarSources.FirstOrDefault(x => x.Id == group.Key);
                if (source == null) continue;
                PagePanel.Children.Add(new TextBlock { Text = source.Name, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(8, 6, 0, 0) });
                foreach (var calendar in group)
                {
                    var selected = !source.CalendarSelectionConfigured || source.SelectedCalendarIds.Contains(calendar.Id);
                    var check = new CheckBox { IsChecked = selected, Content = calendar.Name, MinWidth = 260 };
                    _calendarChecks[CalendarKey(source.Id, calendar.Id)] = check;
                    AddCard(calendar.Name, (calendar.SourceKind == "Outlook" ? "Outlook clásico" : calendar.SourceKind == "Ics" ? "ICS" : calendar.SourceKind) + " · Color del calendario " + calendar.Color, check);
                }
            }
        }
    }

    private void BuildCalendarSourceCard(CalendarSourceSettings source, SettingsState state)
    {
        var body = new StackPanel { Spacing = 10 };
        var title = source.Provider switch { CalendarProviderKind.Ics => "Archivos o suscripciones ICS", CalendarProviderKind.Google => "Google Calendar", _ => "Outlook clásico" };
        body.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var name = new TextBox { Text = source.Name, Header = "Nombre para mostrar", MinWidth = 320 };
        var enabled = new ToggleSwitch { IsOn = source.Enabled, Header = "Usar esta fuente", OnContent = "Activado", OffContent = "Desactivado" };
        body.Children.Add(name); body.Children.Add(enabled);
        TextBox? icsSources = null;
        if (source.Provider == CalendarProviderKind.Ics)
        {
            icsSources = new TextBox { Text = source.IcsSources, Header = "Archivos ICS o enlaces HTTPS y webcal:// (uno por línea)", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinWidth = 420, Height = 90 };
            body.Children.Add(icsSources);
        }
        if (source.Provider == CalendarProviderKind.Google)
        {
            body.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(source.GoogleAccountEmail) ? "Sin conexión" : "Cuenta: " + source.GoogleAccountEmail, Foreground = ThemeBrush("TextFillColorSecondaryBrush") });
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var connect = new Button { Content = string.IsNullOrWhiteSpace(source.GoogleAccountEmail) ? "Conectar cuenta" : "Reconectar o cambiar de cuenta", Tag = source.Id, Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
            connect.Click += ConnectGoogle_Click;
            var disconnect = new Button { Content = "Desconectar", Tag = source.Id, IsEnabled = !string.IsNullOrWhiteSpace(source.GoogleRefreshTokenProtected) };
            disconnect.Click += DisconnectGoogle_Click;
            buttons.Children.Add(connect); buttons.Children.Add(disconnect); body.Children.Add(buttons);
            var active = string.Equals(state.ActiveSourceId, source.Id, StringComparison.OrdinalIgnoreCase);
            var status = new InfoBar
            {
                IsOpen = active || !string.IsNullOrWhiteSpace(source.GoogleAccountEmail),
                IsClosable = false,
                Severity = active && state.IsBusy ? InfoBarSeverity.Informational : string.IsNullOrWhiteSpace(source.GoogleAccountEmail) ? InfoBarSeverity.Warning : InfoBarSeverity.Success,
                Message = active && !string.IsNullOrWhiteSpace(state.StatusMessage) ? state.StatusMessage : string.IsNullOrWhiteSpace(source.GoogleAccountEmail) ? "Sin conexión" : "Conectado como " + source.GoogleAccountEmail
            };
            _googleStatuses[source.Id] = status;
            body.Children.Add(status);
        }
        var remove = new Button { Content = "Quitar fuente", Tag = source.Id, HorizontalAlignment = HorizontalAlignment.Right };
        remove.Click += RemoveCalendarSource_Click; body.Children.Add(remove);
        _calendarSourceEditors[source.Id] = new CalendarSourceEditor { Name = name, Enabled = enabled, IcsSources = icsSources };
        var card = new Border { Style = (Style)Application.Current.Resources["SettingsCardStyle"] };
        card.Child = body; PagePanel.Children.Add(card);
    }

    private void BuildGoogleSetup()
    {
        var expander = new Expander
        {
            IsExpanded = string.IsNullOrWhiteSpace(_settings.GoogleClientId),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Header = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Cliente OAuth de Google", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                    new TextBlock { Text = "Una configuración para todas las cuentas de Google · Acceso de solo lectura", FontSize = 12, Foreground = ThemeBrush("TextFillColorSecondaryBrush") }
                }
            }
        };
        var body = new StackPanel { Spacing = 12, Margin = new Thickness(0, 10, 0, 6) };
        body.Children.Add(new TextBlock { Text = "MeetingBar no recibe tu contraseña de Google. La autorización de solo lectura se realiza en el navegador.", TextWrapping = TextWrapping.Wrap, Foreground = ThemeBrush("TextFillColorSecondaryBrush") });
        body.Children.Add(GoogleStep("1", "Crea o selecciona un proyecto de Google Cloud", "Abrir proyectos de Google Cloud", "https://console.cloud.google.com/projectselector2/home/dashboard"));
        body.Children.Add(GoogleStep("2", "Habilita la API de Google Calendar en ese mismo proyecto", "Abrir la API de Calendar", "https://console.cloud.google.com/apis/library/calendar-json.googleapis.com"));
        body.Children.Add(GoogleStep("3", "Configura el consentimiento OAuth. Si hace falta, agrégate como usuario de prueba. Incluye calendar.readonly en «Acceso a los datos».", "Abrir el consentimiento OAuth", "https://console.cloud.google.com/auth/overview"));
        body.Children.Add(new TextBox { Text = "https://www.googleapis.com/auth/calendar.readonly", IsReadOnly = true, Header = "Permiso de Google requerido" });
        body.Children.Add(GoogleStep("4", "Crea un cliente OAuth de tipo «Aplicación de escritorio». No requiere una URL de redirección.", "Abrir clientes OAuth", "https://console.cloud.google.com/auth/clients"));
        _googleClientId = new TextBox { Text = _settings.GoogleClientId, Header = "ID de cliente de la aplicación de escritorio", PlaceholderText = "…apps.googleusercontent.com" };
        body.Children.Add(_googleClientId);
        _googleClientSecret = new PasswordBox { Password = _settings.GoogleClientSecret, Header = "Secreto del cliente (oculto)", PasswordRevealMode = PasswordRevealMode.Hidden };
        body.Children.Add(_googleClientSecret);
        expander.Content = body;
        var card = new Border { Style = (Style)Application.Current.Resources["SettingsCardStyle"], Padding = new Thickness(8, 2, 8, 2) };
        card.Child = expander;
        PagePanel.Children.Add(card);
    }

    private Grid GoogleStep(string number, string text, string linkText, string url)
    {
        var grid = new Grid { ColumnSpacing = 10 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        var badge = new Button { Content = number, Width = 28, Height = 28, Padding = new Thickness(0), CornerRadius = new CornerRadius(14), Style = (Style)Application.Current.Resources["AccentButtonStyle"], IsHitTestVisible = false };
        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
        var link = new HyperlinkButton { Content = linkText + " ↗", Tag = url, Padding = new Thickness(0, 3, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        link.Click += OpenLink_Click;
        content.Children.Add(link);
        Grid.SetColumn(content, 1); grid.Children.Add(badge); grid.Children.Add(content);
        return grid;
    }

    private static void OpenLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: string url }) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private async void ConnectGoogle_Click(object sender, RoutedEventArgs e)
    {
        CaptureCurrentPage();
        if (sender is not Button { Tag: string sourceId }) return;
        if (string.IsNullOrWhiteSpace(_settings.GoogleClientId) || !_settings.GoogleClientId.EndsWith(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            if (_googleStatuses.TryGetValue(sourceId, out var invalidStatus)) { invalidStatus.IsOpen = true; invalidStatus.Severity = InfoBarSeverity.Error; invalidStatus.Message = "Introduce abajo el ID del cliente de escritorio terminado en .apps.googleusercontent.com."; }
            return;
        }
        try
        {
            SettingsStore.Save(_settings);
            if (_googleStatuses.TryGetValue(sourceId, out var status)) { status.IsOpen = true; status.Severity = InfoBarSeverity.Informational; status.Message = "Abriendo el inicio de sesión de Google…"; }
            await SettingsStore.SendCommandAsync("connect-google|" + sourceId);
        }
        catch (Exception error)
        {
            if (_googleStatuses.TryGetValue(sourceId, out var status)) { status.IsOpen = true; status.Severity = InfoBarSeverity.Error; status.Message = error.Message; }
        }
    }

    private async void DisconnectGoogle_Click(object sender, RoutedEventArgs e)
    {
        CaptureCurrentPage();
        if (sender is not Button { Tag: string sourceId }) return;
        var source = _settings.CalendarSources.FirstOrDefault(x => x.Id == sourceId);
        if (source == null) return;
        source.GoogleRefreshTokenProtected = "";
        source.GoogleAccountEmail = "";
        SettingsStore.Save(_settings);
        try { await SettingsStore.SendCommandAsync("reload"); } catch { }
        _page = "";
        ShowPage("Calendars");
    }

    private void AddOutlookSource_Click(object sender, RoutedEventArgs e) => AddCalendarSource(CalendarProviderKind.Outlook);
    private void AddIcsSource_Click(object sender, RoutedEventArgs e) => AddCalendarSource(CalendarProviderKind.Ics);
    private void AddGoogleSource_Click(object sender, RoutedEventArgs e) => AddCalendarSource(CalendarProviderKind.Google);

    private void AddCalendarSource(CalendarProviderKind provider)
    {
        CaptureCurrentPage();
        if (provider == CalendarProviderKind.Outlook && _settings.CalendarSources.Any(x => x.Provider == provider)) return;
        _settings.CalendarSources.Add(CalendarSourceSettings.Create(provider));
        _page = "";
        ShowPage("Calendars");
    }

    private void RemoveCalendarSource_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string sourceId }) return;
        CaptureCurrentPage();
        _settings.CalendarSources.RemoveAll(x => x.Id == sourceId);
        _page = "";
        ShowPage("Calendars");
    }

    private static string CalendarKey(string sourceId, string calendarId) => sourceId + "|" + calendarId;

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        CaptureCurrentPage();
        var widgetRegistrationCompleted = false;
        try
        {
            SettingsStore.Save(_settings);
            var widgetStatus = await WinWWidgetRegistration.ApplyAsync(_settings.EnableWinWWidget);
            widgetRegistrationCompleted = true;
            await SettingsStore.SendCommandAsync("reload");
            SaveStatus.Severity = InfoBarSeverity.Success;
            SaveStatus.Message = string.IsNullOrWhiteSpace(widgetStatus) ? "Configuración guardada y aplicada" : widgetStatus;
            SaveStatus.IsOpen = true;
            var hide = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
            hide.Tick += (_, _) => { hide.Stop(); SaveStatus.IsOpen = false; };
            hide.Start();
        }
        catch (Exception error)
        {
            if (_settings.EnableWinWWidget && !widgetRegistrationCompleted)
            {
                _settings.EnableWinWWidget = false;
                if (_winWWidget != null) _winWWidget.IsOn = false;
                SettingsStore.Save(_settings);
                try { await SettingsStore.SendCommandAsync("reload"); } catch { }
            }
            SaveStatus.Severity = InfoBarSeverity.Error;
            SaveStatus.Message = "No se pudieron aplicar los cambios en MeetingBar: " + error.Message;
            SaveStatus.IsOpen = true;
        }
    }

    private void CaptureCurrentPage()
    {
        switch (_page)
        {
            case "Widget":
                if (_widgetMonitor?.SelectedItem is ComboBoxItem monitorItem) _settings.WidgetMonitorDevice = monitorItem.Tag as string ?? "";
                if (_widgetMode != null) _settings.WidgetMode = (WidgetMode)Math.Max(0, _widgetMode.SelectedIndex);
                if (_widgetWidth != null) _settings.WidgetWidth = _widgetWidth.Value;
                if (_offsetX != null) _settings.WidgetOffsetX = Finite(_offsetX.Value, 0);
                if (_offsetY != null) _settings.WidgetOffsetY = Finite(_offsetY.Value, 0);
                if (_hideWidgetInFullscreen != null) _settings.HideWidgetInFullscreen = _hideWidgetInFullscreen.IsOn;
                if (_autoWidgetColor != null) _settings.UseAutomaticWidgetColor = _autoWidgetColor.IsOn;
                if (_widgetColor != null) _settings.WidgetBackgroundColor = _widgetColor.Text.Trim();
                if (_titleMode != null) _settings.TitleMode = (TitleMode)Math.Max(0, _titleMode.SelectedIndex);
                if (_timeMode != null) _settings.TimeMode = (TimeMode)Math.Max(0, _timeMode.SelectedIndex);
                if (_ongoingTime != null) _settings.ShowOngoingTimeRemaining = _ongoingTime.IsOn;
                if (_titleLength != null) _settings.TitleMaxLength = (int)Finite(_titleLength.Value, 64);
                if (_showIcon != null) _settings.ShowServiceIcon = _showIcon.IsOn;
                if (_endOfDayBehavior != null) _settings.EndOfDayWidgetBehavior = (EndOfDayWidgetBehavior)Math.Max(0, _endOfDayBehavior.SelectedIndex);
                if (_endOfDayCustomMessage != null) _settings.EndOfDayCustomMessage = _endOfDayCustomMessage.Text.Trim();
                if (_highlightMeetings != null) _settings.HighlightStartingMeetings = _highlightMeetings.IsOn;
                if (_highlightBefore != null) _settings.HighlightMinutesBefore = (int)Finite(_highlightBefore.Value, 5);
                if (_highlightAfter != null) _settings.HighlightMinutesAfterStart = (int)Finite(_highlightAfter.Value, 10);
                if (_highlightWholeMeeting != null) _settings.HighlightWholeMeeting = _highlightWholeMeeting.IsOn;
                if (_highlightStyle != null) _settings.WidgetHighlightStyle = (WidgetHighlightStyle)Math.Max(0, _highlightStyle.SelectedIndex);
                if (_winWWidget != null) _settings.EnableWinWWidget = _winWWidget.IsOn;
                break;
            case "Calendars":
                if (_googleClientId != null) _settings.GoogleClientId = _googleClientId.Text.Trim();
                if (_googleClientSecret != null) _settings.GoogleClientSecret = _googleClientSecret.Password.Trim();
                foreach (var source in _settings.CalendarSources)
                {
                    if (_calendarSourceEditors.TryGetValue(source.Id, out var editor))
                    {
                        source.Name = string.IsNullOrWhiteSpace(editor.Name.Text) ? CalendarSourceSettings.DefaultName(source.Provider) : editor.Name.Text.Trim();
                        source.Enabled = editor.Enabled.IsOn;
                        if (editor.IcsSources != null) source.IcsSources = editor.IcsSources.Text;
                    }
                    var prefix = source.Id + "|";
                    var checks = _calendarChecks.Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal)).ToList();
                    if (checks.Count > 0)
                    {
                        source.CalendarSelectionConfigured = true;
                        source.SelectedCalendarIds = checks.Where(x => x.Value.IsChecked == true).Select(x => x.Key.Substring(prefix.Length)).ToList();
                    }
                }
                break;
            case "Events":
                if (_allDay != null) _settings.ShowAllDayEvents = _allDay.IsOn;
                if (_widgetAllDay != null) _settings.IncludeAllDayEventsInWidget = _widgetAllDay.IsOn;
                if (_declined != null) _settings.ShowDeclinedEvents = _declined.IsOn;
                if (_tentative != null) _settings.ShowTentativeEvents = _tentative.IsOn;
                if (_free != null) _settings.ShowFreeEvents = _free.IsOn;
                if (_ignoreFreeTaskbar != null) _settings.IgnoreFreeEventsInTaskbar = _ignoreFreeTaskbar.IsOn;
                if (_requireLink != null) _settings.RequireMeetingLink = _requireLink.IsOn;
                if (_pastMeetingHours != null) _settings.PastMeetingHours = (int)Finite(_pastMeetingHours.Value, 3);
                if (_futureMeetingHours != null) _settings.FutureMeetingHours = (int)Finite(_futureMeetingHours.Value, 24);
                if (_includePastDay != null) _settings.IncludePastDay = _includePastDay.IsOn;
                if (_includeNextDay != null) { _settings.IncludeNextDay = _includeNextDay.IsOn; _settings.IncludeTomorrow = _includeNextDay.IsOn; }
                if (_timelinePastDays != null) _settings.TimelinePastDays = (int)Finite(_timelinePastDays.Value, 1);
                if (_timelineFutureDays != null) _settings.TimelineFutureDays = (int)Finite(_timelineFutureDays.Value, 1);
                if (_fullDayEventList != null) _settings.ShowFullDayEventList = _fullDayEventList.IsOn;
                if (_use24HourClock != null) _settings.Use24HourClock = _use24HourClock.IsOn;
                if (_showTimelineDate != null) _settings.ShowTimelineDate = _showTimelineDate.IsOn;
                if (_timelineDateFormat != null) _settings.TimelineDateFormat = TimelineDatePatterns[Math.Max(0, _timelineDateFormat.SelectedIndex)];
                if (_refreshMinutes != null) _settings.RefreshMinutes = (int)Finite(_refreshMinutes.Value, 3);
                break;
            case "Meetings":
                if (_browserPath != null) _settings.PreferredBrowserPath = _browserPath.Text.Trim();
                if (_browserArgs != null) _settings.PreferredBrowserArguments = _browserArgs.Text;
                if (_createUrl != null) _settings.CreateMeetingUrl = _createUrl.Text.Trim();
                if (_mapProvider?.SelectedItem is string mapProvider) _settings.MapProvider = mapProvider;
                _settings.Bookmarks = _bookmarkEditors.Select(x => new Bookmark { Name = x.Name.Text.Trim(), Url = x.Url.Text.Trim() })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name) && Uri.IsWellFormedUriString(x.Url, UriKind.Absolute)).ToList();
                break;
            case "Reminders":
                if (_notify != null) _settings.NotifyBeforeMeeting = _notify.IsOn;
                _settings.NotificationMinutesBefore = _notificationChecks.Where(x => x.Value.IsChecked == true).Select(x => x.Key).OrderByDescending(x => x).ToList();
                _settings.NotifyMinutesBefore = _settings.NotificationMinutesBefore.FirstOrDefault();
                _settings.NotificationScheduleVersion = 1;
                if (_fullScreen != null) _settings.FullscreenReminder = _fullScreen.IsOn;
                if (_notifyEnd != null) _settings.NotifyAtEnd = _notifyEnd.IsOn;
                if (_autoJoin != null) _settings.AutoJoin = _autoJoin.IsOn;
                if (_autoJoinBefore != null) _settings.AutoJoinMinutesBefore = (int)Finite(_autoJoinBefore.Value, 0);
                if (_hotkey != null) _settings.GlobalJoinHotkey = _hotkey.IsOn;
                break;
            case "General":
                if (_startWithWindows != null) _settings.StartWithWindows = _startWithWindows.IsOn;
                break;
        }
    }

    private static double Finite(double value, double fallback) => double.IsFinite(value) ? value : fallback;

    private void StateTimer_Tick(object? sender, object e)
    {
        var state = SettingsStore.LoadState();
        var signature = state.IsBusy + "|" + state.ActiveSourceId + "|" + state.StatusMessage + "|" + state.GoogleAccountEmail + "|" + state.Calendars.Count;
        if (signature == _stateSignature) return;
        _stateSignature = signature;
        if (_page == "Calendars" && !string.IsNullOrWhiteSpace(state.ActiveSourceId) && _googleStatuses.TryGetValue(state.ActiveSourceId, out var status))
        {
            status.IsOpen = true;
            status.Message = string.IsNullOrWhiteSpace(state.StatusMessage) ? "Sin conexión" : state.StatusMessage;
            status.Severity = state.IsBusy ? InfoBarSeverity.Informational : state.StatusMessage.StartsWith("Conectado como ", StringComparison.OrdinalIgnoreCase) ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        }
        if (!state.IsBusy && !string.IsNullOrWhiteSpace(state.ActiveSourceId))
        {
            _settings = SettingsStore.Load();
            if (_page == "Calendars")
            {
                _page = "";
                ShowPage("Calendars");
            }
        }
    }
}
