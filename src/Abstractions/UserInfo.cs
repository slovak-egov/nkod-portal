namespace NkodSk.Abstractions
{
    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string? Publisher { get; set; }

        public string? Role { get; set; }

        public string? AuthorizationMethod { get; set; }

        public string? FormattedName { get; set; }
    }
}
