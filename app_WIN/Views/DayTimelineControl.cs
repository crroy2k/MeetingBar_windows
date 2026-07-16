using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Views
{
    public sealed class DayTimelineControl : FrameworkElement
    {
        public static readonly DependencyProperty EventsProperty = DependencyProperty.Register(
            nameof(Events), typeof(IEnumerable), typeof(DayTimelineControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, EventsChanged));

        private readonly DispatcherTimer _clock;
        private INotifyCollectionChanged _observedEvents;

        public DayTimelineControl()
        {
            SnapsToDevicePixels = true;
            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _clock.Tick += (s, e) => InvalidateVisual();
            Loaded += (s, e) => _clock.Start();
            Unloaded += (s, e) => _clock.Stop();
        }

        public IEnumerable Events
        {
            get => (IEnumerable)GetValue(EventsProperty);
            set => SetValue(EventsProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var width = ActualWidth;
            if (width <= 0) return;

            var now = DateTime.Now;
            var rangeStart = now.AddHours(-3);
            var rangeEnd = now.AddHours(6);
            var rangeTicks = (double)(rangeEnd - rangeStart).Ticks;
            const double gridTop = 19;
            var gridBottom = Math.Max(gridTop + 32, ActualHeight - 3);
            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var muted = BrushFrom("#FFB8B8B8");
            var grid = new Pen(BrushFrom("#4D707070"), 1);

            var firstHour = new DateTime(rangeStart.Year, rangeStart.Month, rangeStart.Day, rangeStart.Hour, 0, 0).AddHours(1);
            var hourIndex = 0;
            for (var hour = firstHour; hour < rangeEnd; hour = hour.AddHours(1), hourIndex++)
            {
                var x = X(hour, rangeStart, rangeTicks, width);
                drawingContext.DrawLine(grid, new Point(x, gridTop), new Point(x, gridBottom));
                if (hourIndex % 2 == 0)
                {
                    var label = Text(hour.ToString("t", CultureInfo.CurrentCulture), 9, FontWeights.Normal, muted, pixelsPerDip);
                    drawingContext.DrawText(label, new Point(Math.Max(0, Math.Min(width - label.Width, x - label.Width / 2)), 0));
                }
            }

            var visible = (Events ?? Enumerable.Empty<object>()).Cast<object>().OfType<CalendarEvent>()
                .Where(item => !item.IsAllDay && !item.IsCancelled && item.End > rangeStart && item.Start < rangeEnd)
                .OrderBy(item => item.Start).ThenBy(item => item.End).ToList();
            var laneEnds = new List<DateTime>();
            foreach (var item in visible)
            {
                var lane = 0;
                while (lane < laneEnds.Count && laneEnds[lane] > item.Start) lane++;
                if (lane == laneEnds.Count) laneEnds.Add(item.End); else laneEnds[lane] = item.End;
                if (lane >= 3) lane = 2;

                var left = X(item.Start < rangeStart ? rangeStart : item.Start, rangeStart, rangeTicks, width);
                var right = X(item.End > rangeEnd ? rangeEnd : item.End, rangeStart, rangeTicks, width);
                var segmentWidth = Math.Max(3, right - left);
                var top = gridTop + 5 + lane * 13;
                drawingContext.DrawRoundedRectangle(EventBrush(item.CalendarColor), item.IsCurrent ? new Pen(Brushes.White, 1) : null,
                    new Rect(left, top, segmentWidth, 10), 3, 3);

                if (segmentWidth >= 36)
                {
                    var title = Text(item.Title ?? "", 8, FontWeights.SemiBold, Brushes.White, pixelsPerDip);
                    drawingContext.PushClip(new RectangleGeometry(new Rect(left + 3, top, Math.Max(0, segmentWidth - 6), 10)));
                    drawingContext.DrawText(title, new Point(left + 3, top - 1));
                    drawingContext.Pop();
                }
            }

            var nowX = X(now, rangeStart, rangeTicks, width);
            var accent = BrushFrom("#FFD0D0D0");
            drawingContext.DrawLine(new Pen(accent, 1.5), new Point(nowX, gridTop - 2), new Point(nowX, gridBottom));
            drawingContext.DrawEllipse(accent, null, new Point(nowX, gridTop - 2), 3, 3);
        }

        private static void EventsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var control = (DayTimelineControl)sender;
            if (control._observedEvents != null) control._observedEvents.CollectionChanged -= control.CollectionChanged;
            control._observedEvents = args.NewValue as INotifyCollectionChanged;
            if (control._observedEvents != null) control._observedEvents.CollectionChanged += control.CollectionChanged;
            control.InvalidateVisual();
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();
        private static double X(DateTime value, DateTime start, double rangeTicks, double width) => Math.Max(0, Math.Min(width, (value - start).Ticks / rangeTicks * width));
        private static SolidColorBrush BrushFrom(string value) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        private static Brush EventBrush(string value)
        {
            try { return BrushFrom(string.IsNullOrWhiteSpace(value) ? "#FF6D7CFF" : value); }
            catch { return BrushFrom("#FF6D7CFF"); }
        }
        private static FormattedText Text(string value, double size, FontWeight weight, Brush brush, double pixelsPerDip) =>
            new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal), size, brush, pixelsPerDip);
    }
}
