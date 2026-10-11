using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureWebJobsStorage"];
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("AzureWebJobsStorage is not configured.");

            var options = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            _queueClient = new QueueClient(
                connectionString, "order-processing-queue", options);
        }

        public async Task SendMessageAsync(string message)
        {
            await _queueClient.CreateIfNotExistsAsync();
            await _queueClient.SendMessageAsync(message);
        }
    }
}