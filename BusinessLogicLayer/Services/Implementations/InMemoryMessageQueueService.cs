using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BusinessLogicLayer.Services.Implementations
{
    public class InMemoryMessageQueueService : IMessageQueueService
    {
        private readonly ConcurrentQueue<MessageEntity> _messageQueue;
        private readonly ILogger<InMemoryMessageQueueService> _logger;
        private static bool _hasLoggedWarning = false;
        private static readonly object _lockObject = new object();

        public InMemoryMessageQueueService(ILogger<InMemoryMessageQueueService> logger)
        {
            _messageQueue = new ConcurrentQueue<MessageEntity>();
            _logger = logger;

            // Only log the warning once across all instances
            if (!_hasLoggedWarning)
            {
                lock (_lockObject)
                {
                    if (!_hasLoggedWarning)
                    {
                        _logger.LogInformation("🔄 Using In-Memory Message Queue Service (Redis fallback mode)");
                        _hasLoggedWarning = true;
                    }
                }
            }
        }

        public async Task EnqueueMessageAsync(MessageEntity message)
        {
            try
            {
                _messageQueue.Enqueue(message);
                _logger.LogTrace($"Enqueued message {message.MessageId} (in-memory)");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enqueueing message {message.MessageId}");
                throw;
            }
        }

        public async Task<MessageEntity?> DequeueMessageAsync()
        {
            try
            {
                if (_messageQueue.TryDequeue(out MessageEntity? message))
                {
                    _logger.LogTrace($"Dequeued message {message?.MessageId} (in-memory)");
                    return await Task.FromResult(message);
                }
                return await Task.FromResult<MessageEntity?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dequeuing message");
                throw;
            }
        }

        public async Task EnqueueBulkMessagesAsync(List<MessageEntity> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    _messageQueue.Enqueue(message);
                }
                _logger.LogDebug($"Bulk enqueued {messages.Count} messages (in-memory)");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error bulk enqueueing {messages.Count} messages");
                throw;
            }
        }

        public async Task<List<MessageEntity>> DequeueBulkMessagesAsync(int batchSize = 100)
        {
            try
            {
                var messages = new List<MessageEntity>();
                for (int i = 0; i < batchSize && _messageQueue.TryDequeue(out MessageEntity? message); i++)
                {
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }

                if (messages.Count > 0)
                {
                    _logger.LogDebug($"Bulk dequeued {messages.Count} messages (in-memory)");
                }

                return await Task.FromResult(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk dequeuing messages");
                throw;
            }
        }

        public async Task<int> GetQueueSizeAsync()
        {
            try
            {
                var size = _messageQueue.Count;
                return await Task.FromResult(size);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue size");
                return 0;
            }
        }

        public async Task ClearQueueAsync()
        {
            try
            {
                while (_messageQueue.TryDequeue(out _)) { }
                _logger.LogInformation("Queue cleared (in-memory)");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing queue");
                throw;
            }
        }
    }
}