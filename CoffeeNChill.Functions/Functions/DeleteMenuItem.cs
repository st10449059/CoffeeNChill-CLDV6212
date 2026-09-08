using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Menu
{
    public class DeleteMenuItem
    {
        private readonly ILogger _logger;
        private readonly MenuTableService _menuTableService;

        public DeleteMenuItem(ILoggerFactory loggerFactory, MenuTableService menuTableService)
        {
            _logger = loggerFactory.CreateLogger<DeleteMenuItem>();
            _menuTableService = menuTableService;
        }

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            _logger.LogInformation($"Processing a DELETE request for item: {id} in category: {category}");

            await _menuTableService.DeleteMenuItemAsync(category, id);

            // A successful delete generally returns a 204 No Content response
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}