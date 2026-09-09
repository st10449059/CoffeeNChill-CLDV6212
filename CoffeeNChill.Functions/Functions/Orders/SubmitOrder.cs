using System.IO;
using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions.Orders
{
    public class SubmitOrder
    {
        private readonly QueueStorageService _queueService;

        public SubmitOrder(QueueStorageService queueService)
        {
            _queueService = queueService;
        }

        [Function("SubmitOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            // Read the order data sent in the request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Send it to the queue
            await _queueService.SendMessageAsync(requestBody);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Order successfully sent to the queue.");
            return response;
        }
    }
}