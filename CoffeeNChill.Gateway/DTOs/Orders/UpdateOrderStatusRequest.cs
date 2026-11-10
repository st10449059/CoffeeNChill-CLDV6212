using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs.Orders;

/// <summary>
/// Request to advance an existing CoffeeNChill order.
/// </summary>
public sealed class UpdateOrderStatusRequest
{
    /// <summary>
    /// UTC order date in yyyy-MM-dd format.
    /// </summary>
    [Required]
    public string OrderDate { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the existing order.
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 1)]
    [RegularExpression(@"\A[A-Za-z0-9_-]+\z")]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Requested order-processing status.
    /// </summary>
    [Required]
    [RegularExpression(
        @"\A(Received|Preparing|Ready|Collected)\z")]
    public string Status { get; set; } = string.Empty;
}
