namespace CoffeeNChill.Gateway.DTOs.Admin;

/// <summary>
/// Aggregate user counts without exposing personal information.
/// </summary>
public sealed record UsersSummaryResponse(
    int Total,
    int Customers,
    int Staff,
    int Admins,
    int Other);
