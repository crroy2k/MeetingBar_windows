using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MeetingBar.Windows.Views
{
    public enum LucideIconKind { CalendarDays, RefreshCw, SlidersHorizontal, Video, Plus }

    public sealed class LucideIcon : FrameworkElement
    {
        public static readonly DependencyProperty KindProperty = DependencyProperty.Register(nameof(Kind), typeof(LucideIconKind), typeof(LucideIcon), new FrameworkPropertyMetadata(LucideIconKind.CalendarDays, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(LucideIcon), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
        private static readonly Dictionary<LucideIconKind, string[]> Paths = new Dictionary<LucideIconKind, string[]>
        {
            [LucideIconKind.CalendarDays] = new[] { "M5,3 H19 A2,2 0 0 1 21,5 V19 A2,2 0 0 1 19,21 H5 A2,2 0 0 1 3,19 V5 A2,2 0 0 1 5,3", "M16,1 V5", "M8,1 V5", "M3,9 H21" },
            [LucideIconKind.RefreshCw] = new[] { "M21,12 A9,9 0 0 0 6,5.3 L3,8", "M3,3 V8 H8", "M3,12 A9,9 0 0 0 18,18.7 L21,16", "M16,16 H21 V21" },
            [LucideIconKind.SlidersHorizontal] = new[] { "M21,4 H14", "M10,4 H3", "M14,2 V6", "M21,12 H12", "M8,12 H3", "M8,10 V14", "M21,20 H16", "M12,20 H3", "M16,18 V22" },
            [LucideIconKind.Video] = new[] { "M14,6 H5 A2,2 0 0 0 3,8 V16 A2,2 0 0 0 5,18 H14 A2,2 0 0 0 16,16 V8 A2,2 0 0 0 14,6", "M16,10 L20,7.5 A1,1 0 0 1 21,8.4 V15.6 A1,1 0 0 1 20,16.5 L16,14" },
            [LucideIconKind.Plus] = new[] { "M12,5 V19", "M5,12 H19" }
        };

        public LucideIconKind Kind { get => (LucideIconKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
        public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var scale = Math.Min(ActualWidth, ActualHeight) / 24.0;
            if (scale <= 0) return;
            var x = (ActualWidth - 24 * scale) / 2; var y = (ActualHeight - 24 * scale) / 2;
            drawingContext.PushTransform(new TranslateTransform(x, y));
            drawingContext.PushTransform(new ScaleTransform(scale, scale));
            var pen = new Pen(Stroke, 2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            foreach (var path in Paths[Kind]) drawingContext.DrawGeometry(null, pen, Geometry.Parse(path));
            drawingContext.Pop(); drawingContext.Pop();
        }
    }
}
