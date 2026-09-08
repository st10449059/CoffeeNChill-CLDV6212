using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Documents
{
    public class UploadDocument
    {
        private readonly ILogger _logger;
        private readonly FileShareService _fileShareService;

        public UploadDocument(ILoggerFactory loggerFactory, FileShareService fileShareService)
        {
            _logger = loggerFactory.CreateLogger<UploadDocument>();
            _fileShareService = fileShareService;
        }

        [Function("UploadDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/{fileName}")] HttpRequestData req,
            string fileName)
        {
            _logger.LogInformation($"Processing a POST request to upload document: {fileName}");

            // req.Body contains the raw file data sent in the request
            await _fileShareService.UploadFileAsync(fileName, req.Body);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync($"Successfully uploaded {fileName} to staff-documents.");
            return response;
        }
    }
}