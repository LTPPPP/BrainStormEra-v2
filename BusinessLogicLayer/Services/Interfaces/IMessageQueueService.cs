using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IMessageQueueService
    {
        Task EnqueueMessageAsync(MessageEntity message);
        Task<MessageEntity?> DequeueMessageAsync();
        Task EnqueueBulkMessagesAsync(List<MessageEntity> messages);
        Task<List<MessageEntity>> DequeueBulkMessagesAsync(int batchSize = 100);
        Task<int> GetQueueSizeAsync();
        Task ClearQueueAsync();
    }
}