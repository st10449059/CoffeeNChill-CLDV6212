// Code Attribution:
// The fundamental Azure File Share stream operations and metadata retrieval logic 
// were adapted from the official Microsoft Azure SDK documentation.
// Reference: https://learn.microsoft.com/en-us/azure/storage/files/storage-dotnet-how-to-use-files

using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    public class FileMetadataDto
    {
        public string FileName { get; set; }
        public long? Size { get; set; }
        public string LastModified { get; set; }
    }

    public class FileShareService
    {
        private readonly ShareServiceClient _serviceClient;
        private readonly string _shareName = "staff-docs";

        public FileShareService(IConfiguration configuration)
        {
            // Read connection string securely from environment variables / local.settings.json
            string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                      ?? configuration["AzureStorageConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("AzureStorageConnectionString is not configured.");
            }

            _serviceClient = new ShareServiceClient(connectionString);
        }

        public async Task UploadFileAsync(string fileName, Stream fileStream)
        {
            var shareClient = _serviceClient.GetShareClient(_shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            // MUST use MemoryStream to prevent Kestrel stream exceptions
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            await fileClient.CreateAsync(memoryStream.Length);
            memoryStream.Position = 0;
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, memoryStream.Length), memoryStream);
        }

        public async Task<List<FileMetadataDto>> ListFilesAsync()
        {
            var shareClient = _serviceClient.GetShareClient(_shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetRootDirectoryClient();
            var files = new List<FileMetadataDto>();

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

        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            var shareClient = _serviceClient.GetShareClient(_shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            if (await fileClient.ExistsAsync())
            {
                var downloadInfo = await fileClient.DownloadAsync();
                return downloadInfo.Value.Content;
            }
            return null;
        }
    }
}