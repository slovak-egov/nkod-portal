namespace NotificationService
{
    public class NotificationInput : INotification
    {
        public string Email { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<string>? Tags { get; set; }
    }
}
