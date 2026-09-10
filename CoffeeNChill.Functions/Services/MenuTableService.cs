using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    public class MenuTableService
    {
        private readonly TableClient _tableClient;

        public MenuTableService(IConfiguration configuration)
        {
            // Connect to Azure Storage using the connection string from local.settings.json
            var connectionString = configuration["AzureWebJobsStorage"];
            var serviceClient = new TableServiceClient(connectionString);

            // Create or connect to the "MenuItems" table
            _tableClient = serviceClient.GetTableClient("MenuItems");
            _tableClient.CreateIfNotExists();
        }

        // CREATE
        public async Task CreateMenuItemAsync(MenuItem item)
        {
            await _tableClient.AddEntityAsync(item);
        }

        // READ ALL
        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            var items = new List<MenuItem>();
            await foreach (var entity in _tableClient.QueryAsync<MenuItem>())
            {
                items.Add(entity);
            }
            return items;
        }

        // READ BY CATEGORY (Filters by PartitionKey)
        public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(string category)
        {
            var items = new List<MenuItem>();
            // Azure Table Storage uses the PartitionKey to group items (e.g., "Hot Drinks")
            await foreach (var entity in _tableClient.QueryAsync<MenuItem>(x => x.PartitionKey == category))
            {
                items.Add(entity);
            }
            return items;
        }

        // UPDATE
        public async Task UpdateMenuItemAsync(MenuItem item)
        {
            await _tableClient.UpdateEntityAsync(item, Azure.ETag.All, TableUpdateMode.Replace);
        }

        // DELETE
        public async Task DeleteMenuItemAsync(string category, string id)
        {
            await _tableClient.DeleteEntityAsync(category, id);
        }
    }
}