using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Notifications.Api.DTOs;
using Notifications.Api.Services;

namespace Notifications.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // POST /api/notifications/send
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
        {
            var result = await _notificationService.SendNotificationAsync(request);
           
            return StatusCode(201, result);
        }

        // GET /api/notifications/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetByUserId(int userId)
        {
            var result = await _notificationService.GetNotificationsByUserIdAsync(userId);
            return Ok(result);
        }
    }
}