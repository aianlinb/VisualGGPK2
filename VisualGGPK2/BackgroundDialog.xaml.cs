namespace VisualGGPK2
{
    /// <summary>
    /// When background working, show this dialog.
    /// </summary>
    public partial class BackgroundDialog : System.Windows.Window
    {
        public int progress = 0;
        public string ProgressText;
        public BackgroundDialog()
        {
            InitializeComponent();
        }

        public void NextProgress()
        {
            progress++;
            Dispatcher.BeginInvoke((System.Action)(() => { MessageTextBlock.Text = string.Format(ProgressText, progress); }));
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        public new void Close()
        {
            Closing -= OnClosing;
            Dispatcher.Invoke(base.Close);
        }
    }
}