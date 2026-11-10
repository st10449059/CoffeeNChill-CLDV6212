using System;
using System.Net;
using System.Threading.Tasks;
using Azure;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions.Documents;

public class DeleteStaffDocument
{
    private readonly FileShareService _fileShareService;
    private readonly ILogger<DeleteStaffDocument> _logger;

    public DeleteStaffDocument(
        FileShareService fileShareService,
        ILogger<DeleteStaffDocument> logger)
    {
        _fileShareService = fileShareService;
        _logger = logger;
    }

    [Function("DeleteStaffDocument")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "documents/{fileName}")]
        HttpRequestData req,
        string fileName)
    {
        try
        {
            bool deleted = await _fileShareService.DeleteFileAsync(fileName);

            if (!deleted)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Document not found.");
                return notFound;
            }

            _logger.LogInformation("Staff document deleted successfully.");

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (ArgumentException)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid document filename.");
            return badRequest;
        }
        catch (RequestFailedException ex) when (ex.Status == 400)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid document filename.");
            return badRequest;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Azure Files failed to delete the requested document.");

            var unavailable = req.CreateResponse(
                HttpStatusCode.ServiceUnavailable);

            await unavailable.WriteStringAsync(
                "Document storage is unavailable.");

            return unavailable;
        }
    }
}
