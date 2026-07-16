using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MeetingBar.Windows.Views
{
    public sealed class AutoHideScrollViewer : ScrollViewer
    {
        public static readonly DependencyProperty ScrollBarOpacityProperty = DependencyProperty.Register(
            nameof(ScrollBarOpacity), typeof(double), typeof(AutoHideScrollViewer), new PropertyMetadata(0.0));
        private readonly DispatcherTimer _hideTimer;
        private readonly Stopwatch _scrollClock = new Stopwatch();
        private double _scrollFrom;
        private double _scrollTarget;
        private bool _scrollAnimating;

        public AutoHideScrollViewer()
        {
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1100) };
            _hideTimer.Tick += (s, e) => { _hideTimer.Stop(); ScrollBarOpacity = 0; };
            Unloaded += (s, e) => StopSmoothScroll();
        }

        public double ScrollBarOpacity
        {
            get => (double)GetValue(ScrollBarOpacityProperty);
            set => SetValue(ScrollBarOpacityProperty, value);
        }

        protected override void OnScrollChanged(ScrollChangedEventArgs e) { base.OnScrollChanged(e); ShowScrollBar(); }
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            ShowScrollBar();
            if (e.Delta == 0) { base.OnPreviewMouseWheel(e); return; }
            if (Math.Abs(e.Delta) < 120)
            {
                StopSmoothScroll();
                ScrollToVerticalOffset(ClampOffset(VerticalOffset - e.Delta));
            }
            else
            {
                var lines = SystemParameters.WheelScrollLines;
                var step = lines < 0 ? Math.Max(60, ViewportHeight * 0.82) : Math.Max(42, lines * 18.0);
                var currentTarget = _scrollAnimating ? _scrollTarget : VerticalOffset;
                _scrollFrom = VerticalOffset;
                _scrollTarget = ClampOffset(currentTarget - e.Delta / 120.0 * step);
                _scrollClock.Restart();
                StartSmoothScroll();
            }
            e.Handled = true;
        }
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e) { StopSmoothScroll(); base.OnPreviewMouseDown(e); }
        protected override void OnMouseEnter(MouseEventArgs e) { base.OnMouseEnter(e); ShowScrollBar(); }
        protected override void OnMouseLeave(MouseEventArgs e) { base.OnMouseLeave(e); _hideTimer.Stop(); ScrollBarOpacity = 0; }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.GetPosition(this).X >= ActualWidth - 18) ShowScrollBar();
        }

        private void ShowScrollBar()
        {
            ScrollBarOpacity = ScrollableHeight > 0 ? 0.88 : 0;
            _hideTimer.Stop();
            if (ScrollableHeight > 0) _hideTimer.Start();
        }

        private void SmoothScrollFrame(object sender, EventArgs e)
        {
            const double duration = 135.0;
            var progress = Math.Min(1, _scrollClock.Elapsed.TotalMilliseconds / duration);
            var eased = 1 - Math.Pow(1 - progress, 3);
            ScrollToVerticalOffset(_scrollFrom + (_scrollTarget - _scrollFrom) * eased);
            if (progress < 1) return;
            StopSmoothScroll();
            ScrollToVerticalOffset(_scrollTarget);
        }

        private void StartSmoothScroll()
        {
            if (_scrollAnimating) return;
            _scrollAnimating = true;
            CompositionTarget.Rendering += SmoothScrollFrame;
        }

        private void StopSmoothScroll()
        {
            if (!_scrollAnimating) return;
            CompositionTarget.Rendering -= SmoothScrollFrame;
            _scrollAnimating = false;
        }

        private double ClampOffset(double value) => Math.Max(0, Math.Min(ScrollableHeight, value));
    }
}
