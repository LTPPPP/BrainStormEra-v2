using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Utilities
{
    /// <summary>
    /// Utility class for generating and verifying chat URL hashes
    /// Provides secure URL generation for chat conversations
    /// </summary>
    public class ChatUrlHasher
    {
        private readonly string _secretKey;
        private readonly TimeSpan _urlExpirationTime;

        public ChatUrlHasher(IConfiguration configuration)
        {
            _secretKey = configuration["ChatSecurity:SecretKey"] ?? "DefaultChatSecretKey2024!@#";
            var expirationHours = configuration.GetValue<int>("ChatSecurity:UrlExpirationHours", 24);
            _urlExpirationTime = TimeSpan.FromHours(expirationHours);
        }

        /// <summary>
        /// Generate a secure hash for chat conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="userId1">First user ID</param>
        /// <param name="userId2">Second user ID</param>
        /// <param name="expiresAt">Optional expiration time</param>
        /// <returns>Secure hash string</returns>
        public string GenerateChatHash(string conversationId, string userId1, string userId2, DateTime? expiresAt = null)
        {
            var expiration = expiresAt ?? DateTime.UtcNow.Add(_urlExpirationTime);

            var chatData = new ChatHashData
            {
                ConversationId = conversationId,
                UserId1 = userId1,
                UserId2 = userId2,
                ExpiresAt = expiration,
                Timestamp = DateTime.UtcNow
            };

            var jsonData = JsonSerializer.Serialize(chatData);
            var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
            var signature = GenerateSignature(encodedData);

            return $"{encodedData}.{signature}";
        }

        /// <summary>
        /// Verify and decode chat hash
        /// </summary>
        /// <param name="hash">The hash to verify</param>
        /// <returns>Chat hash data if valid, null if invalid</returns>
        public ChatHashData? VerifyChatHash(string hash)
        {
            try
            {
                var parts = hash.Split('.');
                if (parts.Length != 2)
                    return null;

                var encodedData = parts[0];
                var signature = parts[1];

                // Verify signature
                var expectedSignature = GenerateSignature(encodedData);
                if (signature != expectedSignature)
                    return null;

                // Decode data
                var jsonData = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
                var chatData = JsonSerializer.Deserialize<ChatHashData>(jsonData);

                // Check expiration
                if (chatData != null && chatData.ExpiresAt < DateTime.UtcNow)
                    return null;

                return chatData;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generate a quick access hash for direct user-to-user chat
        /// </summary>
        /// <param name="currentUserId">Current user ID</param>
        /// <param name="targetUserId">Target user ID</param>
        /// <returns>Quick access hash</returns>
        public string GenerateQuickChatHash(string currentUserId, string targetUserId)
        {
            var quickData = new QuickChatHashData
            {
                CurrentUserId = currentUserId,
                TargetUserId = targetUserId,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_urlExpirationTime)
            };

            var jsonData = JsonSerializer.Serialize(quickData);
            var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
            var signature = GenerateSignature(encodedData);

            return $"quick.{encodedData}.{signature}";
        }

        /// <summary>
        /// Verify quick chat hash
        /// </summary>
        /// <param name="hash">Quick chat hash</param>
        /// <returns>Quick chat data if valid</returns>
        public QuickChatHashData? VerifyQuickChatHash(string hash)
        {
            try
            {
                if (!hash.StartsWith("quick."))
                    return null;

                var hashWithoutPrefix = hash.Substring(6); // Remove "quick."
                var parts = hashWithoutPrefix.Split('.');
                if (parts.Length != 2)
                    return null;

                var encodedData = parts[0];
                var signature = parts[1];

                // Verify signature
                var expectedSignature = GenerateSignature(encodedData);
                if (signature != expectedSignature)
                    return null;

                // Decode data
                var jsonData = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
                var quickData = JsonSerializer.Deserialize<QuickChatHashData>(jsonData);

                // Check expiration
                if (quickData != null && quickData.ExpiresAt < DateTime.UtcNow)
                    return null;

                return quickData;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generate a message-specific hash for direct access
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="userId">User ID accessing the message</param>
        /// <returns>Message access hash</returns>
        public string GenerateMessageHash(string messageId, string conversationId, string userId)
        {
            var messageData = new MessageHashData
            {
                MessageId = messageId,
                ConversationId = conversationId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(TimeSpan.FromHours(1)) // Messages expire in 1 hour
            };

            var jsonData = JsonSerializer.Serialize(messageData);
            var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
            var signature = GenerateSignature(encodedData);

            return $"msg.{encodedData}.{signature}";
        }

        /// <summary>
        /// Verify message hash
        /// </summary>
        /// <param name="hash">Message hash</param>
        /// <returns>Message data if valid</returns>
        public MessageHashData? VerifyMessageHash(string hash)
        {
            try
            {
                if (!hash.StartsWith("msg."))
                    return null;

                var hashWithoutPrefix = hash.Substring(4); // Remove "msg."
                var parts = hashWithoutPrefix.Split('.');
                if (parts.Length != 2)
                    return null;

                var encodedData = parts[0];
                var signature = parts[1];

                // Verify signature
                var expectedSignature = GenerateSignature(encodedData);
                if (signature != expectedSignature)
                    return null;

                // Decode data
                var jsonData = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
                var messageData = JsonSerializer.Deserialize<MessageHashData>(jsonData);

                // Check expiration
                if (messageData != null && messageData.ExpiresAt < DateTime.UtcNow)
                    return null;

                return messageData;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generate signature for data using HMAC-SHA256
        /// </summary>
        /// <param name="data">Data to sign</param>
        /// <returns>Base64 encoded signature</returns>
        private string GenerateSignature(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Generate a shareable chat link
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="userId1">First user ID</param>
        /// <param name="userId2">Second user ID</param>
        /// <param name="baseUrl">Base URL of the application</param>
        /// <returns>Complete shareable URL</returns>
        public string GenerateChatUrl(string conversationId, string userId1, string userId2, string baseUrl)
        {
            var hash = GenerateChatHash(conversationId, userId1, userId2);
            return $"{baseUrl.TrimEnd('/')}/Chat/Secure/{hash}";
        }

        /// <summary>
        /// Generate a quick chat URL for user-to-user chat
        /// </summary>
        /// <param name="currentUserId">Current user ID</param>
        /// <param name="targetUserId">Target user ID</param>
        /// <param name="baseUrl">Base URL of the application</param>
        /// <returns>Quick chat URL</returns>
        public string GenerateQuickChatUrl(string currentUserId, string targetUserId, string baseUrl)
        {
            var hash = GenerateQuickChatHash(currentUserId, targetUserId);
            return $"{baseUrl.TrimEnd('/')}/Chat/Quick/{hash}";
        }

        /// <summary>
        /// Generate a message-specific URL
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="baseUrl">Base URL</param>
        /// <returns>Message-specific URL</returns>
        public string GenerateMessageUrl(string messageId, string conversationId, string userId, string baseUrl)
        {
            var hash = GenerateMessageHash(messageId, conversationId, userId);
            return $"{baseUrl.TrimEnd('/')}/Chat/Message/{hash}";
        }
    }

    /// <summary>
    /// Data structure for chat hash
    /// </summary>
    public class ChatHashData
    {
        public string ConversationId { get; set; } = string.Empty;
        public string UserId1 { get; set; } = string.Empty;
        public string UserId2 { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Data structure for quick chat hash
    /// </summary>
    public class QuickChatHashData
    {
        public string CurrentUserId { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Data structure for message hash
    /// </summary>
    public class MessageHashData
    {
        public string MessageId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
