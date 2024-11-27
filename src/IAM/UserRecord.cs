using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

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

        public string? FormattedName { get; set; }

        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

        public DateTimeOffset? InvitedAt { get; set; }

        public Guid? InvitedBy { get; set; }

        public DateTimeOffset? ActivatedAt { get; set; }

        public DateTimeOffset? Registered { get; set; }

        public string? InvitationToken { get; set; }

        public string? Password { get; set; }

        public string? ActivationToken { get; set; }

        public DateTimeOffset? ActivationTokenExpiryTime { get; set; }

        public string? RecoveryToken { get; set; }

        public DateTimeOffset? RecoveryTokenExpiryTime { get; set; }

        public int RecoveryTokenSentTimes { get; set; }

        public string? RegistrationSourceIpAddress { get; set; }

        public string? ExternalId { get; set; }

        public void SetPassword(string password)
        {
            byte[] passwordSalt = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(passwordSalt);
            }

            byte[] key = KeyDerivation.Pbkdf2(password, passwordSalt, KeyDerivationPrf.HMACSHA512, 10000, 32);

            byte[] passwordSalted = new byte[passwordSalt.Length + key.Length];
            Buffer.BlockCopy(passwordSalt, 0, passwordSalted, 0, passwordSalt.Length);
            Buffer.BlockCopy(key, 0, passwordSalted, passwordSalt.Length, key.Length);

            Password = Convert.ToBase64String(passwordSalted);
        }

        public bool VerifyPassword(string password)
        {
            try
            {
                if (!string.IsNullOrEmpty(Password))
                {
                    byte[] passwordBytes = Convert.FromBase64String(Password);
                    if (passwordBytes.Length == 48)
                    {
                        byte[] passwordSalt = new byte[16];
                        Buffer.BlockCopy(passwordBytes, 0, passwordSalt, 0, 16);

                        byte[] computedKey = KeyDerivation.Pbkdf2(password, passwordSalt, KeyDerivationPrf.HMACSHA512, 10000, 32);
                        return MemoryExtensions.SequenceEqual(passwordBytes.AsSpan(16), computedKey);
                    }
                }
            }
            catch
            {
                //ignore
            }
            return false;
        }
    }
}
