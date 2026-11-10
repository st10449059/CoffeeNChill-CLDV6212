using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Provides protected staff-document management.
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize(Policy = "StaffOnly")]
public sealed class DocumentsController : ControllerBase
{
    private const int MaxDownloadBytes = 20 * 1024 * 1024;

    private readonly IHttpClientFactory _clients;
    private readonly ILogger<DocumentsController> _logger;

    /// <summary>
    /// Creates the staff-document controller.
    /// </summary>
    public DocumentsController(
        IHttpClientFactory clients,
        ILogger<DocumentsController> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <summary>
    /// Lists the documents available to Staff and Admin users.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient("FunctionsBackend");

            using var response = await client.GetAsync(
                "api/documents", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendFailure((int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(
                cancellationToken);

            return Content(json, "application/json");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Document listing failed.");
            return BackendFailure(0);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Document service timed out."
            });
        }
    }

    /// <summary>
    /// Downloads one staff document.
    /// </summary>
    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> Download(
        [FromRoute] string fileName,
        CancellationToken cancellationToken)
    {
        if (!IsValidFileName(fileName))
        {
            return BadRequest(new
            {
                message = "Invalid document filename."
            });
        }

        try
        {
            var client = _clients.CreateClient("FunctionsBackend");

            using var response = await client.GetAsync(
                "api/documents/download/" +
                Uri.EscapeDataString(fileName),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            if (!response.IsSuccessStatusCode)
            {
                return BackendFailure((int)response.StatusCode);
            }

            if (response.Content.Headers.ContentLength
                is long length && length > MaxDownloadBytes)
            {
                return StatusCode(502, new
                {
                    message = "Document exceeds the gateway download limit."
                });
            }

            await using var input =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var output = new MemoryStream();
            var buffer = new byte[81920];

            int bytesRead;

            while ((bytesRead = await input.ReadAsync(
                buffer.AsMemory(), cancellationToken)) > 0)
            {
                if (output.Length + bytesRead > MaxDownloadBytes)
                {
                    return StatusCode(502, new
                    {
                        message = "Document exceeds the gateway download limit."
                    });
                }

                await output.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);
            }

            var contentType =
                response.Content.Headers.ContentType?.MediaType ??
                "application/octet-stream";

            return File(
                output.ToArray(),
                contentType,
                fileName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Document download failed.");
            return BackendFailure(0);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Document service timed out."
            });
        }
    }

    /// <summary>
    /// Deletes a staff document.
    /// </summary>
    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(
        [FromRoute] string fileName,
        CancellationToken cancellationToken)
    {
        if (!IsValidFileName(fileName))
        {
            return BadRequest(new
            {
                message = "Invalid document filename."
            });
        }

        try
        {
            var client = _clients.CreateClient("FunctionsBackend");

            using var response = await client.DeleteAsync(
                "api/documents/" +
                Uri.EscapeDataString(fileName),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return BadRequest(new
                {
                    message = "Invalid document deletion request."
                });
            }

            if (!response.IsSuccessStatusCode)
            {
                return BackendFailure((int)response.StatusCode);
            }

            return NoContent();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Document deletion failed.");
            return BackendFailure(0);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Document service timed out."
            });
        }
    }

    private static bool IsValidFileName(string? fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) &&
            fileName is not "." and not ".." &&
            Regex.IsMatch(
                fileName,
                @"\A[A-Za-z0-9][A-Za-z0-9_. -]{0,127}\z",
                RegexOptions.CultureInvariant);
    }

    private IActionResult BackendFailure(int backendStatus)
    {
        _logger.LogWarning(
            "Document backend returned HTTP {StatusCode}.",
            backendStatus);

        return StatusCode(502, new
        {
            message = "Document service is unavailable."
        });
    }
}
