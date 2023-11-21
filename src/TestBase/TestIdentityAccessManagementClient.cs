using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VDS.Common.Tries;

namespace TestBase
{
    public class TestIdentityAccessManagementClient : IIdentityAccessManagementClient
    {
        private readonly Dictionary<string, List<Entry>> users = new Dictionary<string, List<Entry>>();

        private readonly IHttpContextValueAccessor httpContextValueAccessor;

        private readonly ITokenService tokenService;

        public TestIdentityAccessManagementClient(IHttpContextValueAccessor httpContextValueAccessor, ITokenService tokenService)
        {
            this.httpContextValueAccessor = httpContextValueAccessor;
            this.tokenService = tokenService;
        }

        public TimeSpan RefreshTokenAfter { get; set; } = TimeSpan.FromMinutes(30);

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

            if (!users.TryGetValue(publisherId, out List<Entry>? list))
            {
                list = new List<Entry>();
            }

            IQueryable<Entry> queryable = list.AsQueryable();

            if (query.Id is not null)
            {
                queryable = queryable.Where(e => e.UserInfo.Id == query.Id);
            }

            UserInfoResult result = new UserInfoResult(queryable.OrderByDescending(e => e.Updated).Skip(skip).Take(take).Select(e => e.UserInfo).ToList(), list.Count);

            return Task.FromResult(result);
        }

        public PersistentUserInfo? GetUser(string publisherId, string id)
        {
            if (users.TryGetValue(publisherId, out List<Entry>? list))
            {
                return list.Where(u => u.UserInfo.Id == id).Select(e => e.UserInfo).FirstOrDefault();
            }
            return null;
        }

        public Task<SaveResult> CreateUser(NewUserInput input)
        {
            return Task.FromResult(CreateUser(input, PublisherId));
        }

        public SaveResult CreateUser(NewUserInput input, string? publisherId)
        {
            publisherId ??= string.Empty;

            SaveResult result = new SaveResult();
            if (!users.TryGetValue(publisherId, out List<Entry>? list))
            {
                list = new List<Entry>();
                users.Add(publisherId, list);
            }

            PersistentUserInfo userInfo = new PersistentUserInfo
            {
                Id = input.IdentificationNumber!,
                Email = input.Email,
                Role = input.Role,
                FirstName = input.FirstName,
                LastName = input.LastName
            };

            list.Add(new Entry(userInfo, DateTimeOffset.UtcNow));

            result.Id = userInfo.Id;
            result.Success = true;
            return result;
        }

        public void AddUser(string? publisherId, PersistentUserInfo userInfo)
        {
            publisherId ??= string.Empty;

            if (!users.TryGetValue(publisherId, out List<Entry>? list))
            {
                list = new List<Entry>();
                users.Add(publisherId, list);
            }

            list.Add(new Entry(userInfo, DateTimeOffset.UtcNow));
        }

        public Task<SaveResult> UpdateUser(EditUserInput input)
        {
            return Task.FromResult(UpdateUser(input, PublisherId));
        }

        public SaveResult UpdateUser(EditUserInput input, string publisherId)
        {
            publisherId ??= string.Empty;

            SaveResult result = new SaveResult();
            if (!users.TryGetValue(publisherId, out List<Entry>? list))
            {
                list = new List<Entry>();
                users.Add(publisherId, list);
            }

            list.RemoveAll(u => u.UserInfo.Id == input.Id);

            PersistentUserInfo userInfo = new PersistentUserInfo
            {
                Id = input.Id!,
                Email = input.Email,
                Role = input.Role,
                FirstName = input.FirstName,
                LastName = input.LastName
            };

            list.Add(new Entry(userInfo, DateTimeOffset.UtcNow));

            result.Id = userInfo.Id;
            result.Success = true;
            return result;
        }

        public Task DeleteUser(string id)
        {
            return DeleteUser(id, PublisherId);
        }

        public Task DeleteUser(string id, string publisherId)
        {
            publisherId ??= string.Empty;

            if (users.TryGetValue(publisherId, out List<Entry>? list))
            {
                list.RemoveAll(u => u.UserInfo.Id == id);
            }
            return Task.CompletedTask;
        }

        public Task<TokenResult> RefreshToken(string token, string refreshToken)
        {
            return tokenService.RefreshToken(token, refreshToken);
        }

        public Task<DelegationAuthorizationResult?> Logout(string? content)
        {
            return Task.FromResult<DelegationAuthorizationResult?>(null);
        }

        public Task<TokenResult> DelegatePublisher(string publisherId)
        {
            return tokenService.DelegateToken(httpContextValueAccessor, publisherId);
        }

        public Task<UserInfo> GetUserInfo()
        {
            string publisherId = httpContextValueAccessor.Publisher ?? string.Empty;

            if (users.TryGetValue(publisherId, out List<Entry>? list))
            {
                PersistentUserInfo? persistentUserInfo = list.Where(u => u.UserInfo.Id == httpContextValueAccessor.UserId).Select(e => e.UserInfo).FirstOrDefault();
                if (persistentUserInfo is not null)
                {
                    return Task.FromResult(new UserInfo
                    {
                        FirstName = persistentUserInfo.FirstName,
                        LastName = persistentUserInfo.LastName,
                        Role = persistentUserInfo.Role,
                        Email = persistentUserInfo.Email,
                        Id = persistentUserInfo.Id,
                        Publisher = httpContextValueAccessor.Publisher,
                        CompanyName = httpContextValueAccessor.Publisher,
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
            if (string.IsNullOrEmpty(content))
            {
                throw new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden);
            }
            else
            {
                DateTimeOffset refreshTokenAfter = DateTimeOffset.UtcNow.Add(RefreshTokenAfter);

                return Task.FromResult(new TokenResult { Token = content[6..], RefreshToken = "1", Expires = refreshTokenAfter.AddMinutes(30), RefreshTokenAfter = refreshTokenAfter });
            }
        }

        private record Entry(PersistentUserInfo UserInfo, DateTimeOffset Updated) { }
    }
}
