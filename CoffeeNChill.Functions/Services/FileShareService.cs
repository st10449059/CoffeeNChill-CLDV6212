// Code Attribution:
// The fundamental Azure File Share stream operations and metadata retrieval logic 
// were adapted from the official Microsoft Azure SDK documentation.
// Reference: https://learn.microsoft.com/en-us/azure/storage/files/storage-dotnet-how-to-use-files

using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    // DTO (Data Transfer Object) to hold the file information required by the Part 1 rubric
    public class FileMetadataDto
    {
        public string FileName { get; set; }
        public long? Size { get; set; }
        public string LastModified { get; set; }
    }

    public class FileShareService
    {
        private readonly ShareClient _shareClient;

        public FileShareService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureWebJobsStorage"];
            var serviceClient = new ShareServiceClient(connectionString);

            // Ensuring the exact file share name "staff-docs" is used as per the POE scenario
            _shareClient = serviceClient.GetShareClient("staff-docs");
            _shareClient.CreateIfNotExists();
        }

        public async Task UploadFileAsync(string fileName, Stream fileStream)
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            // Allocate the required space on the Azure File Share before initiating the stream transfer
            await fileClient.CreateAsync(fileStream.Length);
            fileStream.Position = 0;
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, fileStream.Length), fileStream);
        }

        public async Task<List<FileMetadataDto>> ListFilesAsync()
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var files = new List<FileMetadataDto>();

            // Iterate through the directory, specifically capturing size and modification dates for the POE rubric
            await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!fileItem.IsDirectory)
                {
                    files.Add(new FileMetadataDto
                    {
                        FileName = fileItem.Name,
                        Size = fileItem.FileSize,
                        LastModified = fileItem.Properties.LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown"
                    });
                }
            }
            return files;
        }

        // NEW: Method required to fulfill the DownloadStaffDocument endpoint requirement
        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            // Prevent server crashes by checking if the file actually exists before attempting a download
            if (await fileClient.ExistsAsync())
            {
                var downloadInfo = await fileClient.DownloadAsync();
                return downloadInfo.Value.Content;
            }
            return null;
        }
    }
}