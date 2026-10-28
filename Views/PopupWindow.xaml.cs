using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using MeetingBar.Windows.Interop;
using MeetingBar.Windows.Models;
using MeetingBar.Windows.ViewModels;

namespace MeetingBar.Windows.Views
{
    public partial class PopupWindow : Window
    {
        private readonly Action _openSettings;
        private IntPtr _handle;
        private bool _isClosing;
        private bool _isAnimating;
        private bool _isPreloaded;
        private bool _isPreloading;
        private bool _isOpen;
        private double _restingTop;
        private int _pageTransitionVersion;
        private readonly DispatcherTimer _detailsOpenTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        private readonly DispatcherTimer _detailsCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
        private EventDetailsWindow _eventDetails;
        private FrameworkElement _pendingEventRow;
        private CalendarEvent _pendingEvent;
        private bool _eventRowHovered;

        public bool IsOpen => _isOpen;
        public bool WasJustDismissed => DateTime.UtcNow - LastHiddenAtUtc < TimeSpan.FromMilliseconds(350);
        public DateTime LastHiddenAtUtc { get; private set; } = DateTime.MinValue;

        public PopupWindow(MainViewModel viewModel, Action openSettings)
        {
            App.ApplyWindowsAccent();
            InitializeComponent();
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0,
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                UseAeroCaptionButtons = false
            });
            DataContext = viewModel;
            _openSettings = openSettings;
            _detailsOpenTimer.Tick += (s, e) => { _detailsOpenTimer.Stop(); ShowPendingEventDetails(); };
            _detailsCloseTimer.Tick += (s, e) =>
            {
                _detailsCloseTimer.Stop();
                if (_eventRowHovered || _eventDetails?.IsMouseOver == true) return;
                HideEventDetails();
                if (!IsActive && !IsMouseOver && _isOpen && !_isAnimating) HideAnimated();
            };
            SourceInitialized += Window_SourceInitialized;
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) HideAnimated(); };
        }

        public void Preload()
        {
            if (_isPreloaded) return;
            _isPreloading = true;
            ShowActivated = false;
            Left = SystemParameters.VirtualScreenLeft - Width - 100;
            Top = SystemParameters.VirtualScreenTop;
            Show();
            UpdateLayout();
            Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            NativeMethods.ShowWindow(_handle, 0);
            ShowActivated = true;
            _isPreloaded = true;
            _isPreloading = false;
        }

        public async void ShowAnimated()
        {
            App.ApplyWindowsAccent();
            if (_isOpen) { Activate(); return; }
            ResetDayPage();
            if (!_isPreloaded) Preload();
            _isClosing = false;
            _isAnimating = true;
            ApplyBackdrop();
            _isOpen = true;
            _restingTop = Top;
            NativeMethods.RECT rect;
            if (!NativeMethods.GetWindowRect(_handle, out rect)) { NativeMethods.ShowWindow(_handle, 5); _isAnimating = false; Activate(); return; }
            var targetTop = rect.Top;
            var startTop = rect.Bottom + (int)Math.Round(12 * VisualTreeHelper.GetDpi(this).DpiScaleY);
            MoveWindow(rect.Left, startTop);
            NativeMethods.ShowWindow(_handle, 5);
            Activate();
            await AnimateWindowAsync(rect.Left, startTop, targetTop, false);
            Top = _restingTop;
            _isAnimating = false;
        }

        public async void HideAnimated()
        {
            if (!_isOpen || _isClosing || _isAnimating) return;
            HideEventDetails();
            _isClosing = true;
            _isAnimating = true;
            NativeMethods.RECT rect;
            if (NativeMethods.GetWindowRect(_handle, out rect))
            {
                var startTop = rect.Top;
                var endTop = rect.Bottom + (int)Math.Round(12 * VisualTreeHelper.GetDpi(this).DpiScaleY);
                await AnimateWindowAsync(rect.Left, startTop, endTop, true);
                NativeMethods.ShowWindow(_handle, 0);
                MoveWindow(rect.Left, startTop);
            }
            else NativeMethods.ShowWindow(_handle, 0);
            Top = _restingTop;
            _isOpen = false;
            ResetDayPage();
            LastHiddenAtUtc = DateTime.UtcNow;
            _isAnimating = false;
            _isClosing = false;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            _handle = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(_handle);
            if (source?.CompositionTarget != null) source.CompositionTarget.BackgroundColor = Colors.Transparent;
            source?.AddHook(WndProc);
            ApplyBackdrop();
        }

        private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == 0x0006 || message == 0x031A || message == 0x031E)
                Dispatcher.BeginInvoke(new Action(ApplyBackdrop));
            return IntPtr.Zero;
        }

        private void ApplyBackdrop()
        {
            if (_handle == IntPtr.Zero) return;
            var glass = NativeMethods.EnableTransientBackdrop(_handle, true);
            PopupSurface.Background = glass ? new SolidColorBrush(Color.FromArgb(68, 32, 32, 32)) : new SolidColorBrush(Color.FromRgb(32, 32, 32));
        }

        private Task AnimateWindowAsync(int left, int from, int to, bool easeIn)
        {
            return Task.Run(() =>
            {
                var clock = Stopwatch.StartNew();
                const double duration = 167.0;
                while (clock.Elapsed.TotalMilliseconds < duration)
                {
                    var progress = Math.Max(0, Math.Min(1, clock.Elapsed.TotalMilliseconds / duration));
                    var eased = easeIn ? progress * progress : 1 - (1 - progress) * (1 - progress);
                    MoveWindow(left, (int)Math.Round(from + (to - from) * eased));
                    NativeMethods.DwmFlush();
                }
                MoveWindow(left, to);
                NativeMethods.DwmFlush();
            });
        }

        private async void Timeline_SelectedDayChanged(object sender, TimelineDayChangedEventArgs e)
        {
            HideEventDetails();
            var version = ++_pageTransitionVersion;
            await AnimateEventPageAsync(-e.Direction * 28, 0, 85);
            if (version != _pageTransitionVersion) return;
            (DataContext as MainViewModel)?.SelectDay(e.DayOffset);
            EventScroller.ScrollToTop();
            EventPageTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            EventPage.BeginAnimation(OpacityProperty, null);
            EventPageTranslate.X = e.Direction * 28;
            EventPage.Opacity = 0;
            await AnimateEventPageAsync(0, 1, 115);
        }

        private Task AnimateEventPageAsync(double targetX, double targetOpacity, int milliseconds)
        {
            var completion = new TaskCompletionSource<bool>();
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(milliseconds);
            var movement = new DoubleAnimation(targetX, duration) { EasingFunction = easing };
            var fade = new DoubleAnimation(targetOpacity, duration) { EasingFunction = easing };
            fade.Completed += (s, e) => completion.TrySetResult(true);
            EventPageTranslate.BeginAnimation(TranslateTransform.XProperty, movement, HandoffBehavior.SnapshotAndReplace);
            EventPage.BeginAnimation(OpacityProperty, fade, HandoffBehavior.SnapshotAndReplace);
            return completion.Task;
        }

        private void ResetDayPage()
        {
            _pageTransitionVersion++;
            TimelineControl.ResetToToday();
            (DataContext as MainViewModel)?.SelectDay(0);
            EventScroller.ScrollToTop();
            EventPageTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            EventPage.BeginAnimation(OpacityProperty, null);
            EventPageTranslate.X = 0;
            EventPage.Opacity = 1;
        }

        private void EventRow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is FrameworkElement row) || !(row.DataContext is CalendarEvent item)) return;
            _eventRowHovered = true;
            _pendingEventRow = row;
            _pendingEvent = item;
            _detailsCloseTimer.Stop();
            if (_eventDetails?.IsVisible == true) ShowPendingEventDetails();
            else { _detailsOpenTimer.Stop(); _detailsOpenTimer.Start(); }
        }

        private void EventRow_MouseLeave(object sender, MouseEventArgs e)
        {
            _eventRowHovered = false;
            _detailsOpenTimer.Stop();
            ScheduleEventDetailsClose();
        }

        private void ShowPendingEventDetails()
        {
            if (!_eventRowHovered || _pendingEventRow == null || _pendingEvent == null) return;
            if (_eventDetails == null)
            {
                _eventDetails = new EventDetailsWindow(this, DataContext as MainViewModel);
                _eventDetails.SubmenuEntered += (s, e) => _detailsCloseTimer.Stop();
                _eventDetails.SubmenuLeft += (s, e) => ScheduleEventDetailsClose();
                _eventDetails.ActionCompleted += (s, e) => { _eventRowHovered = false; HideEventDetails(); };
                _eventDetails.Deactivated += (s, e) => ScheduleEventDetailsClose();
            }
            _eventDetails.ShowFor(_pendingEvent, _pendingEventRow);
        }

        private void ScheduleEventDetailsClose()
        {
            _detailsCloseTimer.Stop();
            _detailsCloseTimer.Start();
        }

        private void HideEventDetails()
        {
            _detailsOpenTimer.Stop();
            _detailsCloseTimer.Stop();
            if (_eventDetails?.IsVisible == true) _eventDetails.Hide();
            _eventRowHovered = false;
            _pendingEventRow = null;
            _pendingEvent = null;
        }

        private void MoveWindow(int left, int top) => NativeMethods.SetWindowPos(_handle, IntPtr.Zero, left, top, 0, 0,
            NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate | NativeMethods.SwpNoSendChanging);

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if ((DataContext as MainViewModel)?.IsCreationMenuOpen == true) return;
                if (_eventDetails?.IsActive == true || _eventDetails?.IsMouseOver == true) return;
                if (_isOpen && !_isAnimating && !_isPreloading) HideAnimated();
            }));
        }
        private void CreateMeeting_Click(object sender, RoutedEventArgs e) => (DataContext as MainViewModel)?.ShowCreationMenu(sender as FrameworkElement);
        private void Settings_Click(object sender, RoutedEventArgs e) { HideAnimated(); _openSettings(); }
    }
}
