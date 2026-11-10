using CoffeeNChill.Gateway.Models;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.Gateway.Services;

/// <summary>
/// Handles secure password hashing and verification.
/// </summary>
public sealed class PasswordService
{
    private readonly IPasswordHasher<UserEntity> _hasher;

    public PasswordService(IPasswordHasher<UserEntity> hasher)
    {
        _hasher = hasher;
    }

    /// <summary>
    /// Creates a secure hash for a user's password.
    /// </summary>
    public string HashPassword(UserEntity user, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "Password cannot be empty.",
                nameof(password));
        }

        return _hasher.HashPassword(user, password);
    }

    /// <summary>
    /// Checks a supplied password against the stored hash.
    /// </summary>
    public PasswordVerificationResult VerifyPassword(
        UserEntity user,
        string password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash) ||
            string.IsNullOrEmpty(password))
        {
            return PasswordVerificationResult.Failed;
        }

        return _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);
    }
}
