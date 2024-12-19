namespace CMS
{
    public interface INotificationService
    {
        void Notify(string email, string url, string title, string description, List<string> tags);

        void Delete(string tag);
    }
}
