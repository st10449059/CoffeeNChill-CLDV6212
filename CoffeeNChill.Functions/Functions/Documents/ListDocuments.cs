using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Documents
{
    public class ListDocuments
    {
        private readonly ILogger _logger;
        private readonly FileShareService _fileShareService;

        public ListDocuments(ILoggerFactory loggerFactory, FileShareService fileShareService)
        {
            _logger = loggerFactory.CreateLogger<ListDocuments>();
            _fileShareService = fileShareService;
        }

        [Function("ListDocuments")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req)
        {
            _logger.LogInformation("Processing a GET request to list all staff documents.");

            var files = await _fileShareService.ListFilesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(files);
            return response;
        }
    }
}