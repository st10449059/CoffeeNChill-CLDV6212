using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace CoffeeNChill.Tests;

public class PasswordServiceTests
{
    private static PasswordService CreateService() =>
        new(new PasswordHasher<UserEntity>());

    [Fact]
    public void HashPassword_DoesNotStorePlaintext()
    {
        var user = new UserEntity();
        var hash = CreateService().HashPassword(user, "SecurePassword123!");

        Assert.NotEqual("SecurePassword123!", hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void VerifyPassword_RejectsIncorrectPassword()
    {
        var user = new UserEntity();
        var service = CreateService();

        user.PasswordHash = service.HashPassword(user, "SecurePassword123!");

        Assert.Equal(
            PasswordVerificationResult.Failed,
            service.VerifyPassword(user, "IncorrectPassword123!"));
    }

    [Fact]
    public void VerifyPassword_AcceptsCorrectPassword()
    {
        var user = new UserEntity();
        var service = CreateService();

        user.PasswordHash = service.HashPassword(user, "SecurePassword123!");

        Assert.Equal(
            PasswordVerificationResult.Success,
            service.VerifyPassword(user, "SecurePassword123!"));
    }
}
