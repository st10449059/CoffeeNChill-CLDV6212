var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Generate interactive Swagger documentation.
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{builder.Environment.ApplicationName}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.AddSecurityDefinition(
    "bearer",
    new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT access token."
    });

options.OperationFilter<
    CoffeeNChill.Gateway.Swagger.JwtSecurityOperationFilter>();

    options.IncludeXmlComments(xmlPath);
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


// Register the Azure Table Storage Users table.
// Connection credentials are supplied through private configuration.
builder.Services.AddSingleton<Azure.Data.Tables.TableClient>(
    serviceProvider =>
    {
        var configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        var connectionString =
            configuration.GetConnectionString("UsersStorage");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:UsersStorage configuration.");
        }

        return new Azure.Data.Tables.TableClient(
            connectionString,
            "Users");
    });

// Allow controllers to request UsersTableService through DI.
builder.Services.AddScoped<
    CoffeeNChill.Gateway.Services.UsersTableService>();

// Register ASP.NET Core Identity password hashing.
builder.Services.AddScoped<
    Microsoft.AspNetCore.Identity.IPasswordHasher<
        CoffeeNChill.Gateway.Models.UserEntity>,
    Microsoft.AspNetCore.Identity.PasswordHasher<
        CoffeeNChill.Gateway.Models.UserEntity>>();

// Register our application's password service.
builder.Services.AddScoped<
    CoffeeNChill.Gateway.Services.PasswordService>();

// JWT authentication validates signed access tokens.
builder.Services.AddAuthentication(
    Microsoft.AspNetCore.Authentication.JwtBearer
        .JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var signingKey = builder.Configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "JWT configuration is missing.");
        }

        byte[] keyBytes;

        try
        {
            keyBytes = Convert.FromBase64String(signingKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "JWT signing key must be valid Base64.", ex);
        }

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");
        }

        options.TokenValidationParameters =
            new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        keyBytes),

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ClockSkew = TimeSpan.FromMinutes(1),

                NameClaimType = "name",
                RoleClaimType = "role"
            };
    });

// Define policies for protected Staff and Admin endpoints.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "StaffOnly",
        policy => policy.RequireRole("Staff", "Admin"));

    options.AddPolicy(
        "AdminOnly",
        policy => policy.RequireRole("Admin"));
});

// Register the JWT token-generation service.
builder.Services.AddSingleton<
    CoffeeNChill.Gateway.Services.JwtTokenService>();

// Configure HTTP communication with the internal Functions backend.
// The backend address comes from application configuration.
builder.Services.AddHttpClient("FunctionsBackend", (provider, client) =>
{
    var configuration =
        provider.GetRequiredService<IConfiguration>();

    var baseUrl = configuration["FunctionsBackend:BaseUrl"];

    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp &&
         uri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException(
            "FunctionsBackend:BaseUrl must be a valid HTTP or HTTPS URL.");
    }

    if (uri.AbsolutePath != "/" ||
        !string.IsNullOrEmpty(uri.Query) ||
        !string.IsNullOrEmpty(uri.Fragment))
    {
        throw new InvalidOperationException(
            "FunctionsBackend:BaseUrl must point to the backend root.");
    }

    client.BaseAddress = uri;
    client.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

app.UseMiddleware<CoffeeNChill.Gateway.Middleware.GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
