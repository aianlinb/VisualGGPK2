using LibGGPK2;
using LibGGPK2.Records;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace VisualGGPK2
{
    public partial class MainWindow : Window
    {
        public GGPKContainer ggpkContainer;
        public static BitmapFrame IconDir = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("dir")));
        public static BitmapFrame IconFile = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("file")));

        public MainWindow() { InitializeComponent(); }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = "ggpk",
                FileName = "Content.ggpk",
                Filter = "GGPK File|*.ggpk"
            };
            var path = Registry.CurrentUser.OpenSubKey(@"Software\GrindingGearGames\Path of Exile")?.GetValue("InstallLocation") as string;
            if (File.Exists(path + @"Content.ggpk"))
                ofd.InitialDirectory = path.TrimEnd('\\');
            else
            {
                path = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Garena\PoE")?.GetValue("Path") as string;
                if (File.Exists(path + @"Content.ggpk"))
                    ofd.InitialDirectory = path.TrimEnd('\\');
            }

            if (ofd.ShowDialog() == true)
            {
                ggpkContainer = new GGPKContainer(ofd.FileName);
                var root = CreateNode(ggpkContainer.rootDirectory);
                Tree.Items.Add(root);
                root.IsExpanded = true;
            }
            else
                Close();
        }

        public static TreeViewItem CreateNode(RecordTreeNode rtn)
        {
            var tvi = new TreeViewItem
            {
                Tag = rtn
            };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new Image
            {
                Source = rtn is DirectoryRecord ? IconDir : IconFile,
                Width = 20,
                Height = 20
            });
            stack.Children.Add(new TextBlock { Text = rtn.Name, FontSize = 16 });
            tvi.Header = stack;
            if (rtn is DirectoryRecord)
                tvi.Items.Add("Loading . . .");
            return tvi;
        }

        private void OnTreeExpanded(object sender, RoutedEventArgs e)
        {
            var tvi = e.Source as TreeViewItem;
            if (tvi.Items.Count == 1 && tvi.Items[0] is string)
            {
                tvi.Items.Clear();
                foreach (var c in ((DirectoryRecord)tvi.Tag).Children)
                    tvi.Items.Add(CreateNode(c));
            }
        }

        private void OnTreeSelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tvi = e.NewValue as TreeViewItem;
            if (tvi == null)
                return;
            //TODO
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Effects.HasFlag(DragDropEffects.Copy))
                return;
            //TODO
        }
    }
}