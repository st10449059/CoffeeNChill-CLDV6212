using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Provides menu access through the CoffeeNChill API Gateway.
/// </summary>
[ApiController]
[Route("api/menu")]
public sealed class MenuController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MenuController> _logger;

    /// <summary>
    /// Creates the menu controller.
    /// </summary>
    public MenuController(
        IHttpClientFactory httpClientFactory,
        ILogger<MenuController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Returns all menu items from the Functions backend.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(
                "FunctionsBackend");

            using var response = await client.GetAsync(
                "api/menu",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Menu backend returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        message = "Menu service is unavailable."
                    });
            }

            var json = await response.Content.ReadAsStringAsync(
                cancellationToken);

            return new ContentResult
            {
                Content = json,
                ContentType = "application/json",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Could not communicate with the menu backend.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Menu service is unavailable."
                });
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Menu backend request timed out.");

            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new
                {
                    message = "Menu service timed out."
                });
        }
    }

    /// <summary>
    /// Updates an existing menu item. Staff or Admin access required.
    /// </summary>
    [Authorize(Policy = "StaffOnly")]
    [HttpPut("{category}/{id}")]
    public async Task<IActionResult> Update(
        [FromRoute] string category,
        [FromRoute] string id,
        [FromBody] CoffeeNChill.Gateway.DTOs.Menu.UpdateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(category) ||
            category.Length > 100 ||
            category.Contains('/') ||
            category.Contains('\\') ||
            category is "." or ".." ||
            string.IsNullOrWhiteSpace(id) ||
            id.Length > 100 ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                id, @"^[A-Za-z0-9_-]+$"))
        {
            return BadRequest(new
            {
                message = "Invalid menu category or item ID."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length > 150 ||
            (request.Description?.Length ?? 0) > 1000 ||
            request.Price is null ||
            !double.IsFinite(request.Price.Value) ||
            request.Price.Value < 0 ||
            request.IsAvailable is null)
        {
            return BadRequest(new
            {
                message = "Invalid menu item details."
            });
        }

        var route =
            "api/menu/" +
            Uri.EscapeDataString(category.Trim()) + "/" +
            Uri.EscapeDataString(id);

        var payload = new
        {
            Name = request.Name.Trim(),
            Description = request.Description ?? string.Empty,
            Price = request.Price.Value,
            IsAvailable = request.IsAvailable.Value
        };

        try
        {
            var client = _httpClientFactory.CreateClient(
                "FunctionsBackend");

            var json = System.Text.Json.JsonSerializer.Serialize(
                payload);

            using var content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");

            using var response = await client.PutAsync(
                route,
                content,
                cancellationToken);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new
                {
                    message = "Menu item not found."
                });
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.BadRequest)
            {
                return BadRequest(new
                {
                    message = "The menu update was rejected."
                });
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new
                {
                    message = "Menu update conflict."
                });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Menu update backend returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                return StatusCode(502, new
                {
                    message = "Menu service is unavailable."
                });
            }

            var responseJson =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            return new ContentResult
            {
                Content = responseJson,
                ContentType = "application/json",
                StatusCode = 200
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex, "Menu update backend connection failed.");

            return StatusCode(502, new
            {
                message = "Menu service is unavailable."
            });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Menu service timed out."
            });
        }
    }

    /// <summary>
    /// Deletes an existing menu item. Staff or Admin access required.
    /// </summary>
    [Authorize(Policy = "StaffOnly")]
    [HttpDelete("{category}/{id}")]
    public async Task<IActionResult> Delete(
        [FromRoute] string category,
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        // Validate route values before forwarding to Functions.
        if (string.IsNullOrWhiteSpace(category) ||
            category.Length > 100 ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                category, @"^[\p{L}\p{N} _-]+$") ||
            string.IsNullOrWhiteSpace(id) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                id, @"^[A-Za-z0-9_-]{1,100}$"))
        {
            return BadRequest(new
            {
                message = "Invalid menu category or item ID."
            });
        }

        var route =
            "api/menu/" +
            Uri.EscapeDataString(category.Trim()) + "/" +
            Uri.EscapeDataString(id);

        try
        {
            var client = _httpClientFactory.CreateClient(
                "FunctionsBackend");

            using var response = await client.DeleteAsync(
                route,
                cancellationToken);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new
                {
                    message = "Menu item not found."
                });
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.BadRequest)
            {
                return BadRequest(new
                {
                    message = "Invalid menu deletion request."
                });
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new
                {
                    message = "Menu item could not be deleted."
                });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Menu deletion backend returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                return StatusCode(502, new
                {
                    message = "Menu service is unavailable."
                });
            }

            return NoContent();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Menu deletion backend connection failed.");

            return StatusCode(502, new
            {
                message = "Menu service is unavailable."
            });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Menu service timed out."
            });
        }
    }

    /// <summary>
    /// Retrieves menu items for a specific category.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(
        [FromRoute] string category,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(category) ||
            category.Length > 100 ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                category, @"^[\p{L}\p{N} _-]+$"))
        {
            return BadRequest(new
            {
                message = "Invalid menu category."
            });
        }

        var route = "api/menu/category/" +
            Uri.EscapeDataString(category.Trim());

        try
        {
            var client = _httpClientFactory.CreateClient(
                "FunctionsBackend");

            using var response = await client.GetAsync(
                route, cancellationToken);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new
                {
                    message = "Menu category not found."
                });
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(502, new
                {
                    message = "Menu service is unavailable."
                });
            }

            var json = await response.Content.ReadAsStringAsync(
                cancellationToken);

            return new ContentResult
            {
                Content = json,
                ContentType = "application/json",
                StatusCode = 200
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex, "Menu category backend request failed.");

            return StatusCode(502, new
            {
                message = "Menu service is unavailable."
            });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Menu service timed out."
            });
        }
    }

    /// <summary>
    /// Creates a new menu item. Staff or Admin access required.
    /// </summary>
    [Authorize(Policy = "StaffOnly")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CoffeeNChill.Gateway.DTOs.Menu.CreateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Category) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                request.Category,
                @"^[\p{L}\p{N} _-]{1,100}$") ||
            string.IsNullOrWhiteSpace(request.ItemId) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                request.ItemId,
                @"^[A-Za-z0-9_-]{1,100}$") ||
            string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length > 150 ||
            (request.Description?.Length ?? 0) > 1000 ||
            request.Price is null ||
            !double.IsFinite(request.Price.Value) ||
            request.Price.Value < 0 ||
            request.IsAvailable is null)
        {
            return BadRequest(new
            {
                message = "Invalid menu item details."
            });
        }

        // Map the Gateway DTO to the Functions MenuItem contract.
        var payload = new
        {
            PartitionKey = request.Category.Trim(),
            RowKey = request.ItemId.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description ?? string.Empty,
            Price = request.Price.Value,
            IsAvailable = request.IsAvailable.Value
        };

        try
        {
            var client = _httpClientFactory.CreateClient(
                "FunctionsBackend");

            var json = System.Text.Json.JsonSerializer.Serialize(
                payload);

            using var content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync(
                "api/menu",
                content,
                cancellationToken);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new
                {
                    message = "A menu item with this category and ID already exists."
                });
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.BadRequest)
            {
                return BadRequest(new
                {
                    message = "Menu item was rejected by the backend."
                });
            }

            if (response.StatusCode !=
                System.Net.HttpStatusCode.Created)
            {
                _logger.LogWarning(
                    "Menu creation backend returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                return StatusCode(502, new
                {
                    message = "Menu service is unavailable."
                });
            }

            var responseJson =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            return new ContentResult
            {
                Content = responseJson,
                ContentType = "application/json",
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Menu creation backend connection failed.");

            return StatusCode(502, new
            {
                message = "Menu service is unavailable."
            });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Menu service timed out."
            });
        }
    }
}
