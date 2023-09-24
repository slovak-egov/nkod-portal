namespace IAM
{
    public class UserRecord
    {
        public string Id { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Role { get; set; }

        public string? Publisher { get; set; }

        public string? RefreshToken { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    }
}
