using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace BusinessLogicLayer.Services.Implementations
{
    public class RedisMessageQueueService : IMessageQueueService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisMessageQueueService> _logger;
        private readonly string _queueKey = "message_queue";
        private readonly string _processingKey = "message_processing";

        public RedisMessageQueueService(IConnectionMultiplexer redis, ILogger<RedisMessageQueueService> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task EnqueueMessageAsync(MessageEntity message)
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(message);
                await _database.ListLeftPushAsync(_queueKey, messageJson);
                _logger.LogDebug($"Enqueued message {message.MessageId}");
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
                var messageJson = await _database.ListRightPopLeftPushAsync(_queueKey, _processingKey);
                if (messageJson.HasValue && messageJson != RedisValue.Null)
                {
                    var message = JsonSerializer.Deserialize<MessageEntity>(messageJson.ToString());
                    _logger.LogDebug($"Dequeued message {message?.MessageId}");
                    return message;
                }
                return null;
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
                var batch = _database.CreateBatch();
                var tasks = messages.Select(async message =>
                {
                    var messageJson = JsonSerializer.Serialize(message);
                    await batch.ListLeftPushAsync(_queueKey, messageJson);
                }).ToArray();

                batch.Execute();
                await Task.WhenAll(tasks);
                _logger.LogDebug($"Bulk enqueued {messages.Count} messages");
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
                var batch = _database.CreateBatch();
                var tasks = new List<Task<RedisValue>>();

                for (int i = 0; i < batchSize; i++)
                {
                    tasks.Add(batch.ListRightPopLeftPushAsync(_queueKey, _processingKey));
                }

                batch.Execute();
                var results = await Task.WhenAll(tasks);

                foreach (var result in results)
                {
                    if (result.HasValue)
                    {
                        var messageJson = result.ToString();
                        if (!string.IsNullOrEmpty(messageJson))
                        {
                            var message = JsonSerializer.Deserialize<MessageEntity>(messageJson);
                            if (message != null)
                            {
                                messages.Add(message);
                            }
                        }
                    }
                }

                _logger.LogDebug($"Bulk dequeued {messages.Count} messages");
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error bulk dequeuing messages");
                throw;
            }
        }

        public async Task<int> GetQueueSizeAsync()
        {
            try
            {
                var size = await _database.ListLengthAsync(_queueKey);
                return (int)size;
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
                await _database.KeyDeleteAsync(_queueKey);
                await _database.KeyDeleteAsync(_processingKey);
                _logger.LogInformation("Queue cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing queue");
                throw;
            }
        }

        public async Task MarkMessageProcessedAsync(MessageEntity message)
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(message);
                await _database.ListRemoveAsync(_processingKey, messageJson);
                _logger.LogDebug($"Marked message {message.MessageId} as processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking message {message.MessageId} as processed");
                throw;
            }
        }
    }
}