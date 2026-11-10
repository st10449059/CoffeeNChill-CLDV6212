using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Gateway.Models;

// Represents a registered user stored in Azure Table Storage.
public sealed class UserEntity : ITableEntity
{
    // Table Storage requires a partition key and row key.
    public string PartitionKey { get; set; } = "USERS";

    public string RowKey { get; set; } = string.Empty;

    // Managed by Azure Table Storage.
    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    // Application-specific user information.
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Stores the password hash, never the original password.
    public string PasswordHash { get; set; } = string.Empty;

    // New users receive the Customer role by default.
    public string Role { get; set; } = "Customer";

    public DateTimeOffset CreatedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;
}
