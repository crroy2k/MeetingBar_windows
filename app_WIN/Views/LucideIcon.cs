using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MeetingBar.Windows.Views
{
    public enum LucideIconKind { CalendarDays, RefreshCw, SlidersHorizontal, Video, Plus, Copy, ExternalLink, Map, CircleX, VolumeX, VolumeOff }

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
            [LucideIconKind.Plus] = new[] { "M12,5 V19", "M5,12 H19" },
            [LucideIconKind.Copy] = new[] { "M9,9 H19 A2,2 0 0 1 21,11 V19 A2,2 0 0 1 19,21 H11 A2,2 0 0 1 9,19 Z", "M5,15 H4 A2,2 0 0 1 2,13 V4 A2,2 0 0 1 4,2 H13 A2,2 0 0 1 15,4 V5" },
            [LucideIconKind.ExternalLink] = new[] { "M15,3 H21 V9", "M10,14 L21,3", "M18,13 V19 A2,2 0 0 1 16,21 H5 A2,2 0 0 1 3,19 V8 A2,2 0 0 1 5,6 H11" },
            [LucideIconKind.Map] = new[] { "M3,6 L9,3 L15,6 L21,3 V18 L15,21 L9,18 L3,21 Z", "M9,3 V18", "M15,6 V21" },
            [LucideIconKind.CircleX] = new[] { "M21,12 A9,9 0 1 1 3,12 A9,9 0 1 1 21,12", "M9,9 L15,15", "M15,9 L9,15" },
            [LucideIconKind.VolumeX] = new[] { "M11,5 L6,9 H2 V15 H6 L11,19 Z", "M22,9 L16,15", "M16,9 L22,15" },
            [LucideIconKind.VolumeOff] = new[] { "M11,5 L6,9 H2 V15 H6 L11,19 Z", "M3,3 L21,21" }
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
