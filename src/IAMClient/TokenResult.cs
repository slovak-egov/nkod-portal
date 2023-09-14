namespace IAMClient
{
    public class TokenResult
    {
        public string Token { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public string? RedirectUrl { get; set; }
    }
}
