using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ManajemenBarang.Models;

namespace ManajemenBarang.Services
{
    public class StorageService
    {
        public string FilePath { get; private set; }
        
        public StorageService()
        {
            // Create a folder in the AppData local folder to store our data
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directoryPath = Path.Combine(appDataPath, "ManajemenBarang");
            
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            FilePath = Path.Combine(directoryPath, "items.json");
        }
        
        public async Task<List<Item>> GetItemsAsync()
        {
            if (!File.Exists(FilePath))
            {
                return new List<Item>();
            }
            
            string json = await File.ReadAllTextAsync(FilePath);
            
            if (string.IsNullOrEmpty(json))
            {
                return new List<Item>();
            }
            
            return JsonSerializer.Deserialize<List<Item>>(json) ?? new List<Item>();
        }
        
        public async Task SaveItemsAsync(List<Item> items)
        {
            string json = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(FilePath, json);
        }
        
        public async Task<bool> AddItemAsync(Item item)
        {
            var items = await GetItemsAsync();
            
            // Check if ID already exists
            if (items.Any(i => i.IdBarang == item.IdBarang))
            {
                return false;
            }
            
            items.Add(item);
            await SaveItemsAsync(items);
            return true;
        }
        
        public async Task<bool> UpdateItemAsync(Item item)
        {
            var items = await GetItemsAsync();
            
            int index = items.FindIndex(i => i.IdBarang == item.IdBarang);
            if (index == -1)
            {
                return false;
            }
            
            items[index] = item;
            await SaveItemsAsync(items);
            return true;
        }
        
        public async Task<bool> DeleteItemAsync(string itemId)
        {
            var items = await GetItemsAsync();
            
            int index = items.FindIndex(i => i.IdBarang == itemId);
            if (index == -1)
            {
                return false;
            }
            
            items.RemoveAt(index);
            await SaveItemsAsync(items);
            return true;
        }
    }
} 