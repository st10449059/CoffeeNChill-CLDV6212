using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Menu
{
    public class GetAllMenuItems
    {
        private readonly ILogger _logger;
        private readonly MenuTableService _menuTableService;

        public GetAllMenuItems(ILoggerFactory loggerFactory, MenuTableService menuTableService)
        {
            _logger = loggerFactory.CreateLogger<GetAllMenuItems>();
            _menuTableService = menuTableService;
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("Processing a GET request to fetch all menu items.");

            var items = await _menuTableService.GetAllMenuItemsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }
    }
}