using System;
using System.Globalization;
using System.Net;
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
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            base.OnStartup(e);
        }

        public static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandledException(e.Exception);
            e.Handled = true;
        }

        public static void HandledException(Exception ex) {
            Current.Dispatcher.Invoke(() => {
                var ew = new ErrorWindow();
                ew.ShowError(ex);
                if (ew.ShowDialog() != true)
                    Current.Shutdown();
            });
        }
    }
}