using Microsoft.AspNetCore.Http;
using NkodSk.Abstractions;

namespace NkodSk.Abstractions
{
    public class DefaultFileAccessPolicy : IFileStorageAccessPolicy
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public DefaultFileAccessPolicy(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public bool HasReadAccessToFile(FileMetadata metadata)
        {
            if (metadata.IsPublic)
            {
                return true;
            }

            return HasModifyAccessToFile(metadata);
        }

        public bool HasModifyAccessToFile(FileMetadata metadata)
        {
            HttpContext? httpContext = httpContextAccessor.HttpContext;

            if (httpContext?.User == null)
            {
                return false;
            }

            if (httpContext.User.IsInRole("Superadmin"))
            {
                return true;
            }

            if (metadata.Publisher != null)
            {
                return httpContext.User.HasClaim("Publisher", metadata.Publisher);
            }

            return false;
        }
    }
}
