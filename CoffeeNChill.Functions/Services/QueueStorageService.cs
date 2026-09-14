using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureWebJobsStorage"];
            _queueClient = new QueueClient(connectionString, "order-queue");

            // Create the order queue if it does not already exist.
            _queueClient.CreateIfNotExists();
        }

        public async Task SendMessageAsync(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }
    }
}
