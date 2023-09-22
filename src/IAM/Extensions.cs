using System.Security.Claims;

namespace IAM
{
    public static class Extensions
    {
        public static string? GetAuthorizedPublisherId(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.IsInRole("PublisherAdmin") || claimsPrincipal.IsInRole("Superadmin"))
            {
                return claimsPrincipal.FindFirst("Publisher")?.Value;
            }
            return null;
        }
    }
}
