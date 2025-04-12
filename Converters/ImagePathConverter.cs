using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ManajemenBarang.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null || string.IsNullOrEmpty(value.ToString()))
                {
                    return new BitmapImage(new Uri("/Assets/no-image.png", UriKind.Relative));
                }

                string fileName = value.ToString();
                string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AssetGambar");
                string fullPath = Path.Combine(directoryPath, fileName);

                if (!File.Exists(fullPath))
                {
                    return new BitmapImage(new Uri("/Assets/no-image.png", UriKind.Relative));
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = new Uri(fullPath);
                image.EndInit();
                image.Freeze(); // Improves performance

                return image;
            }
            catch (Exception)
            {
                return new BitmapImage(new Uri("/Assets/no-image.png", UriKind.Relative));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 