// Code Attribution:
// The logic for serializing Azure File Share metadata into a JSON response
// is based on standard REST API practices for ASP.NET Core and Azure Functions.
// Reference: https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting

using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Documents
{
    public class ListStaffDocuments
    {
        private readonly ILogger _logger;
        private readonly FileShareService _fileShareService;

        public ListStaffDocuments(ILoggerFactory loggerFactory, FileShareService fileShareService)
        {
            _logger = loggerFactory.CreateLogger<ListStaffDocuments>();
            _fileShareService = fileShareService;
        }

        // Aligning the route exactly with the POE requirement: GET /api/documents[cite: 1]
        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req)
        {
            _logger.LogInformation("Retrieving the list of staff documents from the file share.");

            // Fetch the list of DTOs (Data Transfer Objects) containing the file metadata
            var files = await _fileShareService.ListFilesAsync();

            // Return a 200 OK response with the file metadata automatically serialized into JSON
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(files);

            return response;
        }
    }
}