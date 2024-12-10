namespace NotificationService
{
    public interface ISender
    {
        Task Send(string email, string body);
    }
}
