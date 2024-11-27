namespace IAM
{
    public class EmailOptions
    {
        public string? Host { get; set; }

        public int? Port { get; set; }

        public bool UseSsl { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? FromAddress { get; set; }

        public string? FromName {  get; set; }
    }
}
