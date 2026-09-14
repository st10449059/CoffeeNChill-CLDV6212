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
            // Use the configured Azure Storage connection for table operations.
            var connectionString = configuration["AzureWebJobsStorage"];
            var serviceClient = new TableServiceClient(connectionString);

            // Create the MenuItems table if it does not already exist.
            _tableClient = serviceClient.GetTableClient("MenuItems");
            _tableClient.CreateIfNotExists();
        }

        // Adds a new menu item to the MenuItems table.
        public async Task CreateMenuItemAsync(MenuItem item)
        {
            await _tableClient.AddEntityAsync(item);
        }

        // Retrieves all menu items from the table.
        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            var items = new List<MenuItem>();
            await foreach (var entity in _tableClient.QueryAsync<MenuItem>())
            {
                items.Add(entity);
            }
            return items;
        }

        // Retrieves menu items that share the same category PartitionKey.
        public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(string category)
        {
            var items = new List<MenuItem>();
            // Category is stored as the PartitionKey, allowing related menu items to be queried together.
            await foreach (var entity in _tableClient.QueryAsync<MenuItem>(x => x.PartitionKey == category))
            {
                items.Add(entity);
            }
            return items;
        }

        // Replaces an existing table entity with the updated menu item values.
        public async Task UpdateMenuItemAsync(MenuItem item)
        {
            await _tableClient.UpdateEntityAsync(item, Azure.ETag.All, TableUpdateMode.Replace);
        }

        // Deletes a menu item using its category and item ID.
        public async Task DeleteMenuItemAsync(string category, string id)
        {
            await _tableClient.DeleteEntityAsync(category, id);
        }
    }
}
