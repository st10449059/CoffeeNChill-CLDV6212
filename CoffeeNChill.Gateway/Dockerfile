# Build the ASP.NET Core API Gateway using .NET 10.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY CoffeeNChill.Gateway/CoffeeNChill.Gateway.csproj CoffeeNChill.Gateway/

RUN dotnet restore CoffeeNChill.Gateway/CoffeeNChill.Gateway.csproj

COPY CoffeeNChill.Gateway/ CoffeeNChill.Gateway/

WORKDIR /src/CoffeeNChill.Gateway

RUN dotnet publish CoffeeNChill.Gateway.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Run the published Gateway using the smaller runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "CoffeeNChill.Gateway.dll"]
