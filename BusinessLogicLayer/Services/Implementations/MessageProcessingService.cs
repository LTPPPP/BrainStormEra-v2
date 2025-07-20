using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.AspNetCore.SignalR;
using BusinessLogicLayer.Hubs;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Implementations
{
    public class MessageProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageProcessingService> _logger;
        private readonly int _batchSize = 100;
        private readonly int _maxRetryAttempts = 3;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _processingDelay = TimeSpan.FromMilliseconds(100);

        public MessageProcessingService(
            IServiceProvider serviceProvider,
            ILogger<MessageProcessingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message Processing Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessMessageBatch(stoppingToken);
                    await Task.Delay(_processingDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Message Processing Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in message processing service");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("Message Processing Service stopped");
        }

        private async Task ProcessMessageBatch(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
            var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepo>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

            try
            {
                var messages = await messageQueueService.DequeueBulkMessagesAsync(_batchSize);

                if (messages.Any())
                {
                    _logger.LogInformation($"Processing batch of {messages.Count} messages");

                    var successfulMessages = new List<MessageEntity>();
                    var failedMessages = new List<MessageEntity>();

                    // Process messages in parallel with controlled concurrency
                    var semaphore = new SemaphoreSlim(10, 10); // Limit concurrent processing
                    var tasks = messages.Select(async message =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var success = await ProcessSingleMessage(message, chatRepository, hubContext, cancellationToken);
                            if (success)
                            {
                                lock (successfulMessages)
                                {
                                    successfulMessages.Add(message);
                                }
                            }
                            else
                            {
                                lock (failedMessages)
                                {
                                    failedMessages.Add(message);
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);

                    _logger.LogInformation($"Batch processing completed. Success: {successfulMessages.Count}, Failed: {failedMessages.Count}");

                    // Handle failed messages with retry logic
                    if (failedMessages.Any())
                    {
                        await HandleFailedMessages(failedMessages, messageQueueService);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message batch");
                throw;
            }
        }

        private async Task<bool> ProcessSingleMessage(
            MessageEntity message,
            IChatRepo chatRepository,
            IHubContext<ChatHub> hubContext,
            CancellationToken cancellationToken)
        {
            var retryCount = 0;

            while (retryCount < _maxRetryAttempts)
            {
                try
                {
                    // Create or get conversation
                    var conversation = await chatRepository.GetOrCreateConversationAsync(message.SenderId, message.ReceiverId);
                    if (conversation == null)
                    {
                        _logger.LogError($"Failed to create/get conversation for message {message.MessageId}");
                        return false;
                    }

                    // Set conversation ID
                    message.ConversationId = conversation.ConversationId;

                    // Save message to database
                    var savedMessage = await chatRepository.AddAsync(message);
                    if (savedMessage == null)
                    {
                        _logger.LogError($"Failed to save message {message.MessageId} to database");
                        retryCount++;
                        continue;
                    }

                    // Update conversation
                    conversation.LastMessageId = message.MessageId;
                    conversation.LastMessageAt = message.MessageCreatedAt;
                    conversation.ConversationUpdatedAt = DateTime.UtcNow;
                    await chatRepository.UpdateConversationAsync(conversation);

                    // Get full message with sender info
                    var fullMessage = await chatRepository.GetMessageByIdAsync(message.MessageId);
                    if (fullMessage == null)
                    {
                        _logger.LogError($"Failed to retrieve full message {message.MessageId} after save");
                        retryCount++;
                        continue;
                    }

                    // Send success confirmation to clients
                    await SendSuccessConfirmation(fullMessage, hubContext);

                    _logger.LogDebug($"Successfully processed message {message.MessageId}");
                    return true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError(ex, $"Error processing message {message.MessageId}, attempt {retryCount}");

                    if (retryCount < _maxRetryAttempts)
                    {
                        await Task.Delay(_retryDelay, cancellationToken);
                    }
                }
            }

            _logger.LogError($"Failed to process message {message.MessageId} after {_maxRetryAttempts} attempts");
            return false;
        }

        private async Task SendSuccessConfirmation(MessageEntity message, IHubContext<ChatHub> hubContext)
        {
            try
            {
                var confirmationData = new
                {
                    messageId = message.MessageId,
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    content = message.MessageContent,
                    messageType = message.MessageType,
                    replyToMessageId = message.ReplyToMessageId,
                    createdAt = message.MessageCreatedAt,
                    senderName = message.Sender?.Username ?? "Unknown",
                    senderAvatar = message.Sender?.UserImage,
                    status = "delivered"
                };

                // Send to receiver
                await hubContext.Clients.User(message.ReceiverId).SendAsync("MessageDelivered", confirmationData);

                // Send confirmation to sender
                await hubContext.Clients.User(message.SenderId).SendAsync("MessageConfirmed", confirmationData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending success confirmation for message {message.MessageId}");
            }
        }

        private async Task HandleFailedMessages(List<MessageEntity> failedMessages, IMessageQueueService messageQueueService)
        {
            try
            {
                // Re-queue failed messages for retry (with exponential backoff)
                var reQueueTasks = failedMessages.Select(async message =>
                {
                    // Add retry metadata
                    var retryCount = GetRetryCount(message);
                    if (retryCount < _maxRetryAttempts)
                    {
                        // Exponential backoff
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount) * 5);
                        await Task.Delay(delay);

                        SetRetryCount(message, retryCount + 1);
                        await messageQueueService.EnqueueMessageAsync(message);
                        _logger.LogWarning($"Re-queued message {message.MessageId} for retry {retryCount + 1}");
                    }
                    else
                    {
                        _logger.LogError($"Message {message.MessageId} exceeded max retry attempts, moving to dead letter queue");
                        await HandleDeadLetterMessage(message);
                    }
                });

                await Task.WhenAll(reQueueTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling failed messages");
            }
        }

        private int GetRetryCount(MessageEntity message)
        {
            // Simple retry count tracking (in production, you might want to use a separate field)
            return message.MessageContent.StartsWith("RETRY:") ?
                int.Parse(message.MessageContent.Split(':')[1]) : 0;
        }

        private void SetRetryCount(MessageEntity message, int retryCount)
        {
            // Simple retry count tracking (in production, you might want to use a separate field)
            if (message.MessageContent.StartsWith("RETRY:"))
            {
                var parts = message.MessageContent.Split(':', 3);
                message.MessageContent = $"RETRY:{retryCount}:{parts[2]}";
            }
            else
            {
                message.MessageContent = $"RETRY:{retryCount}:{message.MessageContent}";
            }
        }

        private async Task HandleDeadLetterMessage(MessageEntity message)
        {
            try
            {
                // Log dead letter message
                _logger.LogError($"Dead letter message: {message.MessageId} from {message.SenderId} to {message.ReceiverId}");

                // Optionally: Store in dead letter table, send admin notification, etc.
                // For now, just log the failure

                // TODO: Implement dead letter queue storage
                // await _deadLetterRepository.AddAsync(message);

                // TODO: Send notification to admin
                // await _notificationService.SendAdminNotificationAsync("Message delivery failed", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling dead letter message {message.MessageId}");
            }
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Message Processing Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}