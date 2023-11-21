using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using NkodSk.Abstractions;
using System.Security.Claims;

namespace WebApi
{
    public class HttpContextValueAccessor : IHttpContextValueAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpContextValueAccessor(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string? Publisher => httpContextAccessor.HttpContext?.User.FindFirstValue("Publisher");

        public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? Token
        {
            get
            {
                string? token = httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
                if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer "))
                {
                    return token.Substring(7);
                }

                string? cookieToken = httpContextAccessor.HttpContext?.Request.Cookies["accessToken"];
                if (!string.IsNullOrEmpty(cookieToken))
                {
                    TokenResult? tokenResult = JsonConvert.DeserializeObject<TokenResult>(cookieToken);
                    if (tokenResult is not null)
                    {
                        return tokenResult.Token;
                    }
                }

                return null;
            }
        }

        public bool HasRole(string role)
        {
            return httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
        }
    }
}
