﻿using System.Threading;

namespace VisualGGPK2
{
    public partial class BackgroundDialog : System.Windows.Window
    {
        private int progress = 0;
        public string ProgressText;
        public BackgroundDialog()
        {
            InitializeComponent();
        }

        public virtual void NextProgress()
        {
            Interlocked.Increment(ref progress);
            Dispatcher.BeginInvoke(() => { MessageTextBlock.Text = string.Format(ProgressText, progress); });
        }

        protected virtual void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        public virtual new void Close()
        {
            try {
                Closing -= OnClosing;
                Dispatcher.Invoke(base.Close);
            } catch { }
        }
    }
}