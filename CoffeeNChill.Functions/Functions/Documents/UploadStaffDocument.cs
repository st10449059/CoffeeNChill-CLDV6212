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
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Run(
     [HttpTrigger(AuthorizationLevel.Anonymous, "post",
        Route = "documents/upload")] HttpRequest req)
        {
            if (!req.HasFormContentType ||
                req.ContentType?.StartsWith("multipart/form-data",
                    StringComparison.OrdinalIgnoreCase) != true)
                return new BadRequestObjectResult("Use multipart/form-data with a file field.");

            IFormCollection form;
            try
            {
                form = await req.ReadFormAsync();
            }
            catch (InvalidDataException)
            {
                return new BadRequestObjectResult("The multipart request is invalid.");
            }

            var file = form.Files.GetFile("file");
            if (form.Files.Count != 1 || file == null || file.Length == 0)
                return new BadRequestObjectResult("Attach exactly one non-empty file named 'file'.");

            // Keep uploads within the existing service's single-range upload size.
            if (file.Length > 2 * 1024 * 1024)
                return new ObjectResult("The file must not exceed 2 MiB.") { StatusCode = 413 };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf" && extension != ".png" &&
                extension != ".jpg" && extension != ".jpeg")
                return new BadRequestObjectResult("Allowed file extensions: PDF, PNG, JPG and JPEG.");

            var fileName = $"{Guid.NewGuid():N}{extension}";
            using var stream = file.OpenReadStream();
            await _fileShareService.UploadFileAsync(fileName, stream);
            _logger.LogInformation("Uploaded staff document {FileName}.", fileName);

            return new ObjectResult(new { FileName = fileName }) { StatusCode = 201 };
        }
    }
}
