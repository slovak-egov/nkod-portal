using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface IIdentityAccessManagementClient
    {
        Task<UserInfoResult> GetUsers(UserInfoQuery query);

        Task<SaveResult> CreateUser(NewUserInput input);

        Task<SaveResult> UpdateUser(EditUserInput input);

        Task DeleteUser(string id);

        Task<TokenResult> RefreshToken(string token, string refreshToken);

        Task Logout();

        Task<TokenResult> DelegatePublisher(string publisherId);

        Task<UserInfo> GetUserInfo();

        Task<DelegationAuthorizationResult> GetLogin();

        Task<TokenResult> Consume(string content);
    }
}
