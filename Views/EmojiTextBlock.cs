using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MeetingBar.Windows.Views
{
    public sealed class EmojiTextBlock : TextBlock
    {
        public static readonly DependencyProperty EmojiTextProperty = DependencyProperty.Register(
            nameof(EmojiText), typeof(string), typeof(EmojiTextBlock), new PropertyMetadata("", TextChanged));
        private static readonly Lazy<EmojiAtlas> Atlas = new Lazy<EmojiAtlas>(() => new EmojiAtlas());

        public string EmojiText
        {
            get => (string)GetValue(EmojiTextProperty);
            set => SetValue(EmojiTextProperty, value);
        }

        private static void TextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => ((EmojiTextBlock)sender).Rebuild();

        private void Rebuild()
        {
            Inlines.Clear();
            var text = EmojiText ?? "";
            var plain = new StringBuilder();
            foreach (var token in EmojiTokenizer.Tokenize(text))
            {
                BitmapSource image;
                if (token.Key != null && Atlas.Value.TryGet(token.Key, out image))
                {
                    FlushText(plain);
                    var size = Math.Max(12, FontSize * 1.12);
                    var view = new Image { Source = image, Width = size, Height = size, Stretch = Stretch.Uniform, SnapsToDevicePixels = true };
                    Inlines.Add(new InlineUIContainer(view) { BaselineAlignment = BaselineAlignment.Center });
                }
                else plain.Append(token.Text);
            }
            FlushText(plain);
        }

        private void FlushText(StringBuilder text)
        {
            if (text.Length == 0) return;
            Inlines.Add(new Run(text.ToString()));
            text.Clear();
        }

        private sealed class EmojiAtlas
        {
            private const int TileSize = 32;
            private const int Columns = 64;
            private readonly Dictionary<string, int> _indexes;
            private readonly Dictionary<string, BitmapSource> _cache = new Dictionary<string, BitmapSource>();
            private readonly BitmapSource _atlas;

            public EmojiAtlas()
            {
                var map = Application.GetResourceStream(new Uri("/Resources/Emoji/twemoji-map.txt", UriKind.Relative));
                using (var reader = new StreamReader(map.Stream, Encoding.UTF8))
                    _indexes = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select((key, index) => new { Key = key.Trim().TrimStart('\uFEFF'), Index = index })
                        .ToDictionary(item => item.Key, item => item.Index, StringComparer.OrdinalIgnoreCase);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/Resources/Emoji/twemoji-atlas.png", UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                _atlas = bitmap;
            }

            public bool TryGet(string key, out BitmapSource image)
            {
                if (_cache.TryGetValue(key, out image)) return true;
                int index;
                if (!_indexes.TryGetValue(key, out index))
                {
                    key = string.Join("-", key.Split('-').Where(part => !part.Equals("fe0f", StringComparison.OrdinalIgnoreCase)));
                    if (!_indexes.TryGetValue(key, out index)) return false;
                }
                image = new CroppedBitmap(_atlas, new Int32Rect(index % Columns * TileSize, index / Columns * TileSize, TileSize, TileSize));
                image.Freeze();
                _cache[key] = image;
                return true;
            }
        }

    }
}
