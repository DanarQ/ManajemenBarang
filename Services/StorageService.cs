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
        public string ImagePath { get; private set; }
        public string AssetGambarPath { get; private set; }
        
        public StorageService()
        {
            // Create a folder in the AppData local folder to store our data
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directoryPath = Path.Combine(appDataPath, "ManajemenBarang");
            string imagePath = Path.Combine(directoryPath, "Images");
            string assetGambarPath = Path.Combine(directoryPath, "AssetGambar");
            
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }
            
            if (!Directory.Exists(assetGambarPath))
            {
                Directory.CreateDirectory(assetGambarPath);
            }
            
            FilePath = Path.Combine(directoryPath, "items.json");
            ImagePath = imagePath;
            AssetGambarPath = assetGambarPath;
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
            
            // Hapus foto-foto terkait
            var item = items[index];
            if (!string.IsNullOrEmpty(item.FotoPath))
            {
                string fotoPath = Path.Combine(ImagePath, item.FotoPath);
                if (File.Exists(fotoPath)) File.Delete(fotoPath);
            }
            if (!string.IsNullOrEmpty(item.KondisiAwalPath))
            {
                string kondisiAwalPath = Path.Combine(ImagePath, item.KondisiAwalPath);
                if (File.Exists(kondisiAwalPath)) File.Delete(kondisiAwalPath);
            }
            if (!string.IsNullOrEmpty(item.KondisiAkhirPath))
            {
                string kondisiAkhirPath = Path.Combine(ImagePath, item.KondisiAkhirPath);
                if (File.Exists(kondisiAkhirPath)) File.Delete(kondisiAkhirPath);
            }
            
            items.RemoveAt(index);
            await SaveItemsAsync(items);
            return true;
        }

        public string SaveImage(string sourceFilePath, string itemId, string type)
        {
            string fileName = $"{itemId}_{type}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(sourceFilePath)}";
            string destinationPath = Path.Combine(ImagePath, fileName);
            
            File.Copy(sourceFilePath, destinationPath, true);
            return fileName;
        }

        public void DeleteImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            
            string filePath = Path.Combine(ImagePath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
} 