# Stage 1: Build and Restore using .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["CoffeeNChill.Functions.csproj", "./"]
RUN dotnet restore "CoffeeNChill.Functions.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "CoffeeNChill.Functions.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "CoffeeNChill.Functions.csproj" -c Release -o /app/publish

# Stage 3: Final Runtime Image for Azure Functions (.NET 10 Isolated)
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .