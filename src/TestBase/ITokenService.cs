using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public interface ITokenService
    {
        Task<TokenResult> RefreshToken(string token, string refreshToken);

        Task<TokenResult> DelegateToken(IHttpContextValueAccessor httpContextValueAccessor, string publisherId, string userId);
    }
}
