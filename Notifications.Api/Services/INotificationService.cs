using System.Collections.Generic;
using System.Threading.Tasks;
using Notifications.Api.DTOs;

namespace Notifications.Api.Services
{
    public interface INotificationService
    {
        Task<NotificationResponse> SendNotificationAsync(NotificationRequest request);
        Task<IEnumerable<NotificationResponse>> GetNotificationsByUserIdAsync(int userId);
    }
}