namespace WebApi
{
    public class UserInfo : NkodSk.Abstractions.UserInfo
    {
        public PublisherView? PublisherView { get; set; }

        public string? PublisherHomePage { get; set; }

        public string? PublisherEmail { get; set; }

        public string? PublisherPhone { get; set; }

        public bool PublisherActive { get; set; }
    }
}
