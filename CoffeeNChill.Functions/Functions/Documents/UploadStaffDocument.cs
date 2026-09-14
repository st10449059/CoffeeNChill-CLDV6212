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

        // Handles POST requests for uploading staff documents.
        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req)
        {
            _logger.LogInformation("Processing a new staff document upload request.");

            // Use the optional File-Name header, or generate a unique fallback name.
            string fileName = req.Headers.TryGetValues("File-Name", out var headerValues)
                ? headerValues.FirstOrDefault()
                : $"uploaded-doc-{System.Guid.NewGuid()}.pdf";

            // Upload the request body to the staff document file share.
            await _fileShareService.UploadFileAsync(fileName, req.Body);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync($"Successfully uploaded {fileName} to the staff-docs file share.");
            return response;
        }
    }
}
