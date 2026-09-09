using CoffeeNChill.Functions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<MenuTableService>();
        services.AddSingleton<FileShareService>();
        services.AddSingleton<BlobStorageService>();  // <-- Your new Blob service
        services.AddSingleton<QueueStorageService>(); // <-- Your new Queue service
    })
    .Build();

host.Run();