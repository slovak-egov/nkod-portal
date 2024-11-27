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

        Task<UserSaveResult> CreateUser(NewUserInput input);

        Task<UserSaveResult> UpdateUser(EditUserInput input);

        Task DeleteUser(string id);

        Task<TokenResult> RefreshToken(string token, string refreshToken);

        Task<DelegationAuthorizationResult?> Logout(string? queryString);

        Task<TokenResult> DelegatePublisher(string publisherId);

        Task<UserInfo> GetUserInfo();

        Task<DelegationAuthorizationResult> GetLogin(string? method);

        Task<TokenResult> Login(LoginInput? input);

        Task<TokenResult> Consume(string content);

        Task<string> LoginHarvester(string auth, string? publisherId);

        Task<CheckInvitationResult> CheckInvitation();

        Task<TokenResult> SignGoogle(string? code, string? state);

        Task<SaveResult> Register(UserRegistrationInput? input);

        Task<SaveResult> ActivateAccount(ActivationInput? input);

        Task<SaveResult> RequestPasswordRecovery(PasswordRecoveryInput? input);

        Task<SaveResult> ConfirmPasswordRecovery(PasswordRecoveryConfirmationInput? input);

        Task<SaveResult> ChangePassword(PasswordChangeInput? input);
    }
}
