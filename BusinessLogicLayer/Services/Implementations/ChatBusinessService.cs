using BusinessLogicLayer.DTOs.Chat;
using BusinessLogicLayer.DTOs.Common;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Services.Implementations
{
    public class ChatBusinessService : IChatBusinessService
    {
        private readonly IChatService _chatService;
        private readonly ICourseRepo _courseRepo;
        private readonly IUserRepo _userRepo;
        private readonly ILogger<ChatBusinessService> _logger;

        public ChatBusinessService(IChatService chatService, ICourseRepo courseRepo, IUserRepo userRepo, ILogger<ChatBusinessService> logger)
        {
            _chatService = chatService;
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        // Hàm dùng chung lấy danh sách chat users theo role
        private async Task<List<Account>> GetAvailableChatUsersAsync(string currentUserId)
        {
            var currentUser = await _userRepo.GetByIdAsync(currentUserId);
            List<Account> allUsers = new List<Account>();
            if (currentUser != null && currentUser.UserRole == "instructor")
            {
                // Lấy tất cả course mà instructor là tác giả
                var instructorCourses = await _courseRepo.GetInstructorCoursesAsync(currentUserId, null, null, 1, int.MaxValue);
                // Lấy tất cả userId đã enroll vào các course này
                var enrolledStudentIds = new HashSet<string>();
                foreach (var course in instructorCourses)
                {
                    var userIds = await _courseRepo.GetEnrolledUserIdsAsync(course.CourseId);
                    foreach (var id in userIds)
                        enrolledStudentIds.Add(id);
                }
                // Lấy thông tin account của các học viên này
                var allUsersList = await _userRepo.GetAllUsersAsync(null, null, 1, int.MaxValue);
                var enrolledStudents = allUsersList.Where(u => enrolledStudentIds.Contains(u.UserId)).ToList();
                // Lấy users đã từng chat
                var users = await _chatService.GetChatUsersAsync(currentUserId);
                // Hợp nhất
                allUsers = users.Union(enrolledStudents).GroupBy(u => u.UserId).Select(g => g.First()).ToList();
            }
            else if (currentUser != null && currentUser.UserRole == "learner")
            {
                // Lấy tất cả course mà learner đã enroll
                var enrollments = await _courseRepo.GetUserEnrollmentsAsync(currentUserId);
                var instructorIds = enrollments
                    .Select(e => e.Course.AuthorId)
                    .Where(id => !string.IsNullOrEmpty(id) && id != currentUserId)
                    .Distinct()
                    .ToList();
                // Lấy thông tin account của các instructor này
                var allUsersList = await _userRepo.GetAllUsersAsync(null, null, 1, int.MaxValue);
                var instructors = allUsersList.Where(u => instructorIds.Contains(u.UserId)).ToList();
                // Lấy users đã từng chat
                var users = await _chatService.GetChatUsersAsync(currentUserId);
                // Hợp nhất
                allUsers = users.Union(instructors).GroupBy(u => u.UserId).Select(g => g.First()).ToList();
            }
            else
            {
                // Giữ nguyên logic cũ cho các role khác
                allUsers = await _chatService.GetChatUsersAsync(currentUserId);
            }
            return allUsers;
        }

        public async Task<ServiceResult<ChatIndexViewModel>> GetChatIndexViewModelAsync(string currentUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<ChatIndexViewModel>.Failure("User not authenticated");
                }

                var allUsers = await GetAvailableChatUsersAsync(currentUserId);

                var chatUsers = await MapToChatUserDTOsAsync(allUsers, currentUserId);

                var viewModel = new ChatIndexViewModel
                {
                    CurrentUserId = currentUserId,
                    Users = chatUsers
                };

                return ServiceResult<ChatIndexViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat index view model for user {UserId}", currentUserId);
                return ServiceResult<ChatIndexViewModel>.Failure("An error occurred while loading chat users");
            }
        }

        public async Task<ServiceResult<ConversationViewModel>> GetConversationViewModelAsync(string currentUserId, string receiverId)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<ConversationViewModel>.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(receiverId))
                {
                    return ServiceResult<ConversationViewModel>.Failure("Receiver ID is required");
                }

                // Get chat users for sidebar (dùng logic giống trang Index)
                var allUsers = await GetAvailableChatUsersAsync(currentUserId);
                var chatUsers = await MapToChatUserDTOsAsync(allUsers, currentUserId);

                // Get messages
                var messages = await _chatService.GetConversationMessagesAsync(currentUserId, receiverId);
                var messageDTOs = MapToChatMessageDTOs(messages);

                var viewModel = new ConversationViewModel
                {
                    CurrentUserId = currentUserId,
                    ReceiverId = receiverId,
                    ChatUsers = chatUsers,
                    Messages = messageDTOs
                };

                return ServiceResult<ConversationViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation view model for users {CurrentUserId} and {ReceiverId}", currentUserId, receiverId);
                return ServiceResult<ConversationViewModel>.Failure("An error occurred while loading conversation");
            }
        }

        public async Task<ServiceResult<List<ChatMessageDTO>>> GetMessagesAsync(string currentUserId, GetMessagesRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<List<ChatMessageDTO>>.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.ReceiverId))
                {
                    return ServiceResult<List<ChatMessageDTO>>.Failure("Receiver ID is required");
                }

                var messages = await _chatService.GetConversationMessagesAsync(currentUserId, request.ReceiverId, request.Page, request.PageSize);
                var messageDTOs = MapToChatMessageDTOs(messages).OrderBy(m => m.CreatedAt).ToList(); // Show oldest first

                return ServiceResult<List<ChatMessageDTO>>.Success(messageDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for user {UserId}", currentUserId);
                return ServiceResult<List<ChatMessageDTO>>.Failure("An error occurred while loading messages");
            }
        }

        public async Task<ServiceResult<ChatMessageDTO>> SendMessageAsync(string currentUserId, SendMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<ChatMessageDTO>.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.ReceiverId) || string.IsNullOrEmpty(request.Message))
                {
                    return ServiceResult<ChatMessageDTO>.Failure("Invalid message data");
                }

                var message = await _chatService.SendMessageAsync(currentUserId, request.ReceiverId, request.Message, request.ReplyToMessageId);

                if (message != null)
                {
                    var messageDTO = MapToChatMessageDTO(message);
                    return ServiceResult<ChatMessageDTO>.Success(messageDTO);
                }

                return ServiceResult<ChatMessageDTO>.Failure("Failed to send message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from user {UserId}", currentUserId);
                return ServiceResult<ChatMessageDTO>.Failure("An error occurred while sending message");
            }
        }

        public async Task<ServiceResult> MarkMessageAsReadAsync(string currentUserId, MarkAsReadRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.MessageId))
                {
                    return ServiceResult.Failure("Message ID is required");
                }

                var result = await _chatService.MarkMessageAsReadAsync(request.MessageId, currentUserId);

                if (result)
                {
                    return ServiceResult.Success("Message marked as read");
                }

                return ServiceResult.Failure("Failed to mark message as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read for user {UserId}", currentUserId);
                return ServiceResult.Failure("An error occurred while marking message as read");
            }
        }

        public async Task<ServiceResult<int>> GetUnreadCountAsync(string currentUserId, GetUnreadCountRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<int>.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.SenderId))
                {
                    return ServiceResult<int>.Failure("Sender ID is required");
                }

                var count = await _chatService.GetUnreadMessageCountAsync(currentUserId, request.SenderId);

                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", currentUserId);
                return ServiceResult<int>.Failure("An error occurred while getting unread count");
            }
        }

        public async Task<ServiceResult> DeleteMessageAsync(string currentUserId, DeleteMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.MessageId))
                {
                    return ServiceResult.Failure("Message ID is required");
                }

                var result = await _chatService.DeleteMessageAsync(request.MessageId, currentUserId);

                if (result)
                {
                    return ServiceResult.Success("Message deleted successfully");
                }

                return ServiceResult.Failure("Failed to delete message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message for user {UserId}", currentUserId);
                return ServiceResult.Failure("An error occurred while deleting message");
            }
        }

        public async Task<ServiceResult> EditMessageAsync(string currentUserId, EditMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult.Failure("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.MessageId) || string.IsNullOrEmpty(request.NewContent))
                {
                    return ServiceResult.Failure("Message ID and new content are required");
                }

                var result = await _chatService.EditMessageAsync(request.MessageId, request.NewContent, currentUserId);

                if (result)
                {
                    return ServiceResult.Success("Message edited successfully");
                }

                return ServiceResult.Failure("Failed to edit message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message for user {UserId}", currentUserId);
                return ServiceResult.Failure("An error occurred while editing message");
            }
        }

        public async Task<ServiceResult<Conversation>> GetOrCreateConversationAsync(string userId1, string userId2)
        {
            try
            {
                if (string.IsNullOrEmpty(userId1) || string.IsNullOrEmpty(userId2))
                {
                    return ServiceResult<Conversation>.Failure("Both user IDs are required");
                }

                var conversation = await _chatService.GetOrCreateConversationAsync(userId1, userId2);
                if (conversation != null)
                {
                    return ServiceResult<Conversation>.Success(conversation);
                }

                return ServiceResult<Conversation>.Failure("Failed to get or create conversation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating conversation between {UserId1} and {UserId2}", userId1, userId2);
                return ServiceResult<Conversation>.Failure("An error occurred while getting conversation");
            }
        }

        private List<ChatMessageDTO> MapToChatMessageDTOs(List<MessageEntity> messages)
        {
            return messages.Select(MapToChatMessageDTO).ToList();
        }
        private ChatMessageDTO MapToChatMessageDTO(MessageEntity message)
        {
            return new ChatMessageDTO
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.MessageContent,
                MessageType = message.MessageType ?? "text",
                IsRead = message.IsRead ?? false,
                ReplyToMessageId = message.ReplyToMessageId,
                IsEdited = message.IsEdited ?? false,
                CreatedAt = message.MessageCreatedAt,
                SenderName = message.Sender?.Username ?? "Unknown",
                SenderAvatar = message.Sender?.UserImage
            };
        }
        private async Task<List<ChatUserDTO>> MapToChatUserDTOsAsync(List<Account> users, string currentUserId)
        {
            var chatUsers = new List<ChatUserDTO>();

            foreach (var user in users)
            {
                var lastMessage = await _chatService.GetLastMessageBetweenUsersAsync(currentUserId, user.UserId);
                var unreadCount = await _chatService.GetUnreadMessageCountAsync(currentUserId, user.UserId);

                // Get course relationships
                var courseRelationships = await GetCourseRelationshipsAsync(currentUserId, user.UserId);

                chatUsers.Add(new ChatUserDTO
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    UserImage = user.UserImage,
                    LastActive = user.LastLogin, // Use LastLogin as LastActive
                    IsOnline = false, // Default to false, can be updated with real-time status
                    LastMessage = lastMessage?.MessageContent,
                    LastMessageTime = lastMessage?.MessageCreatedAt,
                    UnreadCount = unreadCount,
                    UserRole = user.UserRole,
                    CourseRelationships = courseRelationships
                });
            }

            return chatUsers.OrderByDescending(u => u.LastMessageTime).ToList();
        }

        private async Task<List<CourseRelationshipInfo>> GetCourseRelationshipsAsync(string currentUserId, string otherUserId)
        {
            try
            {
                var relationships = new List<CourseRelationshipInfo>();

                // Get current user's role
                var currentUser = await _userRepo.GetByIdAsync(currentUserId);
                if (currentUser == null) return relationships;

                if (currentUser.UserRole == "LEARNER")
                {
                    // Current user is a learner, so other user is an instructor
                    // Get courses where current user is enrolled and other user is the instructor
                    var enrollments = await _courseRepo.GetUserEnrollmentsAsync(currentUserId);
                    var instructorCourses = enrollments
                        .Where(e => e.Course.AuthorId == otherUserId)
                        .Select(e => new CourseRelationshipInfo
                        {
                            CourseId = e.CourseId,
                            CourseName = e.Course.CourseName,
                            RelationshipType = "Enrolled",
                            EnrollmentDate = e.EnrollmentCreatedAt,
                            ProgressPercentage = e.ProgressPercentage ?? 0
                        })
                        .ToList();

                    relationships.AddRange(instructorCourses);
                }
                else if (currentUser.UserRole == "INSTRUCTOR")
                {
                    // Current user is an instructor, so other user is a learner
                    // Get courses where other user is enrolled and current user is the instructor
                    var otherUserEnrollments = await _courseRepo.GetUserEnrollmentsAsync(otherUserId);
                    var teachingCourses = otherUserEnrollments
                        .Where(e => e.Course.AuthorId == currentUserId)
                        .Select(e => new CourseRelationshipInfo
                        {
                            CourseId = e.CourseId,
                            CourseName = e.Course.CourseName,
                            RelationshipType = "Teaching",
                            EnrollmentDate = e.EnrollmentCreatedAt,
                            ProgressPercentage = e.ProgressPercentage ?? 0
                        })
                        .ToList();

                    relationships.AddRange(teachingCourses);
                }

                return relationships;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course relationships between {CurrentUserId} and {OtherUserId}", currentUserId, otherUserId);
                return new List<CourseRelationshipInfo>();
            }
        }

        public async Task<ServiceResult<string?>> GetMostRecentConversationUserIdAsync(string currentUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return ServiceResult<string?>.Failure("User not authenticated");
                }

                var users = await _chatService.GetChatUsersAsync(currentUserId);
                var chatUsers = await MapToChatUserDTOsAsync(users, currentUserId);

                // Get the user with the most recent message time
                var mostRecentUser = chatUsers
                    .Where(u => u.LastMessageTime.HasValue)
                    .OrderByDescending(u => u.LastMessageTime)
                    .FirstOrDefault();

                return ServiceResult<string?>.Success(mostRecentUser?.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most recent conversation user for user {UserId}", currentUserId);
                return ServiceResult<string?>.Failure("An error occurred while getting most recent conversation");
            }
        }
    }
}
