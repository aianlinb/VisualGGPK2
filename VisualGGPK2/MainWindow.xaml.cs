using LibBundle;
using LibDat2;
using LibGGPK2;
using LibGGPK2.Records;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        public static readonly BitmapFrame IconDir = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("dir")));
        /// <summary>
        /// Icon of file on TreeView
        /// </summary>
        public static readonly BitmapFrame IconFile = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("file")));
        public static readonly ContextMenu TreeMenu = new ContextMenu();
        public static readonly Encoding Unicode = new UnicodeEncoding(false, true);
        public static readonly Encoding UTF8 = new UTF8Encoding(false, false);
        public WebClient Web;
        public readonly bool BundleMode = false;
        public readonly bool SteamMode = false;
        protected string FilePath;
        internal static byte SelectedVersion;

        public MainWindow() {
            var args = Environment.GetCommandLineArgs();
            for (var i = 1; i < args.Length; i++)
                switch (args[i].ToLower()) {
                    case "-bundle":
                        BundleMode = true;
                        break;
                    case "-steam":
                        SteamMode = true;
                        break;
                    default:
                        if (FilePath == null && File.Exists(args[i]))
                            FilePath = args[i];
                        break;
                }
            if (BundleMode && SteamMode) {
                MessageBox.Show("BundleMode and SteamMode cannot be both true", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            InitializeComponent();
        }

        private readonly List<int> toMark = new();
        private async void OnLoaded(object sender, RoutedEventArgs e) {
            if (FilePath == null) {
                var ofd = new OpenFileDialog {
                    DefaultExt = "ggpk",
                    FileName = SteamMode ? "_.index.bin" : "Content.ggpk",
                    Filter = SteamMode ? "Index Bundle File|*.index.bin" : "GGPK File|*.ggpk"
                };

                var setting = Properties.Settings.Default;
                if (setting.GGPKPath == "") {
                    string path;
                    path = Registry.CurrentUser.OpenSubKey(@"Software\GrindingGearGames\Path of Exile")?.GetValue("InstallLocation") as string;
                    if (path != null && File.Exists(path + @"\Content.ggpk")) // Get POE path
                        ofd.InitialDirectory = path.TrimEnd('\\');
                } else
                    ofd.InitialDirectory = setting.GGPKPath;

                if (ofd.ShowDialog() == true) {
                    setting.GGPKPath = Directory.GetParent(FilePath = ofd.FileName).FullName;
                    setting.Save();
                } else {
                    Close();
                    return;
                }
            }

            await Task.Run(() => ggpkContainer = new GGPKContainer(FilePath, BundleMode, SteamMode)); // Initial GGPK

            var mi = new MenuItem { Header = "Export" }; // Initial ContextMenu
            mi.Click += OnExportClicked;
            TreeMenu.Items.Add(mi);
            mi = new MenuItem { Header = "Replace" };
            mi.Click += OnReplaceClicked;
            TreeMenu.Items.Add(mi);
            mi = new MenuItem { Header = "Recovery" };
            mi.Click += OnRecoveryClicked;
            TreeMenu.Items.Add(mi);

            var imageMenu = new ContextMenu();
            mi = new MenuItem { Header = "Save as png" };
            mi.Click += OnSavePngClicked;
            imageMenu.Items.Add(mi);
            ImageView.ContextMenu = imageMenu;

            var root = CreateNode(ggpkContainer.rootDirectory);
            Tree.Items.Add(root); // Initial TreeView
            root.IsExpanded = true;

            RegexCheckBox.IsEnabled = true;
            FilterButton.IsEnabled = true;

            DatPointedTable.CellStyle = new Style(typeof(DataGridCell));
            DatPointedTable.CellStyle.Setters.Add(new EventSetter(LoadedEvent, new RoutedEventHandler((s, e) => {
                var dc = (DataGridCell)s;
                var border = (Border)VisualTreeHelper.GetChild(dc, 0);
                var row = DataGridRow.GetRowContainingElement(dc).GetIndex();
                var col = dc.Column.DisplayIndex;
                if (col == 0 && toMark.Contains(row) || col == 2 && toMark.Contains(row + 1)) {
                    border.Background = Brushes.Red;
                    border.BorderThickness = new Thickness(0);
                }
            })));

            TextView.AppendText("\r\n\r\nDone!\r\n");
        }

        /// <summary>
        /// Create a element of the TreeView
        /// </summary>
        public static TreeViewItem CreateNode(RecordTreeNode rtn)
        {
            var tvi = new TreeViewItem { Tag = rtn, Margin = new Thickness(0,1,0,1) };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            if (rtn is IFileRecord)
                stack.Children.Add(new Image // Icon
                {
                    Source = IconFile,
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(0,0,2,0)
                });
            else
                stack.Children.Add(new Image // Icon
                {
                    Source = IconDir,
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 2, 0)
                });
            stack.Children.Add(new TextBlock { Text = rtn.Name, FontSize = 16 }); // File/Directory Name
            tvi.Header = stack;
            if (!(rtn is IFileRecord))
                tvi.Items.Add("Loading . . ."); // Add expand button
            tvi.ContextMenu = TreeMenu;
            return tvi;
        }

        /// <summary>
        /// Directory expanded event
        /// </summary>
        private void OnTreeExpanded(object sender, RoutedEventArgs e)
        {
            var tvi = e.Source as TreeViewItem;
            if (tvi.Items.Count == 1 && tvi.Items[0] is string) // Haven't been expanded yet
            {
                tvi.Items.Clear();
                foreach (var c in ((RecordTreeNode)tvi.Tag).Children)
                    tvi.Items.Add(CreateNode(c));
            }
        }

        /// <summary>
        /// TreeView selected changed event
        /// </summary>
        private void OnTreeSelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Tree.SelectedItem is TreeViewItem tvi)
            {
                ImageView.Visibility = Visibility.Hidden;
                TextView.Visibility = Visibility.Hidden;
                //OGGView.Visibility = Visibility.Hidden;
                DatView.Visibility = Visibility.Hidden;
                //BK2View.Visibility = Visibility.Hidden;
                //BANKView.Visibility = Visibility.Hidden;
                ButtonSave.Visibility = Visibility.Hidden;
                if (tvi.Tag is RecordTreeNode rtn)
                {
                    TextBoxOffset.Text = rtn.Offset.ToString("X");
                    TextBoxSize.Text = rtn.Length.ToString();
                    TextBoxHash.Text = rtn is DirectoryRecord || rtn is FileRecord ? BitConverter.ToString(rtn.Hash).Replace("-", "") : rtn is BundleFileNode bf ? bf.Hash.ToString("X") : ((BundleDirectoryNode)rtn).Hash.ToString("X");
                    TextBoxBundle.Text = "";
                    if (rtn is IFileRecord f)
                    {
                        if (f is FileRecord fr) TextBoxSize.Text = fr.DataLength.ToString();
                        else TextBoxBundle.Text = ((BundleFileNode)f).BundleFileRecord.bundleRecord.Name;
                        switch (f.DataFormat)
                        {
                            case IFileRecord.DataFormats.Image:
                                ImageView.Source = BitmapFrame.Create(new MemoryStream(f.ReadFileContent(ggpkContainer.fileStream)));
                                ImageView.Visibility = Visibility.Visible;
                                break;
                            case IFileRecord.DataFormats.Ascii:
                                TextView.IsReadOnly = false;
                                TextView.Text = UTF8.GetString(f.ReadFileContent(ggpkContainer.fileStream));
                                TextView.Visibility = Visibility.Visible;
                                ButtonSave.Visibility = Visibility.Visible;
                                break;
                            case IFileRecord.DataFormats.Unicode:
                                if (rtn.Parent.Name == "Bundles" || rtn.Name == "minimap_colours.txt")
                                    goto case IFileRecord.DataFormats.Ascii;
                                TextView.IsReadOnly = false;
                                TextView.Text = Unicode.GetString(f.ReadFileContent(ggpkContainer.fileStream)).TrimStart('\xFEFF');
                                TextView.Visibility = Visibility.Visible;
                                ButtonSave.Visibility = Visibility.Visible;
                                break;
                            case IFileRecord.DataFormats.OGG:
                                //TODO
                                //OGGView.Visibility = Visibility.Visible;
                                break;
                            case IFileRecord.DataFormats.Dat:
                                try {
                                    var dat = new DatContainer(f.ReadFileContent(ggpkContainer.fileStream), rtn.Name);
                                    ShowDatFile(dat);
                                    DatView.Visibility = Visibility.Visible;
                                } catch (Exception ex) {
                                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                break;
                            case IFileRecord.DataFormats.TextureDds:
                                try
                                {
                                    var buffer = f.ReadFileContent(ggpkContainer.fileStream);
                                    if (rtn.Name.EndsWith(".header"))
                                        buffer = buffer[16..];
                                    while (buffer[0] == '*')
                                    {
                                        var path = UTF8.GetString(buffer, 1, buffer.Length - 1);
                                        f = (IFileRecord)ggpkContainer.FindRecord(path, ggpkContainer.FakeBundles2);
                                        buffer = f.ReadFileContent(ggpkContainer.fileStream);
                                        if (path.EndsWith(".header"))
                                            buffer = buffer[16..];
                                    }
                                    Pfim.IImage image;
                                    if (buffer[0] != 'D' || buffer[1] != 'D' || buffer[2] != 'S' || buffer[3] != ' ')
                                        buffer = BrotliSharpLib.Brotli.DecompressBuffer(buffer, 4, buffer.Length - 4);
                                    image = Pfim.Pfim.FromStream(new MemoryStream(buffer));
                                    image.Decompress();
                                    ImageView.Tag = rtn.Name;
                                    ImageView.Source = BitmapSource.Create(image.Width, image.Height, 96.0, 96.0,
                                    PixelFormat(image), null, image.Data, image.Stride);
                                    ImageView.Visibility = Visibility.Visible;
                                } catch (Exception ex) {
                                    TextView.Text = ex.ToString();
                                    TextView.IsReadOnly = true;
                                    TextView.Visibility = Visibility.Visible;
                                }
                                break;
                            case IFileRecord.DataFormats.BK2:
                                //TODO
                                //BK2View.Visibility = Visibility.Visible;
                                break;
                            case IFileRecord.DataFormats.BANK:
                                //TODO
                                //BANKView.Visibility = Visibility.Visible;
                                break;
                        }
                    }
                }
            }
        }

        DataGridLength dataGridLength = new(1.0, DataGridLengthUnitType.Auto);
        private void ShowDatFile(DatContainer dat) {
            toMark.Clear();
            DatTable.Tag = dat;
            DatTable.Columns.Clear();
            var eos = new List<ExpandoObject>(dat.FieldDefinitions.Count);
            for (var i = 0; i < dat.FieldDatas.Count; i++) {
                var eo = new ExpandoObject() as IDictionary<string, object>;
                eo.Add("Row", i + 1);
                foreach (var (name, value) in (dat.FieldDefinitions.Keys, dat.FieldDatas[i]))
                    eo.Add((string)name, value);
                eos.Add((ExpandoObject)eo);
            }

            DatTable.Columns.Add(new DataGridTextColumn {
                Header = "Row",
                Binding = new Binding("Row"),
                Width = dataGridLength
            });
            foreach (var col in dat.FieldDefinitions.Keys)
                DatTable.Columns.Add(new DataGridTextColumn {
                    Header = col,
                    Binding = new Binding(col + ".Value") { TargetNullValue = "{null}" },
                    Width = dataGridLength
                });

            DatTable.ItemsSource = eos;

            var lastEndOffset = 8L;
            var row = 0;
            foreach (var p in dat.PointedDatas.Values) {
                if (p.Offset != lastEndOffset)
                    toMark.Add(row);
                lastEndOffset = p.EndOffset;
                ++row;
            }
            DatPointedTable.ItemsSource = dat.PointedDatas.Values;

            if (dat.FirstError.HasValue)
                MessageBox.Show($"At Row:{dat.FirstError.Value.Row},\r\nColumn:{dat.FirstError.Value.Column} ({dat.FirstError.Value.FieldName}),\r\nStreamPosition:{dat.FirstError.Value.StreamPosition},\r\nLastSucceededPosition:{dat.FirstError.Value.LastSucceededPosition}\r\n\r\n{dat.FirstError.Value.Exception}", "Error While Reading: " + dat.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Get the PixelFormat of the dds image
        /// </summary>
        public static PixelFormat PixelFormat(Pfim.IImage image) => image.Format switch {
            Pfim.ImageFormat.Rgb24 => PixelFormats.Bgr24,
            Pfim.ImageFormat.Rgba32 => PixelFormats.Bgr32,
            Pfim.ImageFormat.Rgb8 => PixelFormats.Gray8,
            Pfim.ImageFormat.R5g5b5a1 or Pfim.ImageFormat.R5g5b5 => PixelFormats.Bgr555,
            Pfim.ImageFormat.R5g6b5 => PixelFormats.Bgr565,
            _ => throw new Exception($"Unable to convert {image.Format} to WPF PixelFormat"),
        };

        /// <summary>
        /// TreeViewItem MouseDown event
        /// </summary>
        private void OnTreePreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is not DependencyObject ui || ui is TreeView) return;

            // Get Clicked TreeViewItem
            while (ui is not TreeViewItem)
                ui = VisualTreeHelper.GetParent(ui);
            var tvi = ui as TreeViewItem;

            if (e.ChangedButton != MouseButton.Left)
                tvi.IsSelected = true; // Select when clicked
            else if (tvi.Tag is DirectoryRecord or BundleDirectoryNode && e.Source is not TreeViewItem) // Is Directory
                tvi.IsExpanded = true; // Expand when left clicked (but not on arrow)
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Effects.HasFlag(DragDropEffects.Copy)) return; // Drop File/Folder
            var dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            string fileName;
            if (dropped.Length != 1 || (fileName = Path.GetFileName(dropped[0])) != "ROOT" && !fileName.EndsWith(".zip"))
            {
                MessageBox.Show("You can only drop \"ROOT\" folder or a .zip file that contains it", "Replace Faild",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var bkg = new BackgroundDialog();
            if (fileName.EndsWith(".zip"))
                Task.Run(() => {
                    try {
                        var f = ZipFile.OpenRead(dropped[0]);
                        var es = f.Entries;
                        var list = new List<KeyValuePair<IFileRecord, ZipArchiveEntry>>(es.Count);
                        ggpkContainer.GetFileListFromZip(es, list);
                        if (MessageBox.Show($"Replace {list.Count} Files?", "Replace Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) {
                            Dispatcher.Invoke(bkg.Close);
                            return;
                        }
                        bkg.ProgressText = "Replacing {0}/" + list.Count.ToString() + " Files . . .";
                        ggpkContainer.Replace(list, bkg.NextProgress);
                        Dispatcher.Invoke(() => {
                            MessageBox.Show("Replaced " + list.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                            bkg.Close();
                        });
                    } catch (Exception ex) {
                        App.HandledException(ex);
                    }
                });
            else
                Task.Run(() => {
                    try {
                        var list = new Collection<KeyValuePair<IFileRecord, string>>();
                        ggpkContainer.GetFileList(dropped[0], list);
                        if (MessageBox.Show($"Replace {list.Count} Files?", "Replace Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) {
                            Dispatcher.Invoke(bkg.Close);
                            return;
					    }
                        bkg.ProgressText = "Replacing {0}/" + list.Count.ToString() + " Files . . .";
                        ggpkContainer.Replace(list, bkg.NextProgress);
                        Dispatcher.Invoke(() => {
                            MessageBox.Show("Replaced " + list.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                            bkg.Close();
                        });
                    } catch (Exception ex) {
                        App.HandledException(ex);
                    }
            });
            bkg.ShowDialog();
        }

        private void OnExportClicked(object sender, RoutedEventArgs e)
        {
            if ((Tree.SelectedItem as TreeViewItem)?.Tag is RecordTreeNode rtn)
            {
                var sfd = new SaveFileDialog();
                if (rtn is IFileRecord fr)
                {
                    sfd.FileName = rtn.Name;
                    if (sfd.ShowDialog() == true)
                    {
                        File.WriteAllBytes(sfd.FileName, fr.ReadFileContent(ggpkContainer.fileStream));
                        MessageBox.Show("Exported " + rtn.GetPath(), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    sfd.FileName = rtn.Name + ".dir";
                    if (sfd.ShowDialog() == true)
                    {
                        var bkg = new BackgroundDialog();
                        Task.Run(() => {
                            try {
                                var list = new SortedDictionary<IFileRecord, string>(BundleSortComparer.Instance);
                                var path = Directory.GetParent(sfd.FileName).FullName + "\\" + rtn.Name;
                                GGPKContainer.RecursiveFileList(rtn, path, list, true);
                                bkg.ProgressText = "Exporting {0}/" + list.Count.ToString() + " Files . . .";
                                GGPKContainer.Export(list, bkg.NextProgress);
                                Dispatcher.Invoke(() => {
                                    MessageBox.Show("Exported " + list.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                                    bkg.Close();
                                });
                            } catch (Exception ex) {
                                App.HandledException(ex);
                            }
                        });
                        bkg.ShowDialog();
                    }
                }
            }
        }

        private void OnReplaceClicked(object sender, RoutedEventArgs e)
        {
            if ((Tree.SelectedItem as TreeViewItem)?.Tag is RecordTreeNode rtn)
            {
                if (rtn is IFileRecord fr)
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
                        var bkg = new BackgroundDialog();
                        Task.Run(() => {
                            try {
                                var list = new Collection<KeyValuePair<IFileRecord, string>>();
                                GGPKContainer.RecursiveFileList(rtn, ofd.DirectoryPath, list, false);
                                bkg.ProgressText = "Replacing {0}/" + list.Count.ToString() + " Files . . .";
                                ggpkContainer.Replace(list, bkg.NextProgress);
                                Dispatcher.Invoke(() => {
                                    MessageBox.Show("Replaced " + list.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                                    bkg.Close();
                                });
                            } catch (Exception ex) {
                                App.HandledException(ex);
                            }
                        });
                        bkg.ShowDialog();
                    }
                }
            }
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            if (Tree.SelectedItem is TreeViewItem tvi && tvi.Tag is IFileRecord fr)
            {
                switch (fr.DataFormat) {
                    case IFileRecord.DataFormats.Ascii:
                        fr.ReplaceContent(UTF8.GetBytes(TextView.Text));
                        break;
                    case IFileRecord.DataFormats.Unicode:
                        if (((RecordTreeNode)fr).GetPath().EndsWith(".amd"))
                            fr.ReplaceContent(Unicode.GetBytes(TextView.Text));
                        else if (((RecordTreeNode)fr).Parent.Name == "Bundles" || ((RecordTreeNode)fr).Name == "minimap_colours.txt")
                            goto case IFileRecord.DataFormats.Ascii;
                        else
                            fr.ReplaceContent(Unicode.GetBytes("\xFEFF" + TextView.Text));
                        break;
                    default:
                        return;
                }
                MessageBox.Show("Saved to " + ((RecordTreeNode)fr).GetPath(), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnRecoveryClicked(object sender, RoutedEventArgs e) {
            if (new VersionSelector().ShowDialog() != true) return;

            var bkg = new BackgroundDialog();
            Task.Run(() => {
                try {
                    Web ??= new WebClient();
                    string PatchServer = null;
                    var indexUrl = SelectedVersion switch {
                        1 => (PatchServer = GetPatchServer()) + "Bundles2/_.index.bin",
                        2 => (PatchServer = GetPatchServer(true)) + "Bundles2/_.index.bin",
                        3 => "http://poesmoother.eu/owncloud/index.php/s/GKuEGtTyAsRueqC/download",
                        _ => null
                    };
                    var l = new List<IFileRecord>();
                    GGPKContainer.RecursiveFileList((RecordTreeNode)((TreeViewItem)Tree.SelectedItem).Tag, l);
                    bkg.ProgressText = "Recovering {0}/" + l.Count.ToString() + " Files . . .";

                    BinaryReader br = null;
                    IndexContainer i = null;
                    if (l.Any((ifr) => ifr is BundleFileNode)) {
                        if (SelectedVersion == 3) {
                            MessageBox.Show("Tencent version currently only support recovering files under \"Bundles2\" directory !", "Unsupported", MessageBoxButton.OK, MessageBoxImage.Error);
                            Dispatcher.Invoke(bkg.Close);
                            return;
                        }
                        br = new BinaryReader(new MemoryStream(Web.DownloadData(indexUrl)));
                        i = new IndexContainer(br);
                    }

                    foreach (var f in l) {
                        if (f is BundleFileNode bfn) {
                            var bfr = bfn.BundleFileRecord;
                            var newbfr = i.FindFiles[bfr.NameHash];
                            bfr.Offset = newbfr.Offset;
                            bfr.Size = newbfr.Size;
                            bfr.BundleIndex = newbfr.BundleIndex;
                        } else {
                            var fr = f as FileRecord;
                            var path = Regex.Replace(fr.GetPath(), "^ROOT/", "");
                            fr.ReplaceContent(Web.DownloadData(PatchServer + path));
                        }
                        bkg.NextProgress();
                    }
                    br?.Close();

                    if (i != null)
                        if (SteamMode)
                            ggpkContainer.Index.Save("_.index.bin");
                        else
                            ggpkContainer.IndexRecord.ReplaceContent(ggpkContainer.Index.Save());
                    Dispatcher.Invoke(() => {
                        MessageBox.Show("Recoveried " + l.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                        bkg.Close();
                    });
                } catch (Exception ex) {
                    App.HandledException(ex);
                }
            });
            bkg.ShowDialog();
		}

		private static string GetPatchServer(bool garena = false) {
            var tcp = new TcpClient() { NoDelay = true };
            tcp.Connect(Dns.GetHostAddresses(garena ? "login.tw.pathofexile.com" : "us.login.pathofexile.com"), garena ? 12999 : 12995);
            var tcs = tcp.GetStream();
            tcs.Write(new byte[] { 1, 4 }, 0, 2);
            var b = new byte[256];
            tcs.Read(b, 0, 256);
            tcs.Close();
            tcp.Close();
            return Encoding.Unicode.GetString(b, 35, b[34] * 2);
        }

        private void OnSavePngClicked(object sender, RoutedEventArgs e) {
            var sfd = new SaveFileDialog { FileName = ((string)ImageView.Tag).Replace("dds", "png") };
            if (sfd.ShowDialog() == true) {
                var pbe = new PngBitmapEncoder();
                pbe.Frames.Add(BitmapFrame.Create((BitmapSource)ImageView.Source));
                var f = File.OpenWrite(sfd.FileName);
                pbe.Save(f);
                f.Flush();
                f.Close();
                MessageBox.Show("Saved " + sfd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

		private void ReloadClick(object sender, RoutedEventArgs e) {
            try {
                DatContainer.ReloadDefinitions();
                OnTreeSelectedChanged(null, null);
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

		private void CSVClick(object sender, RoutedEventArgs e) {
            var dat = DatTable.Tag as DatContainer;
            var sfd = new SaveFileDialog() {
                FileName = dat.Name + ".csv",
                DefaultExt = "csv"
            };
            if (sfd.ShowDialog() != true) return;
            File.WriteAllText(sfd.FileName, dat.ToCsv());
            MessageBox.Show($"Exported " + sfd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }

		private void FilterButton_Click(object sender, RoutedEventArgs e) {
            Tree.Items.Clear();
            ggpkContainer.FakeBundles2.Children.Clear();
            foreach (var f in ggpkContainer.Index.Files)
                if (RegexCheckBox.IsChecked.Value && Regex.IsMatch(f.path, FilterBox.Text) || !RegexCheckBox.IsChecked.Value && f.path.Contains(FilterBox.Text)) ggpkContainer.BuildBundleTree(f, ggpkContainer.FakeBundles2);
            var root = CreateNode(ggpkContainer.rootDirectory);
            Tree.Items.Add(root);
            root.IsExpanded = true;
        }

		private void FilterBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key != Key.Enter) return;
            FilterButton_Click(null, null);
            e.Handled = true;
        }
	}

	public static class GetTupleEnumerator {
        public static IEnumerator<(object, object)> GetEnumerator(this (IEnumerable, IEnumerable) TupleEnumerable) => new TupleEnumerator(TupleEnumerable);

        public class TupleEnumerator : ITuple, IEnumerator<(object, object)> {

            public IEnumerator Item1;

            public IEnumerator Item2;

            public TupleEnumerator((IEnumerable, IEnumerable) TupleEnumerable) {
                Item1 = TupleEnumerable.Item1.GetEnumerator();
                Item2 = TupleEnumerable.Item2.GetEnumerator();
            }

            public (object, object) Current => (Item1.Current, Item2.Current);

            object IEnumerator.Current => Current;

            public int Length => 2;

            public object this[int index] => index switch {
                1 => Item1,
                2 => Item2,
                _ => throw new IndexOutOfRangeException()
            };

            public bool MoveNext() {
                return Item1.MoveNext() | Item2.MoveNext();
            }

            public void Reset() {
                Item1.Reset();
                Item2.Reset();
            }

#pragma warning disable CA1816 // Dispose 方法應該呼叫 SuppressFinalize
            public void Dispose() { }
        }
    }
}