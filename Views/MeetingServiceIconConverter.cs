using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MeetingBar.Windows.Views
{
    public sealed class MeetingServiceIconConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> Icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Google Meet"] = "google_meet_icon", ["Zoom"] = "zoom_icon", ["ZoomGov"] = "zoom_icon",
            ["Microsoft Teams"] = "ms_teams_icon", ["Cisco Webex"] = "webex_icon", ["Jitsi"] = "jitsi_icon", ["Amazon Chime"] = "amazon_chime_icon",
            ["RingCentral"] = "ringcentral_icon", ["GoToMeeting"] = "gotomeeting_icon", ["GoToWebinar"] = "gotowebinar_icon", ["BlueJeans"] = "bluejeans_icon",
            ["8x8"] = "8x8_icon", ["Demio"] = "demio_icon", ["Join.me"] = "joinme_icon", ["Whereby"] = "whereby_icon",
            ["UberConference"] = "uberconference_icon", ["TeamViewer Meeting"] = "teamviewer_meeting_icon", ["VSee"] = "vsee_icon", ["StarLeaf"] = "starleaf_icon",
            ["Tencent VooV"] = "voov_icon", ["Workplace"] = "facebook_workplace_icon", ["Lifesize"] = "lifesize_icon", ["Skype"] = "skype_icon",
            ["FaceTime"] = "facetime_icon", ["Discord"] = "discord_icon", ["Slack Huddle"] = "slack_icon", ["YouTube"] = "youtube_icon",
            ["Vonage Meetings"] = "vonage_icon", ["CoScreen"] = "coscreen_icon", ["Vowel"] = "vowel_icon", ["Zhumu"] = "zhumu_icon",
            ["Lark"] = "lark_icon", ["Feishu"] = "feishu_icon", ["Vimeo"] = "vimeo_icon", ["oVice"] = "ovice_icon",
            ["Preply"] = "preply_icon", ["UserZoom"] = "userzoom_icon", ["Venue"] = "venue_icon", ["Teemyco"] = "teemyco_icon",
            ["Demodesk"] = "demodesk_icon", ["Zoho Cliq"] = "zoho_cliq_icon", ["Gather"] = "gather_icon", ["Pop"] = "pop_icon",
            ["Chorus"] = "chorus_icon", ["Gong"] = "gong_icon", ["Livestorm"] = "livestorm_icon", ["Tuple"] = "tuple_icon",
            ["Pumble"] = "pumble_icon", ["Suit Conference"] = "suit_conference_icon", ["Doxy.me"] = "doxy_me_icon", ["Cal Video"] = "calcom_icon",
            ["zm.page"] = "zm_page_icon", ["LiveKit Meet"] = "livekit_icon", ["Meetecho"] = "meetecho_icon", ["StreamYard"] = "streamyard_icon",
            ["Riverside"] = "riverside_icon"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string icon;
            if (!Icons.TryGetValue(value as string ?? "", out icon)) return null;
            return new BitmapImage(new Uri("pack://application:,,,/Resources/MeetingServices/" + icon + ".png", UriKind.Absolute));
        }

        public static bool IsKnown(string service) => !string.IsNullOrWhiteSpace(service) && Icons.ContainsKey(service);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class UnknownMeetingServiceVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            !string.IsNullOrWhiteSpace(value as string) && !MeetingServiceIconConverter.IsKnown(value as string) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
