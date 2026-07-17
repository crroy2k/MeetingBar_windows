namespace MeetingBar.Windows.Models
{
    public sealed class ProviderStatus
    {
        public string Provider { get; set; }
        public bool IsHealthy { get; set; }
        public string Message { get; set; }
    }
}
