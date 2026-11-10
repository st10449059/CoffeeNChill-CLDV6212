using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs.Menu;

/// <summary>
/// Validated request for updating an existing menu item.
/// The category and item ID are supplied through the URL.
/// </summary>
public sealed class UpdateMenuItemRequest
{
    /// <summary>
    /// Display name of the menu item.
    /// </summary>
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the menu item.
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Price must be supplied and cannot be negative.
    /// </summary>
    [Required]
    [Range(0.0, double.MaxValue)]
    public double? Price { get; set; }

    /// <summary>
    /// Whether customers can currently order the item.
    /// </summary>
    [Required]
    public bool? IsAvailable { get; set; }
}
