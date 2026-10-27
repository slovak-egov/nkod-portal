using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IAM
{
    public sealed class AuditLog
    {
        private readonly ILogger<AuditLog> logger;

        private readonly byte[]? hashKey;

        public AuditLog(ILogger<AuditLog> logger, IConfiguration configuration)
        {
            this.logger = logger;

            string? key = configuration["AuditLog:HashKey"];
            if (!string.IsNullOrEmpty(key))
            {
                hashKey = Encoding.UTF8.GetBytes(key);
            }
        }

        public void Success(HttpContext httpContext, string action, string? targetId = null, string? details = null, string? actorId = null)
        {
            logger.LogInformation("AUDIT {AuditAction} {AuditOutcome} actor={ActorId} target={TargetId} ip={ClientIp} details={AuditDetails}",
                action, "Success", actorId ?? GetActorId(httpContext.User), targetId, GetClientIp(httpContext), details);
        }

        public void Failure(HttpContext httpContext, string action, string reason, string? targetId = null, string? emailHash = null, string? actorId = null)
        {
            logger.LogWarning("AUDIT {AuditAction} {AuditOutcome} actor={ActorId} target={TargetId} ip={ClientIp} reason={AuditReason} emailHash={EmailHash}",
                action, "Failure", actorId ?? GetActorId(httpContext.User), targetId, GetClientIp(httpContext), reason, emailHash);
        }

        /// <summary>
        /// Pseudonymizuje e-mail (HMAC-SHA256), aby bolo možné korelovať opakované pokusy bez uloženia samotnej adresy.
        /// Bez nakonfigurovaného kľúča AuditLog:HashKey sa e-mail nezaznamená vôbec.
        /// </summary>
        public string? HashEmail(string? email)
        {
            if (hashKey is null || string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            byte[] hash = HMACSHA256.HashData(hashKey, Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
            return Convert.ToHexString(hash, 0, 16);
        }

        private static string? GetActorId(ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private static string? GetClientIp(HttpContext httpContext)
        {
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
