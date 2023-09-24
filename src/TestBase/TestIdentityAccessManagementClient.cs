using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VDS.Common.Tries;

namespace TestBase
{
    public class TestIdentityAccessManagementClient : IIdentityAccessManagementClient
    {
        private readonly Dictionary<string, List<PersistentUserInfo>> users = new Dictionary<string, List<PersistentUserInfo>>();

        private readonly IHttpContextValueAccessor httpContextValueAccessor;

        private readonly ITokenService tokenService;

        public TestIdentityAccessManagementClient(IHttpContextValueAccessor httpContextValueAccessor, ITokenService tokenService)
        {
            this.httpContextValueAccessor = httpContextValueAccessor;
            this.tokenService = tokenService;
        }

        private string PublisherId
        {
            get
            {
                if (httpContextValueAccessor.Publisher is not null)
                {
                    if (httpContextValueAccessor.HasRole("PublisherAdmin") || httpContextValueAccessor.HasRole("Superadmin"))
                    {
                        return httpContextValueAccessor.Publisher;
                    }
                    else
                    {
                        throw new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden);
                    }
                }
                throw new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Unauthorized);
            }
        }

        public Task<UserInfoResult> GetUsers(UserInfoQuery query)
        {
            string? publisherId = PublisherId;
            if (!string.IsNullOrEmpty(publisherId))
            {
                return GetUsers(query, publisherId);
            }
            return Task.FromResult(new UserInfoResult(new List<PersistentUserInfo>(), 0));
        }

        public Task<UserInfoResult> GetUsers(UserInfoQuery query, string publisherId)
        {
            int take = query.PageSize ?? int.MaxValue;
            int skip = query.Page.HasValue && query.PageSize.HasValue ? (query.Page.Value - 1) * query.PageSize.Value : 0;

            if (!users.TryGetValue(publisherId, out List<PersistentUserInfo>? list))
            {
                list = new List<PersistentUserInfo>();
            }

            UserInfoResult result = new UserInfoResult(list.Skip(skip).Take(take).ToList(), list.Count);

            return Task.FromResult(result);
        }

        public Task<SaveResult> CreateUser(NewUserInput input)
        {
            return CreateUser(input, PublisherId);
        }

        public Task<SaveResult> CreateUser(NewUserInput input, string publisherId)
        {
            SaveResult result = new SaveResult();
            if (!string.IsNullOrEmpty(publisherId))
            {
                if (!users.TryGetValue(publisherId, out List<PersistentUserInfo>? list))
                {
                    list = new List<PersistentUserInfo>();
                    users.Add(publisherId, list);
                }

                PersistentUserInfo userInfo = new PersistentUserInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = input.Email,
                    Role = input.Role
                };

                list.Add(userInfo);

                result.Id = userInfo.Id;
                result.Success = true;
            }
            return Task.FromResult(result);
        }

        public Task<SaveResult> UpdateUser(EditUserInput input)
        {
            return UpdateUser(input, PublisherId);
        }

        public Task<SaveResult> UpdateUser(EditUserInput input, string publisherId)
        {
            SaveResult result = new SaveResult();
            if (!string.IsNullOrEmpty(publisherId))
            {
                if (!users.TryGetValue(publisherId, out List<PersistentUserInfo>? list))
                {
                    list = new List<PersistentUserInfo>();
                    users.Add(publisherId, list);
                }

                list.RemoveAll(u => u.Id == input.Id);

                PersistentUserInfo userInfo = new PersistentUserInfo
                {
                    Id = input.Id!,
                    Email = input.Email,
                    Role = input.Role
                };

                list.Add(userInfo);

                result.Id = userInfo.Id;
                result.Success = true;
            }
            return Task.FromResult(result);
        }

        public Task DeleteUser(string id)
        {
            return DeleteUser(id, PublisherId);
        }

        public Task DeleteUser(string id, string publisherId)
        {
            if (!string.IsNullOrEmpty(publisherId))
            {
                if (users.TryGetValue(publisherId, out List<PersistentUserInfo>? list))
                {
                    list.RemoveAll(u => u.Id == id);
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<TokenResult> RefreshToken(string token, string refreshToken)
        {
            return tokenService.RefreshToken(token, refreshToken);
        }

        public Task Logout()
        {
            return Task.CompletedTask;
        }

        public Task<TokenResult> DelegatePublisher(string publisherId)
        {
            return tokenService.DelegateToken(httpContextValueAccessor, publisherId);
        }

        public Task<UserInfo> GetUserInfo()
        {
            if (!string.IsNullOrEmpty(httpContextValueAccessor.Publisher) && users.TryGetValue(httpContextValueAccessor.Publisher, out List<PersistentUserInfo>? list))
            {
                PersistentUserInfo? persistentUserInfo = list.FirstOrDefault(u => u.Id == httpContextValueAccessor.UserId);
                if (persistentUserInfo is not null)
                {
                    return Task.FromResult(new UserInfo
                    {
                        FirstName = persistentUserInfo.FirstName,
                        LastName = persistentUserInfo.LastName,
                        Role = persistentUserInfo.Role,
                        Email = persistentUserInfo.Email,
                        Id = persistentUserInfo.Id
                    });
                }
            }
            throw new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden);
        }

        public Task<DelegationAuthorizationResult> GetLogin()
        {
            throw new NotImplementedException();
        }

        public Task<TokenResult> Consume(string content)
        {
            throw new NotImplementedException();
        }
    }
}
