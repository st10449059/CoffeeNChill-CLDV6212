using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Menu
{
    public class GetMenuItemsByCategory
    {
        private readonly ILogger _logger;
        private readonly MenuTableService _menuTableService;

        public GetMenuItemsByCategory(ILoggerFactory loggerFactory, MenuTableService menuTableService)
        {
            _logger = loggerFactory.CreateLogger<GetMenuItemsByCategory>();
            _menuTableService = menuTableService;
        }

        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequestData req,
            string category)
        {
            _logger.LogInformation($"Processing a GET request to fetch menu items for category: {category}");

            var items = await _menuTableService.GetMenuItemsByCategoryAsync(category);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }
    }
}