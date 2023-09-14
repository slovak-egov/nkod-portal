namespace IAMClient
{
    public class NewUserInput
    {
        public string? Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Role { get; set; }

        public string? IdentificationNumber { get; set; }
    }
}
