namespace MeetingBar.Windows.Models
{
    public sealed class Bookmark
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public override string ToString() => Name;
    }
}
