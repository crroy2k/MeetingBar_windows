using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace MeetingBar.SettingsApp;

public sealed partial class MainWindow : Window
{
    private static readonly string[] TimelineDatePatterns = { "dd.MM.yyyy", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "dd MMM yyyy" };
    private static readonly string[] TimelineDateChoices = { "31.12.2026  (dd.MM.yyyy)", "12/31/2026  (MM/dd/yyyy)", "31/12/2026  (dd/MM/yyyy)", "2026-12-31  (yyyy-MM-dd)", "31 Dec 2026  (dd MMM yyyy)" };
    private readonly DispatcherTimer _stateTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Dictionary<string, CheckBox> _calendarChecks = new();
    private readonly List<(TextBox Name, TextBox Url)> _bookmarkEditors = new();
    private AppSettings _settings = SettingsStore.Load();
    private string _page = "";
    private string _stateSignature = "";

    private ComboBox? _widgetMode, _titleMode, _timeMode, _provider, _timelineDateFormat;
    private Slider? _widgetWidth;
    private NumberBox? _offsetX, _offsetY, _titleLength, _refreshMinutes, _notifyBefore, _autoJoinBefore, _pastMeetingHours, _futureMeetingHours, _timelinePastDays, _timelineFutureDays;
    private TextBox? _widgetColor, _icsSources, _googleClientId, _browserPath, _browserArgs, _createUrl, _debugTitle, _debugColor;
    private PasswordBox? _googleClientSecret;
    private ToggleSwitch? _autoWidgetColor, _ongoingTime, _showIcon, _allDay, _declined, _tentative, _free, _ignoreFreeTaskbar, _requireLink, _includePastDay, _includeNextDay, _fullDayEventList, _use24HourClock, _showTimelineDate;
    private ToggleSwitch? _notify, _fullScreen, _notifyEnd, _autoJoin, _hotkey, _startWithWindows;
    private InfoBar? _googleStatus;
    private ComboBox? _debugService;
    private NumberBox? _debugStartMinutes, _debugDurationMinutes;
    private InfoBar? _debugStatus;

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
        appWindow.Title = "MeetingBar Settings";
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

    private static Brush ThemeBrush(string key)
    {
        if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush) return brush;
        return new SolidColorBrush(Colors.Transparent);
    }

    private static ComboBox Choice(IEnumerable<string> values, int selected)
    {
        var combo = new ComboBox { Width = 260, SelectedIndex = selected, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var value in values) combo.Items.Add(value);
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

    private static ToggleSwitch Toggle(bool value) => new() { IsOn = value, OnContent = "Ein", OffContent = "Aus", HorizontalAlignment = HorizontalAlignment.Right };

    private static TextBox TextEntry(string value, double width = 310) => new() { Text = value ?? "", MinWidth = width };

    private void BuildWidgetPage()
    {
        AddHeader("Widget", "Choose where MeetingBar appears and what it shows.", "\uE7F4");
        AddSection("Taskbar surface");
        _widgetMode = Choice(new[] { "Tray icon only", "Right side", "Left side" }, (int)_settings.WidgetMode);
        AddCard("Mode", "Taskbar modes use a borderless overlay because Windows has no third-party text-widget slot.", _widgetMode);

        _widgetWidth = new Slider { Minimum = 140, Maximum = 900, Value = _settings.WidgetWidth, Width = 280, StepFrequency = 10 };
        AddCard("Maximum widget width", "Short titles shrink automatically; longer titles can use this limit.", _widgetWidth);

        _offsetX = Number(_settings.WidgetOffsetX, -1000, 1000);
        AddCard("Horizontal offset", "Fine-tune placement for taskbar tools or display scaling.", _offsetX);
        _offsetY = Number(_settings.WidgetOffsetY, -500, 500);
        AddCard("Vertical offset", "Move the surface relative to the taskbar.", _offsetY);

        _autoWidgetColor = Toggle(_settings.UseAutomaticWidgetColor);
        AddCard("Match Windows taskbar color", "Uses the current Windows light, dark, and taskbar appearance.", _autoWidgetColor);
        _widgetColor = TextEntry(_settings.WidgetBackgroundColor, 170);
        AddCard("Custom widget color", "Used when automatic matching is off. Enter #RRGGBB.", _widgetColor);

        AddSection("Display");
        _titleMode = Choice(Enum.GetNames<TitleMode>(), (int)_settings.TitleMode);
        AddCard("Meeting title", "Choose full title, a generic label, a dot, or hidden.", _titleMode);
        _timeMode = Choice(new[] { "Countdown", "Start time", "Hidden" }, (int)_settings.TimeMode);
        AddCard("Meeting time", "Controls the time text in the taskbar surface.", _timeMode);
        _ongoingTime = Toggle(_settings.ShowOngoingTimeRemaining);
        AddCard("Time remaining", "Show how much time is left during an ongoing meeting.", _ongoingTime);
        _titleLength = Number(_settings.TitleMaxLength, 8, 200);
        AddCard("Maximum title length", "Longer event names are shortened at this character count.", _titleLength);
        _showIcon = Toggle(_settings.ShowServiceIcon);
        AddCard("Colored event icon", "Show the event-colored square beside the title.", _showIcon);
    }

    private void BuildEventsPage()
    {
        AddHeader("Events", "Control which calendar events appear in MeetingBar.", "\uE8FD");
        AddSection("Event filters");
        _allDay = Toggle(_settings.ShowAllDayEvents); AddCard("All-day events", "Show events without a specific time.", _allDay);
        _declined = Toggle(_settings.ShowDeclinedEvents); AddCard("Declined events", "Keep meetings you declined in the list.", _declined);
        _tentative = Toggle(_settings.ShowTentativeEvents); AddCard("Tentative and pending events", "Include invitations that are not accepted yet.", _tentative);
        _free = Toggle(_settings.ShowFreeEvents); AddCard("Free and non-busy events", "Include events that do not block your calendar.", _free);
        _ignoreFreeTaskbar = Toggle(_settings.IgnoreFreeEventsInTaskbar); AddCard("Ignore free events in taskbar", "Keep free events in the expanded popup, but skip them for the taskbar and tray preview.", _ignoreFreeTaskbar);
        _requireLink = Toggle(_settings.RequireMeetingLink); AddCard("Require a meeting link", "Hide events that have no joinable video or web link.", _requireLink);
        AddSection("Event list range");
        _fullDayEventList = Toggle(_settings.ShowFullDayEventList); AddCard("Show complete days", "Show every event on each allowed day. When enabled, the hour values below affect only the timeline.", _fullDayEventList);
        _includePastDay = Toggle(_settings.IncludePastDay); AddCard("Allow previous days", "Enable past-day pages and backward timeline scrubbing.", _includePastDay);
        _includeNextDay = Toggle(_settings.IncludeNextDay); AddCard("Allow following days", "Enable future-day pages and forward timeline scrubbing.", _includeNextDay);
        AddSection("Timeline range");
        _timelinePastDays = Number(_settings.TimelinePastDays, 0, 30); AddCard("Days backward", "Maximum number of earlier days available while scrubbing.", _timelinePastDays);
        _timelineFutureDays = Number(_settings.TimelineFutureDays, 0, 30); AddCard("Days forward", "Maximum number of later days available while scrubbing.", _timelineFutureDays);
        _pastMeetingHours = Number(_settings.PastMeetingHours, 0, 168); AddCard("Past meetings", "Hours before now shown on today's timeline.", _pastMeetingHours);
        _futureMeetingHours = Number(_settings.FutureMeetingHours, 1, 168); AddCard("Next meetings", "Hours after now shown on today's timeline.", _futureMeetingHours);
        AddSection("Time format");
        _use24HourClock = Toggle(_settings.Use24HourClock); AddCard("24-hour clock", "Use 13:30 instead of 1:30 PM throughout MeetingBar.", _use24HourClock);
        _showTimelineDate = Toggle(_settings.ShowTimelineDate); AddCard("Show timeline date", "Display the scrubbed date at the top-right of the timeline.", _showTimelineDate);
        _timelineDateFormat = Choice(TimelineDateChoices, Math.Max(0, Array.IndexOf(TimelineDatePatterns, _settings.TimelineDateFormat))); AddCard("Timeline date format", "Choose the full-date order and separators.", _timelineDateFormat);
        _refreshMinutes = Number(_settings.RefreshMinutes, 1, 120); AddCard("Refresh interval", "Minutes between automatic calendar updates.", _refreshMinutes);
    }

    private void BuildRemindersPage()
    {
        AddHeader("Reminders", "Choose when MeetingBar alerts or joins for you.", "\uEA8F");
        AddSection("Notifications");
        _notify = Toggle(_settings.NotifyBeforeMeeting); AddCard("Notify before meetings", "Show a Windows notification before an event starts.", _notify);
        _notifyBefore = Number(_settings.NotifyMinutesBefore, 0, 120); AddCard("Notification lead time", "Minutes before the meeting starts.", _notifyBefore);
        _fullScreen = Toggle(_settings.FullscreenReminder); AddCard("Full-screen reminder", "Show a prominent meeting reminder with a join action.", _fullScreen);
        _notifyEnd = Toggle(_settings.NotifyAtEnd); AddCard("Meeting-end notification", "Notify when the scheduled meeting time ends.", _notifyEnd);
        AddSection("Automation");
        _autoJoin = Toggle(_settings.AutoJoin); AddCard("Automatically join meetings", "Open a detected meeting link at the configured time.", _autoJoin);
        _autoJoinBefore = Number(_settings.AutoJoinMinutesBefore, 0, 30); AddCard("Join lead time", "Minutes before the meeting starts.", _autoJoinBefore);
        _hotkey = Toggle(_settings.GlobalJoinHotkey); AddCard("Global join shortcut", "Ctrl+Alt+J joins the nearest meeting.", _hotkey);
    }

    private void BuildGeneralPage()
    {
        AddHeader("General", "App startup, privacy, and Windows integration.", "\uE713");
        AddSection("App behavior");
        _startWithWindows = Toggle(_settings.StartWithWindows);
        AddCard("Start MeetingBar with Windows", "Launch the tray and taskbar surfaces when you sign in.", _startWithWindows);
        AddSection("Privacy");
        AddCard("Calendar data stays on this PC", "Google tokens are encrypted with Windows Data Protection for the signed-in user.");
        AddCard("Diagnostics", "Provider results and crash details are stored locally without event titles or meeting URLs.");
    }

    private void BuildDebugPage()
    {
        AddHeader("Debug", "Inject local-only meetings and trigger UI states without changing a real calendar.", "\uE9D9");
        AddSection("Test event");
        _debugTitle = TextEntry("Test meeting");
        AddCard("Title", "Displayed in the taskbar widget, popup, and reminders.", _debugTitle);
        _debugService = Choice(new[] { "Google Meet", "Microsoft Teams", "Zoom", "No meeting link" }, 0);
        AddCard("Meeting service", "Selects the join icon and test link behavior.", _debugService);
        _debugStartMinutes = Number(5, -1440, 1440);
        AddCard("Starts in", "Minutes from now. Use a negative value for an ongoing or past event.", _debugStartMinutes);
        _debugDurationMinutes = Number(30, 1, 1440);
        AddCard("Duration", "Meeting duration in minutes.", _debugDurationMinutes);
        _debugColor = TextEntry("#0078D4", 180);
        AddCard("Calendar color", "Hex color used by the event, tray icon, widget, and timeline.", _debugColor);

        AddSection("Actions");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var inject = new Button { Content = "Inject event", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        inject.Click += DebugInject_Click;
        var notification = new Button { Content = "Test notification" };
        notification.Click += DebugNotification_Click;
        var reminder = new Button { Content = "Test reminder" };
        reminder.Click += DebugReminder_Click;
        var clear = new Button { Content = "Clear events" };
        clear.Click += DebugClear_Click;
        actions.Children.Add(inject); actions.Children.Add(notification); actions.Children.Add(reminder); actions.Children.Add(clear);
        AddCard("Run debug action", "Injected events live only until MeetingBar exits or Clear events is pressed.", actions);
        _debugStatus = new InfoBar { IsOpen = true, IsClosable = false, Severity = InfoBarSeverity.Informational, Message = "Ready. Debug events never write to your calendar.", Margin = new Thickness(0, 10, 0, 0) };
        PagePanel.Children.Add(_debugStatus);
    }

    private DebugRequest CurrentDebugRequest() => new()
    {
        Title = _debugTitle?.Text.Trim() ?? "Test meeting",
        StartMinutes = (int)Finite(_debugStartMinutes?.Value ?? 5, 5),
        DurationMinutes = (int)Finite(_debugDurationMinutes?.Value ?? 30, 30),
        Service = _debugService?.SelectedItem?.ToString() ?? "Google Meet",
        Color = _debugColor?.Text.Trim() ?? "#0078D4"
    };

    private async void DebugInject_Click(object sender, RoutedEventArgs e) => await RunDebugCommand("debug-inject", "Test event injected into the widget and popup.", true);
    private async void DebugNotification_Click(object sender, RoutedEventArgs e) => await RunDebugCommand("debug-notification", "Test notification triggered.", false);
    private async void DebugReminder_Click(object sender, RoutedEventArgs e) => await RunDebugCommand("debug-reminder", "Test full-screen reminder triggered.", false);
    private async void DebugClear_Click(object sender, RoutedEventArgs e) => await RunDebugCommand("debug-clear", "All injected events cleared.", false);

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
        AddHeader("Meetings", "Choose how links open and keep recurring rooms nearby.", "\uE714");
        AddSection("Opening");
        _browserPath = TextEntry(_settings.PreferredBrowserPath); AddCard("Preferred browser executable", "Leave blank to use the Windows default browser.", _browserPath);
        _browserArgs = TextEntry(_settings.PreferredBrowserArguments); AddCard("Browser arguments", "Use {url} where MeetingBar should insert the meeting link.", _browserArgs);
        _createUrl = TextEntry(_settings.CreateMeetingUrl); AddCard("Create meeting URL", "Opened by the Create meeting button.", _createUrl);

        var heading = new Grid { Margin = new Thickness(0, 24, 0, 9) };
        heading.ColumnDefinitions.Add(new ColumnDefinition()); heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heading.Children.Add(new TextBlock { Text = "Bookmarks", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        var add = new Button { Content = "Add bookmark" }; add.Click += AddBookmark_Click; Grid.SetColumn(add, 1); heading.Children.Add(add); PagePanel.Children.Add(heading);
        _bookmarkEditors.Clear();
        foreach (var bookmark in _settings.Bookmarks) AddBookmarkCard(bookmark);
        if (_settings.Bookmarks.Count == 0) AddCard("No bookmarks", "Add a recurring meeting room for one-click access.");
    }

    private void AddBookmarkCard(Bookmark bookmark)
    {
        var name = TextEntry(bookmark.Name, 180);
        var url = TextEntry(bookmark.Url, 300);
        var remove = new Button { Content = "Remove", Tag = bookmark };
        remove.Click += RemoveBookmark_Click;
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        controls.Children.Add(name); controls.Children.Add(url); controls.Children.Add(remove);
        AddCard("Meeting bookmark", "Name and join URL", controls);
        _bookmarkEditors.Add((name, url));
    }

    private void AddBookmark_Click(object sender, RoutedEventArgs e)
    {
        CaptureCurrentPage();
        _settings.Bookmarks.Add(new Bookmark { Name = "New meeting", Url = "https://" });
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
        AddHeader("Calendars", "Connect a calendar source and choose which calendars are visible.", "\uE787");
        AddSection("Calendar source");
        _provider = Choice(new[] { "Classic Outlook", "ICS files or feeds", "Google Calendar" }, (int)_settings.Provider);
        AddCard("Provider", "Outlook uses its local COM API. ICS accepts one file path or HTTPS feed per line.", _provider);
        _icsSources = new TextBox { Text = _settings.IcsSources, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinWidth = 360, Height = 82 };
        AddCard("ICS files and feed URLs", "One local .ics path or HTTPS URL per line.", _icsSources);

        AddSection("Google Calendar");
        BuildGoogleSetup();

        AddSection("Visible calendars");
        _calendarChecks.Clear();
        var state = SettingsStore.LoadState();
        if (state.Calendars.Count == 0)
        {
            AddCard("No calendars loaded yet", "Save, then refresh MeetingBar after connecting your provider.");
        }
        else
        {
            foreach (var calendar in state.Calendars)
            {
                var selected = _settings.SelectedCalendarIds.Count == 0 || _settings.SelectedCalendarIds.Contains(calendar.Id);
                var check = new CheckBox { IsChecked = selected, Content = calendar.Name, MinWidth = 260 };
                _calendarChecks[calendar.Id] = check;
                AddCard(calendar.Name, "Calendar color " + calendar.Color, check);
            }
        }
    }

    private void BuildGoogleSetup()
    {
        var expander = new Expander
        {
            IsExpanded = string.IsNullOrWhiteSpace(_settings.GoogleAccountEmail),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Header = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Connect Google Calendar", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                    new TextBlock { Text = string.IsNullOrWhiteSpace(_settings.GoogleAccountEmail) ? "Four guided steps · read-only access" : "Connected as " + _settings.GoogleAccountEmail, FontSize = 12, Foreground = ThemeBrush("TextFillColorSecondaryBrush") }
                }
            }
        };
        var body = new StackPanel { Spacing = 12, Margin = new Thickness(0, 10, 0, 6) };
        body.Children.Add(new TextBlock { Text = "MeetingBar never sees your Google password. Google asks for read-only calendar access in your browser.", TextWrapping = TextWrapping.Wrap, Foreground = ThemeBrush("TextFillColorSecondaryBrush") });
        body.Children.Add(GoogleStep("1", "Create or select a Google Cloud project", "Open Google Cloud projects", "https://console.cloud.google.com/projectselector2/home/dashboard"));
        body.Children.Add(GoogleStep("2", "Enable the Google Calendar API in that same project", "Open Calendar API page", "https://console.cloud.google.com/apis/library/calendar-json.googleapis.com"));
        body.Children.Add(GoogleStep("3", "Configure OAuth consent, add yourself as a test user if needed, and grant calendar.readonly under Data Access", "Open OAuth consent", "https://console.cloud.google.com/auth/overview"));
        body.Children.Add(new TextBox { Text = "https://www.googleapis.com/auth/calendar.readonly", IsReadOnly = true, Header = "Required Google scope" });
        body.Children.Add(GoogleStep("4", "Create an OAuth client of type Desktop app. No redirect URL is needed.", "Open OAuth clients", "https://console.cloud.google.com/auth/clients"));
        _googleClientId = new TextBox { Text = _settings.GoogleClientId, Header = "Desktop app client ID", PlaceholderText = "…apps.googleusercontent.com" };
        body.Children.Add(_googleClientId);
        _googleClientSecret = new PasswordBox { Password = _settings.GoogleClientSecret, Header = "Client secret (hidden)", PasswordRevealMode = PasswordRevealMode.Hidden };
        body.Children.Add(_googleClientSecret);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var connect = new Button { Content = "Connect Google Calendar", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        connect.Click += ConnectGoogle_Click;
        var disconnect = new Button { Content = "Disconnect" };
        disconnect.Click += DisconnectGoogle_Click;
        buttons.Children.Add(connect); buttons.Children.Add(disconnect); body.Children.Add(buttons);
        var state = SettingsStore.LoadState();
        _googleStatus = new InfoBar
        {
            IsOpen = true,
            IsClosable = false,
            Severity = state.IsBusy ? InfoBarSeverity.Informational : string.IsNullOrWhiteSpace(_settings.GoogleAccountEmail) ? InfoBarSeverity.Warning : InfoBarSeverity.Success,
            Message = !string.IsNullOrWhiteSpace(state.StatusMessage) ? state.StatusMessage : string.IsNullOrWhiteSpace(_settings.GoogleAccountEmail) ? "Not connected" : "Connected as " + _settings.GoogleAccountEmail
        };
        body.Children.Add(_googleStatus);
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
        if (string.IsNullOrWhiteSpace(_settings.GoogleClientId) || !_settings.GoogleClientId.EndsWith(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            if (_googleStatus != null) { _googleStatus.Severity = InfoBarSeverity.Error; _googleStatus.Message = "Enter the Desktop app client ID ending in .apps.googleusercontent.com."; }
            return;
        }
        try
        {
            SettingsStore.Save(_settings);
            if (_googleStatus != null) { _googleStatus.Severity = InfoBarSeverity.Informational; _googleStatus.Message = "Opening Google sign-in…"; }
            await SettingsStore.SendCommandAsync("connect-google");
        }
        catch (Exception error)
        {
            if (_googleStatus != null) { _googleStatus.Severity = InfoBarSeverity.Error; _googleStatus.Message = error.Message; }
        }
    }

    private async void DisconnectGoogle_Click(object sender, RoutedEventArgs e)
    {
        CaptureCurrentPage();
        _settings.GoogleRefreshTokenProtected = "";
        _settings.GoogleAccountEmail = "";
        SettingsStore.Save(_settings);
        try { await SettingsStore.SendCommandAsync("reload"); } catch { }
        if (_googleStatus != null) { _googleStatus.Severity = InfoBarSeverity.Warning; _googleStatus.Message = "Not connected"; }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        CaptureCurrentPage();
        try
        {
            SettingsStore.Save(_settings);
            await SettingsStore.SendCommandAsync("reload");
            SaveStatus.Severity = InfoBarSeverity.Success;
            SaveStatus.Message = "Settings saved and reloaded";
            SaveStatus.IsOpen = true;
            var hide = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
            hide.Tick += (_, _) => { hide.Stop(); SaveStatus.IsOpen = false; };
            hide.Start();
        }
        catch (Exception error)
        {
            SaveStatus.Severity = InfoBarSeverity.Error;
            SaveStatus.Message = "Could not reload MeetingBar: " + error.Message;
            SaveStatus.IsOpen = true;
        }
    }

    private void CaptureCurrentPage()
    {
        switch (_page)
        {
            case "Widget":
                if (_widgetMode != null) _settings.WidgetMode = (WidgetMode)Math.Max(0, _widgetMode.SelectedIndex);
                if (_widgetWidth != null) _settings.WidgetWidth = _widgetWidth.Value;
                if (_offsetX != null) _settings.WidgetOffsetX = Finite(_offsetX.Value, 0);
                if (_offsetY != null) _settings.WidgetOffsetY = Finite(_offsetY.Value, 0);
                if (_autoWidgetColor != null) _settings.UseAutomaticWidgetColor = _autoWidgetColor.IsOn;
                if (_widgetColor != null) _settings.WidgetBackgroundColor = _widgetColor.Text.Trim();
                if (_titleMode != null) _settings.TitleMode = (TitleMode)Math.Max(0, _titleMode.SelectedIndex);
                if (_timeMode != null) _settings.TimeMode = (TimeMode)Math.Max(0, _timeMode.SelectedIndex);
                if (_ongoingTime != null) _settings.ShowOngoingTimeRemaining = _ongoingTime.IsOn;
                if (_titleLength != null) _settings.TitleMaxLength = (int)Finite(_titleLength.Value, 64);
                if (_showIcon != null) _settings.ShowServiceIcon = _showIcon.IsOn;
                break;
            case "Calendars":
                if (_provider != null) _settings.Provider = (CalendarProviderKind)Math.Max(0, _provider.SelectedIndex);
                if (_icsSources != null) _settings.IcsSources = _icsSources.Text;
                if (_googleClientId != null) _settings.GoogleClientId = _googleClientId.Text.Trim();
                if (_googleClientSecret != null) _settings.GoogleClientSecret = _googleClientSecret.Password.Trim();
                if (_calendarChecks.Count > 0) _settings.SelectedCalendarIds = _calendarChecks.Where(x => x.Value.IsChecked == true).Select(x => x.Key).ToList();
                break;
            case "Events":
                if (_allDay != null) _settings.ShowAllDayEvents = _allDay.IsOn;
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
                _settings.Bookmarks = _bookmarkEditors.Select(x => new Bookmark { Name = x.Name.Text.Trim(), Url = x.Url.Text.Trim() })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name) && Uri.IsWellFormedUriString(x.Url, UriKind.Absolute)).ToList();
                break;
            case "Reminders":
                if (_notify != null) _settings.NotifyBeforeMeeting = _notify.IsOn;
                if (_notifyBefore != null) _settings.NotifyMinutesBefore = (int)Finite(_notifyBefore.Value, 5);
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
        var signature = state.IsBusy + "|" + state.StatusMessage + "|" + state.GoogleAccountEmail + "|" + state.Calendars.Count;
        if (signature == _stateSignature) return;
        _stateSignature = signature;
        if (_page == "Calendars" && _googleStatus != null)
        {
            _googleStatus.Message = string.IsNullOrWhiteSpace(state.StatusMessage) ? "Not connected" : state.StatusMessage;
            _googleStatus.Severity = state.IsBusy ? InfoBarSeverity.Informational : string.IsNullOrWhiteSpace(state.GoogleAccountEmail) ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
        }
        if (!state.IsBusy && !string.IsNullOrWhiteSpace(state.GoogleAccountEmail) && state.GoogleAccountEmail != _settings.GoogleAccountEmail)
        {
            var disk = SettingsStore.Load();
            _settings.GoogleRefreshTokenProtected = disk.GoogleRefreshTokenProtected;
            _settings.GoogleAccountEmail = disk.GoogleAccountEmail;
            _settings.Provider = disk.Provider;
            _settings.SelectedCalendarIds = disk.SelectedCalendarIds;
            if (_page == "Calendars")
            {
                _page = "";
                ShowPage("Calendars");
            }
        }
    }
}
