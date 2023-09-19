using Microsoft.Extensions.Primitives;
using NkodSk.Abstractions;
using System.Security.Claims;

namespace CodelistProvider
{
    public class HttpContextValueAccessor : IHttpContextValueAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpContextValueAccessor(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string? Publisher => httpContextAccessor.HttpContext?.User.FindFirstValue("Publisher");

        public string? Token => httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();

        public bool HasRole(string role)
        {
            return httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
        }
    }
}
