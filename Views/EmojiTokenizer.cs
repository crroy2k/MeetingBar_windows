using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MeetingBar.Windows.Views
{
    internal static class EmojiTokenizer
    {
        public static IEnumerable<EmojiToken> Tokenize(string text)
        {
            for (var index = 0; index < text.Length;)
            {
                var start = index;
                var codepoint = ReadCodepoint(text, ref index);
                if (!IsEmojiStart(codepoint)) { yield return new EmojiToken(text.Substring(start, index - start), null); continue; }

                var codepoints = new List<int> { codepoint };
                if (IsRegional(codepoint) && index < text.Length)
                {
                    var probe = index; var next = ReadCodepoint(text, ref probe);
                    if (IsRegional(next)) { codepoints.Add(next); index = probe; }
                }
                while (index < text.Length)
                {
                    var probe = index; var next = ReadCodepoint(text, ref probe);
                    if (IsEmojiContinuation(next)) { codepoints.Add(next); index = probe; continue; }
                    if (next == 0x200D && probe < text.Length)
                    {
                        codepoints.Add(next); index = probe;
                        codepoints.Add(ReadCodepoint(text, ref index));
                        continue;
                    }
                    break;
                }
                var key = string.Join("-", codepoints.Select(value => value.ToString("x", CultureInfo.InvariantCulture)));
                yield return new EmojiToken(text.Substring(start, index - start), key);
            }
        }

        private static int ReadCodepoint(string value, ref int index)
        {
            var high = value[index++];
            if (char.IsHighSurrogate(high) && index < value.Length && char.IsLowSurrogate(value[index])) return char.ConvertToUtf32(high, value[index++]);
            return high;
        }
        private static bool IsEmojiStart(int value) => value >= 0x1F000 && value <= 0x1FAFF || value >= 0x2600 && value <= 0x27FF || value >= 0x2300 && value <= 0x23FF;
        private static bool IsEmojiContinuation(int value) => value == 0xFE0F || value == 0x20E3 || value >= 0x1F3FB && value <= 0x1F3FF || value >= 0xE0020 && value <= 0xE007F;
        private static bool IsRegional(int value) => value >= 0x1F1E6 && value <= 0x1F1FF;
    }

    internal sealed class EmojiToken
    {
        public EmojiToken(string text, string key) { Text = text; Key = key; }
        public string Text { get; }
        public string Key { get; }
    }
}
