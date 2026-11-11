using IAMClient;
using NkodSk.Abstractions;

namespace WebApi
{
    public class SuperadminNotificationService : ISuperadminNotificationService
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly string harvesterAuthToken;

        public SuperadminNotificationService(IHttpClientFactory httpClientFactory, string harvesterAuthToken)
        {
            this.httpClientFactory = httpClientFactory;
            this.harvesterAuthToken = harvesterAuthToken;
        }

        public async Task Notify(string subject, string body)
        {
            if (string.IsNullOrEmpty(harvesterAuthToken))
            {
                throw new InvalidOperationException("HarvesterAuthToken is not configured");
            }

            HarvesterContextValueAccessor accessor = new HarvesterContextValueAccessor();
            IdentityAccessManagementClient client = new IdentityAccessManagementClient(httpClientFactory, accessor);
            accessor.Token = await client.LoginHarvester(harvesterAuthToken, null).ConfigureAwait(false);

            SaveResult result = await client.NotifySuperadmins(new AdminNotificationInput { Subject = subject, Body = body }).ConfigureAwait(false);
            if (!result.Success)
            {
                throw new InvalidOperationException("Superadmin notification was not sent");
            }
        }

        private class HarvesterContextValueAccessor : IHttpContextValueAccessor
        {
            public string? Publisher => null;

            public string? Token { get; set; }

            public string? UserId => null;

            public bool HasRole(string role)
            {
                return string.Equals(role, "Harvester", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
