using CoffeeNChill.Functions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // This registers your service so the HTTP Functions can use it
        services.AddSingleton<MenuTableService>();
    })
    .Build();

host.Run();