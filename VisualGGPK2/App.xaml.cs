using System.IO;
using System.Threading;
using System.Windows;

namespace VisualGGPK2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += OnUnhandledException;
            if (!File.Exists("LibBundle.dll"))
            {
                MessageBox.Show("File not found: LibBundle.dll", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            if (!File.Exists("oo2core_8_win64.dll"))
            {
                MessageBox.Show("File not found: oo2core_8_win64.dll", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            base.OnStartup(e);
        }

        public void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ew = new ErrorWindow();
            var t = new Thread(new ParameterizedThreadStart(ew.ShowError))
            {
                CurrentCulture = new System.Globalization.CultureInfo("en-US"),
                CurrentUICulture = new System.Globalization.CultureInfo("en-US")
            };
            t.Start(e.Exception);
            e.Handled = true;
            if (ew.ShowDialog() != true)
                Shutdown();
        }
    }
}