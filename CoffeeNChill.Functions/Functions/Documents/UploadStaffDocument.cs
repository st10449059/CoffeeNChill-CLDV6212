// Code Attribution:
// The implementation for handling HTTP file uploads in an Azure Function
// was adapted from Microsoft's documentation on Azure Functions HTTP triggers.
// Reference: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Documents
{
    public class UploadStaffDocument
    {
        private readonly ILogger _logger;
        private readonly FileShareService _fileShareService;

        public UploadStaffDocument(ILoggerFactory loggerFactory, FileShareService fileShareService)
        {
            _logger = loggerFactory.CreateLogger<UploadStaffDocument>();
            _fileShareService = fileShareService;
        }

        // Aligning the route exactly with the POE requirement: POST /api/documents/upload[cite: 1]
        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req)
        {
            _logger.LogInformation("Processing a new staff document upload request.");

            // To handle the file stream dynamically for testing, we will look for a custom header named 'File-Name'.
            // If the header is missing, we generate a unique fallback name to prevent overwriting existing files.
            string fileName = req.Headers.TryGetValues("File-Name", out var headerValues)
                ? headerValues.FirstOrDefault()
                : $"uploaded-doc-{System.Guid.NewGuid()}.pdf";

            // Pass the raw request body stream directly to the File Share service
            await _fileShareService.UploadFileAsync(fileName, req.Body);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync($"Successfully uploaded {fileName} to the staff-docs file share.");
            return response;
        }
    }
}