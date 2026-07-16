using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Services
{
    public sealed class MeetingLinkResult
    {
        public string Url { get; set; }
        public string Service { get; set; }
    }

    public static class MeetingLinkDetector
    {
        private static readonly Tuple<string, Regex>[] Providers = BuildProviders();
        private static readonly Regex SafeLink = new Regex(@"https://[^\s]+\.safelinks\.protection\.outlook\.com/[^\s?]*[?&]url=([^&\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AnyUrl = new Regex(@"(?:https?|zoommtg|discord)://[^\s<>\""']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static Tuple<string, Regex>[] BuildProviders()
        {
            var specs = new Dictionary<string, string>
            {
                ["Google Meet"] = @"https?://(?:stream\.)?meet\.google\.com/(?:_meet/|stream/)?[a-z0-9-]+[^\s<>\""']*",
                ["Proton Meet"] = @"https?://meet\.proton\.me/join/[A-Za-z0-9-]+[^\s<>\""']*",
                ["Zoom"] = @"(?:https://(?:[\w.-]+\.)?zoom(?:-x)?\.(?:us|com|com\.cn|de)/[^\s<>\""']+|zoommtg://[^\s<>\""']+)",
                ["ZoomGov"] = @"https?://[\w.]*zoomgov\.com/j/[^\s<>\""']+",
                ["Microsoft Teams"] = @"https?://(?:gov\.)?teams\.microsoft\.(?:com|us)/(?:l/meetup-join|meet)/[^\s<>\""']+",
                ["Cisco Webex"] = @"https?://[\w.-]*webex\.com/(?:[^\s<>\""']*/)?(?:j\.php|meet|join)/?[^\s<>\""']*",
                ["Jitsi"] = @"https?://meet\.jit\.si/[^\s<>\""']+",
                ["Amazon Chime"] = @"https?://[\w.-]*chime\.aws/[^\s<>\""']+",
                ["RingCentral"] = @"https?://[\w.-]*ringcentral\.com/[^\s<>\""']+",
                ["GoToMeeting"] = @"https?://[\w.-]*gotomeeting\.com/[^\s<>\""']+",
                ["GoToWebinar"] = @"https?://[\w.-]*gotowebinar\.com/[^\s<>\""']+",
                ["BlueJeans"] = @"https?://[\w.-]*bluejeans\.com/[^\s<>\""']+",
                ["8x8"] = @"https?://8x8\.vc/[^\s<>\""']+",
                ["Demio"] = @"https?://event\.demio\.com/[^\s<>\""']+",
                ["Join.me"] = @"https?://join\.me/[^\s<>\""']+",
                ["Whereby"] = @"https?://whereby\.com/[^\s<>\""']+",
                ["UberConference"] = @"https?://uberconference\.com/[^\s<>\""']+",
                ["TeamViewer Meeting"] = @"https?://go\.(?:blizz|teamviewer)\.com/[^\s<>\""']+",
                ["VSee"] = @"https?://vsee\.com/[^\s<>\""']+",
                ["StarLeaf"] = @"https?://meet\.starleaf\.com/[^\s<>\""']+",
                ["Tencent VooV"] = @"https?://voovmeeting\.com/[^\s<>\""']+",
                ["Workplace"] = @"https?://[\w.-]*workplace\.com/groupcall/[^\s<>\""']+",
                ["Lifesize"] = @"https?://call\.lifesizecloud\.com/[^\s<>\""']+",
                ["Skype"] = @"https?://(?:join\.skype|meet\.lync)\.com/[^\s<>\""']+",
                ["FaceTime"] = @"https?://facetime\.apple\.com/join[^\s<>\""']+",
                ["Discord"] = @"(?:https?|discord)://(?:www\.|canary\.)?discord(?:app)?\.[^\s<>\""']+",
                ["Slack Huddle"] = @"https?://app\.slack\.com/huddle/[^\s<>\""']+",
                ["YouTube"] = @"https?://(?:(?:www|m)\.)?(?:youtube\.com|youtu\.be)/[^\s<>\""']+",
                ["Vonage Meetings"] = @"https?://meetings\.vonage\.com/\d+",
                ["Around"] = @"https?://(?:meet\.)?around\.co/[^\s<>\""']+",
                ["Jam"] = @"https?://jam\.systems/[^\s<>\""']+",
                ["Blackboard Collaborate"] = @"https?://us\.bbcollab\.com/[^\s<>\""']+",
                ["CoScreen"] = @"https?://join\.coscreen\.co/[^\s<>\""']+",
                ["Vowel"] = @"https?://[\w.]*vowel\.com/#/g/[^\s<>\""']+",
                ["Zhumu"] = @"https?://welink\.zhumu\.com/[^\s<>\""']+",
                ["Lark"] = @"https?://vc\.larksuite\.com/j/\d+",
                ["Feishu"] = @"https?://vc\.feishu\.cn/j/\d+",
                ["Vimeo"] = @"https?://(?:vimeo\.com/(?:showcase|event)/\d+|venues\.vimeo\.com/[^\s<>\""']+)",
                ["oVice"] = @"https?://[\w.-]*ovice\.(?:in|com)/[^\s<>\""']+",
                ["Luma"] = @"https?://lu\.ma/join/[^\s<>\""']+",
                ["Preply"] = @"https?://preply\.com/[^\s<>\""']+",
                ["UserZoom"] = @"https?://go\.userzoom\.com/participate/[a-z0-9-]+",
                ["Venue"] = @"https?://app\.venue\.live/app/[^\s<>\""']+",
                ["Teemyco"] = @"https?://app\.teemyco\.com/room/[^\s<>\""']+",
                ["Demodesk"] = @"https?://demodesk\.com/[^\s<>\""']+",
                ["Zoho Cliq"] = @"https?://cliq\.zoho\.eu/meetings/[^\s<>\""']+",
                ["Gather"] = @"https?://app\.gather\.town/app/[^\s<>\""']+",
                ["Pop"] = @"https?://pop\.com/j/[0-9-]+",
                ["Chorus"] = @"https?://go\.chorus\.ai/[^\s<>\""']+",
                ["Gong"] = @"https?://[\w.-]*join\.gong\.io/[^\s<>\""']+",
                ["Livestorm"] = @"https?://app\.livestorm\.com/p/[^\s<>\""']+",
                ["Tuple"] = @"https?://tuple\.app/c/[^\s<>\""']+",
                ["Pumble"] = @"https?://meet\.pumble\.com/[a-z-]+",
                ["Suit Conference"] = @"https?://[\w.]*conference\.istesuit\.com/[^\s<>\""']+",
                ["Doxy.me"] = @"https?://[\w.]*doxy\.me/[^\s<>\""']+",
                ["Cal Video"] = @"https?://app\.cal\.com/video/[^\s<>\""']+",
                ["zm.page"] = @"https?://[\w.]+\.zm\.page[^\s<>\""']*",
                ["LiveKit Meet"] = @"https?://meet[\w.]*\.livekit\.io/rooms/[^\s<>\""']+",
                ["Meetecho"] = @"https?://meetings\.conf\.meetecho\.com/[^\s<>\""']+",
                ["StreamYard"] = @"https?://(?:www\.)?streamyard\.com/(?:guest/)?[^\s<>\""']+",
                ["Riverside"] = @"https?://riverside\.(?:com|fm)/studio/[^\s<>\""']+",
                ["Reclaim.ai"] = @"https?://reclaim\.ai/z/[^\s<>\""']+"
            };
            return specs.Select(x => Tuple.Create(x.Key, new Regex(x.Value, RegexOptions.IgnoreCase | RegexOptions.Compiled))).ToArray();
        }

        public static MeetingLinkResult Detect(CalendarEvent item, IEnumerable<string> customRegexes = null)
        {
            var sources = new[] { item.ConferenceUrl, item.EventUrl, item.Location, item.Notes };
            foreach (var source in sources)
            {
                var result = DetectText(UnwrapSafeLinks(source), customRegexes);
                if (result != null) return result;
            }
            return null;
        }

        public static MeetingLinkResult DetectText(string text, IEnumerable<string> customRegexes = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = WebUtility.HtmlDecode(Regex.Replace(text, "<[^>]+>", " "));
            foreach (var provider in Providers)
            {
                var match = provider.Item2.Match(text);
                if (match.Success) return Result(Clean(match.Value), provider.Item1);
            }
            foreach (var pattern in customRegexes ?? Enumerable.Empty<string>())
            {
                try { var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase); if (match.Success) return Result(Clean(match.Value), "Other"); }
                catch (ArgumentException) { }
            }
            var generic = AnyUrl.Match(text);
            return generic.Success ? Result(Clean(generic.Value), "Web meeting") : null;
        }

        private static MeetingLinkResult Result(string url, string service) => new MeetingLinkResult { Url = url, Service = service };
        private static string Clean(string value) => value.TrimEnd('.', ',', ';', ')', ']', '}', '&').Replace("&amp;", "&");

        public static string UnwrapSafeLinks(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            for (var i = 0; i < 32; i++)
            {
                var match = SafeLink.Match(text);
                if (!match.Success) break;
                var decoded = WebUtility.UrlDecode(match.Groups[1].Value);
                if (string.IsNullOrWhiteSpace(decoded) || decoded == match.Value) break;
                text = text.Substring(0, match.Index) + decoded + text.Substring(match.Index + match.Length);
            }
            return text;
        }
    }
}
