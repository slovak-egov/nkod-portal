namespace IAM
{
    public class UserRecord
    {
        public string Id { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string? Email { get; set; }

        public string? Role { get; set; }

        public string? Publisher { get; set; }

        public string? RefreshToken { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

        public DateTimeOffset? InvitedAt { get; set; }

        public Guid? InvitedBy { get; set; }

        public DateTimeOffset? ActivatedAt { get; set; }

        public string? InvitationToken { get; set; }
    }
}
