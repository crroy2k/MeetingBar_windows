namespace MeetingBar.Windows.Models
{
    public sealed class CalendarInfo
    {
        public string SourceId { get; set; }
        public string SourceName { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string Color { get; set; } = "#6D7CFF";
        public bool Selected { get; set; } = true;
        public override string ToString() => Name;
    }
}
