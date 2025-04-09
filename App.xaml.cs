using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace ManajemenBarang;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Set the culture to Indonesian (Indonesia)
        var culture = new CultureInfo("id-ID");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Ensure the culture is applied to WPF framework elements
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        base.OnStartup(e);
    }
}

