using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace ManajemenBarang.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        private static readonly BitmapImage DefaultImage;
        private static readonly string AssetGambarPath;
        private static readonly string ImagesPath;

        static ImagePathConverter()
        {
            try
            {
                // Set default image from resources
                DefaultImage = new BitmapImage(new Uri("pack://application:,,,/Assets/no-image.png"));
                
                // Set up paths
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                AssetGambarPath = Path.Combine(appDataPath, "ManajemenBarang", "AssetGambar");
                ImagesPath = Path.Combine(appDataPath, "ManajemenBarang", "Images");
                
                Debug.WriteLine($"ImageConverter initialized with paths:");
                Debug.WriteLine($"AssetGambarPath: {AssetGambarPath}");
                Debug.WriteLine($"ImagesPath: {ImagesPath}");
                
                // Create directories if they don't exist
                if (!Directory.Exists(AssetGambarPath))
                {
                    Directory.CreateDirectory(AssetGambarPath);
                    Debug.WriteLine($"Created AssetGambarPath directory");
                }
                
                if (!Directory.Exists(ImagesPath))
                {
                    Directory.CreateDirectory(ImagesPath);
                    Debug.WriteLine($"Created ImagesPath directory");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ImageConverter initialization: {ex.Message}");
                DefaultImage = new BitmapImage();
                AssetGambarPath = string.Empty;
                ImagesPath = string.Empty;
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Return default image if path is null or empty
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                Debug.WriteLine("ImageConverter: Input is null or empty, returning default image");
                return DefaultImage;
            }

            try
            {
                string fileName = value.ToString();
                Debug.WriteLine($"ImageConverter: Looking for image file: {fileName}");
                
                // Try looking in the Images folder first (where SaveImage stores files)
                string imagesFullPath = Path.Combine(ImagesPath, fileName);
                Debug.WriteLine($"ImageConverter: Checking path: {imagesFullPath}");
                
                if (File.Exists(imagesFullPath))
                {
                    Debug.WriteLine($"ImageConverter: File found in ImagesPath: {imagesFullPath}");
                    return LoadImageFromPath(imagesFullPath);
                }
                
                // Then try the AssetGambar folder
                string assetFullPath = Path.Combine(AssetGambarPath, fileName);
                Debug.WriteLine($"ImageConverter: Checking path: {assetFullPath}");
                
                if (File.Exists(assetFullPath))
                {
                    Debug.WriteLine($"ImageConverter: File found in AssetGambarPath: {assetFullPath}");
                    return LoadImageFromPath(assetFullPath);
                }
                
                // If file doesn't exist in either location, log and return default
                Debug.WriteLine($"ImageConverter: Image file not found in either location: {fileName}");
                return DefaultImage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ImageConverter: Error loading image: {ex.Message}");
                return DefaultImage;
            }
        }

        private BitmapImage LoadImageFromPath(string fullPath)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.DecodePixelWidth = 80;
                image.DecodePixelHeight = 80;
                image.UriSource = new Uri(fullPath);
                image.EndInit();
                if (image.CanFreeze)
                {
                    image.Freeze();
                }
                Debug.WriteLine($"ImageConverter: Successfully loaded image from: {fullPath}");
                return image;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ImageConverter: Failed to load image from {fullPath}: {ex.Message}");
                return DefaultImage;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 