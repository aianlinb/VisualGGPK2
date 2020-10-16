using LibGGPK2;
using LibGGPK2.Records;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VisualGGPK2
{
    public partial class MainWindow : Window
    {
        public GGPKContainer ggpkContainer;
        /// <summary>
        /// Icon of directory on TreeView
        /// </summary>
        public static BitmapFrame IconDir = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("dir")));
        /// <summary>
        /// Icon of file on TreeView
        /// </summary>
        public static BitmapFrame IconFile = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("file")));
        public static ContextMenu MenuDir = new ContextMenu();
        public static ContextMenu TreeMenu = new ContextMenu();

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
            if (path != null && File.Exists(path + @"\Content.ggpk"))
                ofd.InitialDirectory = path.TrimEnd('\\'); // Get POE path
            else
            {
                path = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Garena\PoE")?.GetValue("Path") as string;
                if (path != null && File.Exists(path + @"\Content.ggpk"))
                    ofd.InitialDirectory = path.TrimEnd('\\'); // Get Garena POE path
            }

            if (ofd.ShowDialog() == true) // Select Content.ggpk
            {
                var mi = new MenuItem() { Header = "Export" }; // Initial ContextMenu
                mi.Click += OnExportClicked;
                TreeMenu.Items.Add(mi);
                mi = new MenuItem() { Header = "Replace"};
                mi.Click += OnReplaceClicked;
                TreeMenu.Items.Add(mi);

                ggpkContainer = new GGPKContainer(ofd.FileName); // Initial GGPK
                var root = CreateNode(ggpkContainer.rootDirectory);
                Tree.Items.Add(root); // Initial TreeView
                root.IsExpanded = true;
            }
            else
                Close();
        }

        /// <summary>
        /// Create a element of the TreeView
        /// </summary>
        public TreeViewItem CreateNode(RecordTreeNode rtn)
        {
            var tvi = new TreeViewItem { Tag = rtn };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new Image // Icon
            {
                Source = rtn is DirectoryRecord ? IconDir : IconFile,
                Width = 20,
                Height = 20
            });
            stack.Children.Add(new TextBlock { Text = rtn.Name, FontSize = 16 }); // File/Directory Name
            //stack.MouseDown += OnTreeMouseDown;
            tvi.Header = stack;
            if (rtn is DirectoryRecord)
                tvi.Items.Add("Loading . . ."); // Add expand button
            tvi.ContextMenu = TreeMenu;
            return tvi;
        }

        private void OnTreeExpanded(object sender, RoutedEventArgs e)
        {
            var tvi = e.Source as TreeViewItem;
            if (tvi.Items.Count == 1 && tvi.Items[0] is string) // Haven't been expanded yet
            {
                tvi.Items.Clear();
                foreach (var c in ((DirectoryRecord)tvi.Tag).Children)
                    tvi.Items.Add(CreateNode(c));
            }
        }

        private void OnTreeSelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem tvi)
            {
                ImageView.Visibility = Visibility.Collapsed;
                TextView.Visibility = Visibility.Collapsed;
                //OGGView.Visibility = Visibility.Collapsed;
                //BK2View.Visibility = Visibility.Collapsed;
                //BANKView.Visibility = Visibility.Collapsed;
                if (tvi.Tag is RecordTreeNode rtn)
                {
                    TextBoxOffset.Text = rtn.RecordBegin.ToString("X");
                    TextBoxSize.Text = rtn.Length.ToString();
                    TextBoxHash.Text = BitConverter.ToString(rtn.Hash).Replace("-","");
                    if (tvi.Tag is FileRecord fr)
                    {
                        TextBoxSize.Text = fr.DataLength.ToString(); // FileSize
                        switch (fr.DataFormat)
                        {
                            case FileRecord.DataFormats.Image:
                                ImageView.Source = BitmapFrame.Create(new MemoryStream(fr.ReadFileContent()));
                                ImageView.Visibility = Visibility.Visible;
                                break;
                            case FileRecord.DataFormats.Ascii:
                                TextView.Text = Encoding.UTF8.GetString(fr.ReadFileContent());
                                TextView.Visibility = Visibility.Visible;
                                break;
                            case FileRecord.DataFormats.Unicode:
                                TextView.Text = Encoding.Unicode.GetString(fr.ReadFileContent());
                                TextView.Visibility = Visibility.Visible;
                                break;
                            case FileRecord.DataFormats.OGG:
                                //TODO
                                //OGGView.Visibility = Visibility.Visible;
                                break;
                            case FileRecord.DataFormats.Dat:
                                //TODO
                                //DatView.Visibility = Visibility.Visible;
                                break;
                            case FileRecord.DataFormats.TextureDds:
                                //TODO
                                //ImageView.Source = ;
                                //ImageView.Visibility = Visibility.Visible;
                                break;
                            case FileRecord.DataFormats.BK2:
                                //TODO
                                //BK2View.Visibility = Visibility.Visible;
                                break;
                            case FileRecord.DataFormats.BANK:
                                //TODO
                                //BANKView.Visibility = Visibility.Visible;
                                break;
                        }
                    }
                }
            }
        }

        private void OnTreePreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var ui = e.Source as DependencyObject;
            if (ui == null || ui is TreeView) return;
            while (!(ui is TreeViewItem))
                ui = VisualTreeHelper.GetParent(ui);
            var tvi = ui as TreeViewItem;
            if (e.ChangedButton != MouseButton.Left) // Select when clicked
                tvi.IsSelected = true;
            else if (tvi.Tag is DirectoryRecord && !(e.Source is TreeViewItem)) // Expand when left clicked
                tvi.IsExpanded = true;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) // Drag File/Folder
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Effects.HasFlag(DragDropEffects.Copy)) return; // Drop File/Folder
            var dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropped.Length != 1 || Path.GetFileName(dropped[0]) != "ROOT")
            {
                MessageBox.Show("The dropped directory must be \"ROOT\"", "Replace Faild",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MessageBox.Show("Replace files?", "Replace Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                var count = Directory.GetFiles(dropped[0], "*", SearchOption.AllDirectories).Length;
                var bkg = new BackgroundDialog { ProgressText = "Replaced {0}/" + count.ToString() + " Files . . ." };
                GGPKContainer.ReplaceAsync(ggpkContainer.rootDirectory, dropped[0], bkg.NextProgress).ContinueWith(new Action<Task<int>>((tsk) => {
                    MessageBox.Show("Relaced " + tsk.Result.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                    bkg.Close();
                }));
                bkg.ShowDialog();
            }
        }

        private void OnExportClicked(object sender, RoutedEventArgs e)
        {
            if ((Tree.SelectedItem as TreeViewItem)?.Tag is RecordTreeNode rtn)
            {
                var sfd = new SaveFileDialog();
                if (rtn is FileRecord fr)
                {
                    sfd.FileName = rtn.Name;
                    if (sfd.ShowDialog() == true)
                    {
                        File.WriteAllBytes(sfd.FileName, fr.ReadFileContent());
                        MessageBox.Show("Exported " + rtn.GetPath(), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    sfd.FileName = rtn.Name + ".dir";
                    if (sfd.ShowDialog() == true)
                    {
                        var bkg = new BackgroundDialog { ProgressText = "Exported {0} Files . . ." };
                        GGPKContainer.ExportAsync(rtn, Directory.GetParent(sfd.FileName) + "\\" + rtn.Name, bkg.NextProgress).ContinueWith(new Action<Task<int>>((tsk) => {
                            MessageBox.Show("Exported " + tsk.Result.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                            bkg.Close();
                        }));
                        bkg.ShowDialog();
                    }
                }
            }
        }

        private void OnReplaceClicked(object sender, RoutedEventArgs e)
        {
            if ((Tree.SelectedItem as TreeViewItem)?.Tag is RecordTreeNode rtn)
            {
                if (rtn is FileRecord fr)
                {
                    var ofd = new OpenFileDialog { FileName = rtn.Name };
                    if (ofd.ShowDialog() == true)
                    {
                        fr.ReplaceContent(File.ReadAllBytes(ofd.FileName));
                        MessageBox.Show("Replaced " + rtn.GetPath(), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    var ofd = new OpenFolderDialog();
                    if (ofd.ShowDialog() == true)
                    {
                        var count = Directory.GetFiles(ofd.DirectoryPath, "*", SearchOption.AllDirectories).Length;
                        var bkg = new BackgroundDialog { ProgressText = "Replaced {0}/" + count.ToString() + " Files . . ." };
                        GGPKContainer.ReplaceAsync(rtn, ofd.DirectoryPath, bkg.NextProgress).ContinueWith(new Action<Task<int>>((tsk) => {
                            MessageBox.Show("Replaced " + tsk.Result.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                            bkg.Close();
                        }));
                        bkg.ShowDialog();
                    }
                }
            }
        }
    }
}