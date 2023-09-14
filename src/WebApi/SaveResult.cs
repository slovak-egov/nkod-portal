namespace WebApi
{
    public class SaveResult
    {
        public string? Id { get; set; }

        public bool Success { get; set; }

        public Dictionary<string, string>? Errors { get; set; }
    }
}
