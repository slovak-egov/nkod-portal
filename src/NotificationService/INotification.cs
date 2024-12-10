namespace NotificationService
{
    public interface INotification
    {
        string Url { get; set; }

        string Title { get; set; }

        string Description { get; set; }
    }
}
