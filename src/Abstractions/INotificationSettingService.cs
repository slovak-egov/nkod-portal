using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface INotificationSettingService
    {
        Task<NotificationSetting?> GetCurrent(string email);

        Task<NotificationSetting?> GetCurrentWithAuthKey(string authKey);

        Task UpdateSetting(string email, bool isDisabled);

        Task UpdateSettingWithAuthKey(string authKey, bool isDisabled);
    }
}
