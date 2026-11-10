using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.Services;

/// <summary>
/// Manages CoffeeNChill user accounts in Azure Table Storage.
/// </summary>
public sealed class UsersTableService
{
    private const string UserPartition = "USERS";
    private readonly TableClient _table;

    /// <summary>
    /// Creates a user service using the configured Users table.
    /// </summary>
    public UsersTableService(TableClient table)
    {
        _table = table;
    }

    /// <summary>
    /// Finds an existing user by their email address.
    /// </summary>
    public async Task<UserEntity?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        await _table.CreateIfNotExistsAsync(cancellationToken);

        var rowKey = CreateRowKey(email);

        var response = await _table.GetEntityIfExistsAsync<UserEntity>(
            UserPartition,
            rowKey,
            cancellationToken: cancellationToken);

        return response.HasValue ? response.Value : null;
    }

    /// <summary>
    /// Registers a Customer account without allowing duplicate emails.
    /// The password must already be securely hashed.
    /// </summary>
    public async Task<bool> TryRegisterCustomerAsync(
        string email,
        string fullName,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        await _table.CreateIfNotExistsAsync(cancellationToken);

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = new UserEntity
        {
            PartitionKey = UserPartition,
            RowKey = CreateRowKey(normalizedEmail),
            Email = normalizedEmail,
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            Role = "Customer",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        try
        {
            await _table.AddEntityAsync(user, cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            // A user with this email already exists.
            return false;
        }
    }

    private static string CreateRowKey(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalizedEmail);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}
