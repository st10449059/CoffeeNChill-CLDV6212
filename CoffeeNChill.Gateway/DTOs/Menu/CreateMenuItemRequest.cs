using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs.Menu;

/// <summary>
/// Request for creating a CoffeeNChill menu item.
/// </summary>
public sealed class CreateMenuItemRequest
{
    /// <summary>
    /// Menu category, such as Hot Drinks.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Unique menu item identifier.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional item description.
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Item price.
    /// </summary>
    [Required]
    [Range(0.0, double.MaxValue)]
    public double? Price { get; set; }

    /// <summary>
    /// Whether the item is available.
    /// </summary>
    [Required]
    public bool? IsAvailable { get; set; }
}
