using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

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
                
                // Create directories if they don't exist
                if (!Directory.Exists(AssetGambarPath))
                {
                    Directory.CreateDirectory(AssetGambarPath);
                }
                
                if (!Directory.Exists(ImagesPath))
                {
                    Directory.CreateDirectory(ImagesPath);
                }
            }
            catch
            {
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
                return DefaultImage;
            }

            try
            {
                string fileName = value.ToString();
                
                // Try looking in the Images folder first (where SaveImage stores files)
                string imagesFullPath = Path.Combine(ImagesPath, fileName);
                if (File.Exists(imagesFullPath))
                {
                    return LoadImageFromPath(imagesFullPath);
                }
                
                // Then try the AssetGambar folder
                string assetFullPath = Path.Combine(AssetGambarPath, fileName);
                if (File.Exists(assetFullPath))
                {
                    return LoadImageFromPath(assetFullPath);
                }
                
                // If file doesn't exist in either location, log and return default
                System.Diagnostics.Debug.WriteLine($"Image file not found: {fileName}");
                return DefaultImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                return DefaultImage;
            }
        }

        private BitmapImage LoadImageFromPath(string fullPath)
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
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 