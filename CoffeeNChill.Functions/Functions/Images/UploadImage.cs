using System.Net;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions.Images
{
    public class UploadImage
    {
        private readonly BlobStorageService _blobService;

        public UploadImage(BlobStorageService blobService)
        {
            _blobService = blobService;
        }

        [Function("UploadImage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "images/{fileName}")] HttpRequestData req,
            string fileName)
        {
            await _blobService.UploadImageAsync(fileName, req.Body);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync($"Successfully uploaded {fileName} to Blob Storage.");
            return response;
        }
    }
}