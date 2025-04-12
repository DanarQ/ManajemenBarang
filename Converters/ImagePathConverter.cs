using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ManajemenBarang.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        private static readonly BitmapImage DefaultImage;
        private static readonly string ImagePath;

        static ImagePathConverter()
        {
            try
            {
                DefaultImage = new BitmapImage(new Uri("pack://application:,,,/Assets/no-image.png"));
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                ImagePath = Path.Combine(appDataPath, "ManajemenBarang", "Images");
            }
            catch
            {
                DefaultImage = new BitmapImage();
                ImagePath = string.Empty;
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return DefaultImage;
            }

            try
            {
                string fileName = value.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(fileName))
                {
                    return DefaultImage;
                }

                string fullPath = Path.Combine(ImagePath, fileName);
                System.Diagnostics.Debug.WriteLine($"Trying to load image from: {fullPath}");

                if (!File.Exists(fullPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Image file not found: {fullPath}");
                    return DefaultImage;
                }

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                return DefaultImage;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 