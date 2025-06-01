using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BrainStormEra_MVC.Services.Interfaces;

namespace BrainStormEra_MVC.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(INotificationService notificationService, ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                // Add user to their personal group for targeted notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                // Log connection
                _logger.LogInformation($"User {userId} connected to NotificationHub with connection {Context.ConnectionId}");

                // Send unread notification count
                var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(userId);
                await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation($"User {userId} disconnected from NotificationHub");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Mark notification as read
        public async Task MarkAsRead(string notificationId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await _notificationService.MarkAsReadAsync(notificationId, userId);

                // Send updated unread count
                var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(userId);
                await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);
            }
        }

        // Mark all notifications as read
        public async Task MarkAllAsRead()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await _notificationService.MarkAllAsReadAsync(userId);
                await Clients.Caller.SendAsync("UpdateUnreadCount", 0);
            }
        }

        // Join course-specific group for course notifications
        public async Task JoinCourseGroup(string courseId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Course_{courseId}");
        }

        // Leave course-specific group
        public async Task LeaveCourseGroup(string courseId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Course_{courseId}");
        }

        // Join role-specific group (Admin, Instructor, Learner)
        public async Task JoinRoleGroup(string role)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
        }
    }
}
