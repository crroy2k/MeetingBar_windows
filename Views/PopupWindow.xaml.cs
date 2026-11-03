using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using MeetingBar.Windows.Models;
using MeetingBar.Windows.ViewModels;

namespace MeetingBar.Windows.Views
{
    public partial class PopupWindow : Window
    {
        private readonly Action _openSettings;
        private bool _isOpen;
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
                GlassFrameThickness = new Thickness(0),
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
                if (!IsActive && !IsMouseOver && _isOpen) HideAgenda();
            };
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) HideAgenda(); };
        }

        public void ShowAgenda()
        {
            App.ApplyWindowsAccent();
            if (!_isOpen)
            {
                ResetDayPage();
                _isOpen = true;
                Show();
            }
            Activate();
        }

        public void HideAgenda()
        {
            if (!_isOpen) return;
            _isOpen = false;
            HideEventDetails();
            Hide();
            ResetDayPage();
            LastHiddenAtUtc = DateTime.UtcNow;
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

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (IsActive) return;
                if ((DataContext as MainViewModel)?.IsCreationMenuOpen == true) return;
                if (_eventDetails?.IsActive == true || _eventDetails?.IsMouseOver == true) return;
                if (_isOpen) HideAgenda();
            }));
        }
        private void CreateMeeting_Click(object sender, RoutedEventArgs e) => (DataContext as MainViewModel)?.ShowCreationMenu(sender as FrameworkElement);
        private void Settings_Click(object sender, RoutedEventArgs e) { HideAgenda(); _openSettings(); }
    }
}
