using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class TestNotificationSettingService : INotificationSettingService
    {
        private readonly List<Setting> settings = new List<Setting>();

        private Setting GetOrCreateSetting(string email)
        {
            Setting? setting = settings.FirstOrDefault(e => e.Email == email);
            if (setting is null)
            {
                setting = new Setting { Email = email, AuthKey = Guid.NewGuid().ToString() };
                settings.Add(setting);
            }
            return setting;
        }

        public string GetAuthKey(string email)
        {
            return GetOrCreateSetting(email).AuthKey;
        }

        public bool IsDisabled(string email)
        {
            return settings.FirstOrDefault(e => e.Email == email)?.IsDisabled ?? false;
        }

        public Task UpdateSetting(string email, bool isDisabled)
        {
            Setting setting = GetOrCreateSetting(email);
            setting.IsDisabled = isDisabled;
            return Task.CompletedTask;
        }

        public Task UpdateSettingWithAuthKey(string authKey, bool isDisabled)
        {
            Setting? setting = settings.FirstOrDefault(e => e.AuthKey == authKey);
            if (setting is not null)
            {
                setting.IsDisabled = isDisabled;
            }
            return Task.CompletedTask;
        }

        public Task<NotificationSetting> GetCurrent(string email)
        {
            Setting? setting = GetOrCreateSetting(email);
            return Task.FromResult(new NotificationSetting { Email = setting?.Email ?? string.Empty, IsDisabled = setting?.IsDisabled ?? false });
        }

        public Task<NotificationSetting> GetCurrentWithAuthKey(string authKey)
        {
            Setting? setting = settings.FirstOrDefault(e => e.AuthKey == authKey);
            return Task.FromResult(new NotificationSetting { Email = setting?.Email ?? string.Empty, IsDisabled = setting?.IsDisabled ?? false });
        }

        private class Setting
        {
            public string Email { get; set; } = string.Empty;

            public string AuthKey { get; set; } = string.Empty;

            public bool IsDisabled { get; set; }
        }
    }
}
