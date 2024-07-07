using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace VisualGGPK2
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += OnUnhandledException;
            base.OnStartup(e);
        }

        public static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true;
        }

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