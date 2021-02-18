using System.Windows;

namespace VisualGGPK2 {
	public partial class VersionSelector : Window {
		public VersionSelector() {
			InitializeComponent();
		}

		private void International_Click(object sender, RoutedEventArgs e) {
			MainWindow.SelectedVersion = 1;
			DialogResult = true;
		}

		private void Garena_Click(object sender, RoutedEventArgs e) {
			MainWindow.SelectedVersion = 2;
			DialogResult = true;
		}

		private void Tencent_Click(object sender, RoutedEventArgs e) {
			MainWindow.SelectedVersion = 3;
			DialogResult = true;
		}
	}
}
