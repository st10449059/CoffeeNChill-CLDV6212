using Azure;
using CoffeeNChill.Gateway.DTOs.Auth;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Handles CoffeeNChill account registration and authentication.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UsersTableService _users;
    private readonly PasswordService _passwords;
    private readonly JwtTokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Creates the authentication controller.
    /// </summary>
    public AuthController(
        UsersTableService users,
        PasswordService passwords,
        JwtTokenService tokens,
        ILogger<AuthController> logger)
    {
        _users = users;
        _passwords = passwords;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new Customer account.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Email, full name and password are required."
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var fullName = request.FullName.Trim();

        // Never accept a role from public registration.
        var user = new UserEntity
        {
            Email = email,
            FullName = fullName,
            Role = "Customer"
        };

        // The original password is never saved to Table Storage.
        var passwordHash = _passwords.HashPassword(
            user,
            request.Password);

        try
        {
            var created = await _users.TryRegisterCustomerAsync(
                email,
                fullName,
                passwordHash,
                cancellationToken);

            if (!created)
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Customer account registered successfully.",
                email,
                role = "Customer"
            });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "User registration storage operation failed.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "User registration is temporarily unavailable."
                });
        }
    }

    /// <summary>
    /// Authenticates a user and returns a signed JWT access token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        try
        {
            var user = await _users.GetByEmailAsync(
                email,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var result = _passwords.VerifyPassword(
                user,
                request.Password);

            if (result ==
                Microsoft.AspNetCore.Identity
                    .PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Generate a token using the role stored in Table Storage.
            var token = _tokens.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful.",
                accessToken = token.AccessToken,
                tokenType = "Bearer",
                expiresAtUtc = token.ExpiresAtUtc,
                role = user.Role,
                fullName = user.FullName
            });
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "User login storage operation failed.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "Authentication is temporarily unavailable."
                });
        }
    }
}
