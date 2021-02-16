using System;
using System.Diagnostics;
using System.Windows;
namespace VisualGGPK2
{
    public partial class ErrorWindow : Window
    {
        public ErrorWindow()
        {
            InitializeComponent();
        }

        public virtual void ShowError(Exception e)
        {
            ErrorBox.Text = e.ToString();
            ButtonCopy.IsEnabled = true;
            ButtonResume.IsEnabled = true;
            ButtonStop.IsEnabled = true;
        }

        protected virtual void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        protected virtual void OnCopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ErrorBox.Text);
        }

        protected virtual void OnGitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/aianlinb/LibGGPK2");
        }

        protected virtual void OnResumeClick(object sender, RoutedEventArgs e)
        {
            Closing -= OnClosing;
            DialogResult = true;
            Close(); // This line will never reached
        }

        protected virtual void OnStopClick(object sender, RoutedEventArgs e)
        {
            Closing -= OnClosing;
            DialogResult = false;
            Close(); // This line will never reached
        }
    }
}