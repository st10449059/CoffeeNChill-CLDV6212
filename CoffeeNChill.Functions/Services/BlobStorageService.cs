using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureWebJobsStorage"];
            var serviceClient = new BlobServiceClient(connectionString);

            // This ensures a container named "menu-images" exists
            _containerClient = serviceClient.GetBlobContainerClient("menu-images");
            _containerClient.CreateIfNotExists();
        }

        public async Task UploadImageAsync(string blobName, Stream content)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, overwrite: true);
        }
    }
}