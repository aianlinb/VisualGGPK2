using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

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
            base.OnStartup(e);
        }

        public void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandledException(e.Exception);
            e.Handled = true;
        }

        public static void HandledException(Exception ex) {
            var ew = new ErrorWindow();
            var t = new Thread(new ParameterizedThreadStart(ew.ShowError)) { // Show Error In English
                CurrentCulture = new System.Globalization.CultureInfo("en-US"),
                CurrentUICulture = new System.Globalization.CultureInfo("en-US")
            };
            t.Start(ex);
            if (Current.Dispatcher.Invoke(ew.ShowDialog) != true)
                Current.Shutdown();
        }
    }
}