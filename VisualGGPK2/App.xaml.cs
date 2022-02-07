using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace VisualGGPK2
{
    public partial class App : Application
    {
#if !DEBUG
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += OnUnhandledException;
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            base.OnStartup(e);
        }

        public static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true;
        }
#endif
        public static void HandleException(Exception ex) {
            Current.Dispatcher.Invoke(() => {
                var ew = new ErrorWindow();
                ew.ShowError(ex);
                if (ew.ShowDialog() != true)
                    Current.Shutdown();
            });
        }
    }
}