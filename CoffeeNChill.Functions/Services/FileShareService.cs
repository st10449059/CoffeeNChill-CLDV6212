using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    public class FileShareService
    {
        private readonly ShareClient _shareClient;

        public FileShareService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureWebJobsStorage"];
            var serviceClient = new ShareServiceClient(connectionString);

            // This creates a file share named "staff-documents"
            _shareClient = serviceClient.GetShareClient("staff-documents");
            _shareClient.CreateIfNotExists();
        }

        // UPLOAD A FILE
        public async Task UploadFileAsync(string fileName, Stream fileStream)
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            // Azure File Shares require you to create the file space first, then upload the data
            await fileClient.CreateAsync(fileStream.Length);
            fileStream.Position = 0; // Reset stream to the beginning
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, fileStream.Length), fileStream);
        }

        // LIST ALL FILES
        public async Task<List<string>> ListFilesAsync()
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileNames = new List<string>();

            // Loop through the directory and grab the names of all the files
            await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!fileItem.IsDirectory)
                {
                    fileNames.Add(fileItem.Name);
                }
            }
            return fileNames;
        }
    }
}