namespace WebApi
{
    public class RegistrationResult
    {
        public bool Success { get; set; }

        public List<string>? Errors { get; } = new List<string>();
    }
}
