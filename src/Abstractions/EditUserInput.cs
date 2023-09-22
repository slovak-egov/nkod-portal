namespace NkodSk.Abstractions
{
    public class EditUserInput
    {
        public string? Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string? Role { get; set; }
    }
}
