using Azure.Data.Tables;
using CoffeeNChill.Gateway.DTOs.Admin;
using CoffeeNChill.Gateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Administrative reporting endpoints.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController : ControllerBase
{
    private readonly TableClient _users;

    public AdminController(TableClient users)
    {
        _users = users;
    }

    /// <summary>
    /// Returns user totals grouped by role.
    /// Only authenticated administrators can access this report.
    /// </summary>
    [HttpGet("users/summary")]
    [ProducesResponseType(
        typeof(UsersSummaryResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<UsersSummaryResponse>> GetUsersSummary(
        CancellationToken cancellationToken)
    {
        int total = 0;
        int customers = 0;
        int staff = 0;
        int admins = 0;
        int other = 0;

        await foreach (var user in _users.QueryAsync<UserEntity>(
            filter: "PartitionKey eq 'USERS'",
            cancellationToken: cancellationToken))
        {
            total++;

            switch (user.Role)
            {
                case "Customer":
                    customers++;
                    break;

                case "Staff":
                    staff++;
                    break;

                case "Admin":
                    admins++;
                    break;

                default:
                    other++;
                    break;
            }
        }

        return Ok(new UsersSummaryResponse(
            total,
            customers,
            staff,
            admins,
            other));
    }
}
