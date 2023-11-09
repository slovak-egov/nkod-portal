namespace NkodSk.Abstractions
{
    public class TokenResult
    {
        public string Token { get; set; } = string.Empty;

        public DateTimeOffset Expires { get; set; }

        public DateTimeOffset RefreshTokenAfter { get; set; }

        public string? RefreshToken { get; set; }

        public string? RedirectUrl { get; set; }
    }
}
