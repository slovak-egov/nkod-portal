using NkodSk.Abstractions;

namespace CMS
{
    public class EmptyHttpContextValueAccessor : IHttpContextValueAccessor
    {
        public string Publisher => null;

        public string Token => null;

        public string UserId => null;

        public bool HasRole(string role) => false;
    }
}
