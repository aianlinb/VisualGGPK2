using LibBundle3.Records;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace LibBundle3 {
	public unsafe class Index : IDisposable {
		protected readonly Bundle bundle;
		public readonly BundleRecord[] Bundles;
		public readonly Dictionary<ulong, FileRecord> Files;
		public readonly DirectoryRecord[] Directories;
		protected readonly byte[] directoryBundleData;
		protected int UncompressedSize; // For memory alloc when saving

		protected DirectoryNode? _Root;
		public virtual DirectoryNode Root {
			get {
				if (_Root is null) {
					_Root = new("ROOT");
					foreach (var f in Files.Values) {
						var paths = f.Path.Split('/');
						var parent = Root;
						var last = paths.Length - 1;
						for (var i = 0; i < last; ++i) {
							if (parent.Children.FirstOrDefault(n => n.Name == paths[i]) is not DirectoryNode next) {
								next = new DirectoryNode(paths[i]);
								parent.Children.Add(next);
								next.Parent = parent;
							}
							parent = next;
						}
						parent.Children.Add(new FileNode(f));
					}
				}
				return _Root;
			}
		}

		public Index(string filePath) : this(File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read), false) { }

		public Index(Stream stream, bool leaveOpen = true) {
			bundle = new(stream, leaveOpen);
			var data = bundle.ReadData();
			UncompressedSize = data.Length;
			var UTF8 = Encoding.UTF8;
			fixed (byte* p = data) {
				var ptr = (int*)p;

				var bundleCount = *ptr++;
				Bundles = new BundleRecord[bundleCount];
				for (var i = 0; i < bundleCount; i++) {
					var nameLength = *ptr++;
					var name = UTF8.GetString((byte*)ptr, nameLength);
					ptr = (int*)((byte*)ptr + nameLength);
					var uncompressedSize = *ptr++;
					Bundles[i] = new BundleRecord(nameLength, name, uncompressedSize, this) { BundleIndex = i };
				}

				var fileCount = *ptr++;
				Files = new(fileCount);
				for (var i = 0; i < fileCount; i++) {
					var nameHash = *(ulong*)ptr;
					ptr += 2;
					var f = new FileRecord(nameHash, *ptr++, *ptr++, *ptr++);
					Files[nameHash] = f;
					var b = Bundles[f.BundleIndex];
					f.BundleRecord = b;
					b.Files.Add(f);
					if (f.Offset >= b.ValidSize)
						b.ValidSize = f.Offset + f.Size;
				}

				var directoryCount = *ptr++;
				Directories = new DirectoryRecord[directoryCount];
				for (var i = 0; i < directoryCount; i++) {
					var nameHash = *(ulong*)ptr;
					ptr += 2;
					Directories[i] = new DirectoryRecord(nameHash, *ptr++, *ptr++, *ptr++);
				}

				directoryBundleData = data[(int)((byte*)ptr - p)..];
			}

			fixed (byte* p = directoryBundleData) {
				var ptr = p;
				foreach (var d in Directories) {
					var temp = new List<string>();
					var Base = false;
					ptr = p + d.Offset;
					while (ptr - p <= d.Size - 4) {
						var index = *(int*)ptr;
						ptr += 4;
						if (index == 0) {
							Base = !Base;
							if (Base)
								temp.Clear();
						} else {
							index -= 1;
							var sb = new StringBuilder();
							byte c;
							while ((c = *ptr++) != 0)
								sb.Append((char)c);
							var str = sb.ToString();
							if (index < temp.Count)
								str = temp[index] + str;
							if (Base)
								temp.Add(str);
							else {
								var f = Files[FNV1a64Hash(str)];
								f.Path = str;
								d.Children.Add(f);
								f.DirectoryRecord = d;
							}
						}
					}
				}
			}
		}

		public virtual void Save() {
			var ms = new MemoryStream(UncompressedSize);
			var bw = new BinaryWriter(ms);
			var utf8 = Encoding.UTF8;

			bw.Write(Bundles.Length);
			foreach (var b in Bundles) {
				bw.Write(b.PathLength);
				bw.Write(utf8.GetBytes(b.Path), 0, b.PathLength);
				bw.Write(b.UncompressedSize);
			}

			bw.Write(Files.Count);
			foreach (var f in Files.Values) {
				bw.Write(f.PathHash);
				bw.Write(f.BundleIndex);
				bw.Write(f.Offset);
				bw.Write(f.Size);
			}

			bw.Write(Directories.Length);
			foreach (var d in Directories) {
				bw.Write(d.PathHash);
				bw.Write(d.Offset);
				bw.Write(d.Size);
				bw.Write(d.RecursiveSize);
			}

			bw.Write(directoryBundleData);

			bundle.SaveData(new(ms.GetBuffer(), 0, (int)ms.Length));
			bw.Close();
		}

		public static void Extract(Node node, string pathToSave) {
			if (node is FileNode fn) {
				var d = Path.GetDirectoryName(pathToSave);
				if (d is not null)
					Directory.CreateDirectory(d);
				var f = File.Create(pathToSave, fn.Record.Size);
				f.Write(fn.Record.Read());
				f.Flush();
				f.Close();
				return;
			}

			var list = new SortedList<FileRecord, string>(BundleComparer.Instance);
			RecursiveList(node, pathToSave, list, true);
			if (list.Count == 0)
				return;

			var br = list.First().Key.BundleRecord;
			var err = false;
			try {
				br.Bundle.ReadDataAndCache();
			} catch {
				err = true;
			}
			foreach (var (fr, path) in list) {
				if (br != fr.BundleRecord) {
					if (!err)
						br.Bundle.CachedData = null;
					br = fr.BundleRecord;
					try {
						br.Bundle.ReadDataAndCache();
						err = false;
					} catch {
						err = true;
					}
				}
				if (err)
					continue;
				var f = File.Create(path, fr.Size);
				f.Write(fr.Read());
				f.Flush();
				f.Close();
			}
			if (!err)
				br.Bundle.CachedData = null;
		}

		public virtual void Replace(Node node, string pathToLoad, bool dontChangeBundle = false) {
			if (node is FileNode fn) {
				var fr = fn.Record;
				var b = fr.BundleRecord.Bundle.ReadData();
				var f = File.OpenRead(pathToLoad);
				if (fr.Offset + fr.Size >= fr.BundleRecord.ValidSize)
					fr.BundleRecord.ValidSize = fr.Offset;
				var b2 = new byte[fr.BundleRecord.ValidSize + f.Length];
				Unsafe.CopyBlockUnaligned(ref b2[0], ref b[0], (uint)fr.BundleRecord.ValidSize);
				for (var l = 0; l < f.Length;)
					l += f.Read(b2, fr.BundleRecord.ValidSize + l, (int)f.Length - l);
				fr.BundleRecord.Bundle.SaveData(b2);
				fr.Offset = b.Length;
				fr.Size = b2.Length;
				f.Close();
				return;
			}
			if (dontChangeBundle) {
				var list = new SortedList<FileRecord, string>(BundleComparer.Instance);
				RecursiveList(node, pathToLoad, list);
				if (list.Count == 0)
					return;
				var br = list.First().Key.BundleRecord;
				var ms = new MemoryStream(br.Bundle.ReadData());
				ms.SetLength(br.ValidSize);
				foreach (var (fr, path) in list) {
					if (br != fr.BundleRecord) {
						br.Bundle.SaveData(new(ms.GetBuffer(), 0, (int)ms.Length));
						ms.Close();
						br.UncompressedSize = br.Bundle.UncompressedSize;
						br = fr.BundleRecord;
						ms = new MemoryStream(br.Bundle.ReadData());
						ms.SetLength(br.ValidSize);
					}
					var b = File.ReadAllBytes(path);
					ms.Write(b);
					fr.Offset = (int)ms.Length;
					fr.Size = b.Length;
				}
				br.Bundle.SaveData(new(ms.GetBuffer(), 0, (int)ms.Length));
				ms.Close();
				br.UncompressedSize = br.Bundle.UncompressedSize;
			} else {
				var list = new List<KeyValuePair<FileRecord, string>>();
				RecursiveList(node, pathToLoad, list);
				if (list.Count == 0)
					return;
				var maxSize = 300000000; //300MB
				var br = GetSmallestBundle();
				while (br.ValidSize >= maxSize)
					maxSize *= 2;
				var ms = new MemoryStream(br.Bundle.ReadData());
				ms.SetLength(br.ValidSize);
				foreach (var (fr, path) in list) {
					if (ms.Length >= maxSize) {
						br.Bundle.SaveData(new(ms.GetBuffer(), 0, (int)ms.Length));
						ms.Close();
						br.UncompressedSize = br.Bundle.UncompressedSize;
						br.ValidSize = br.UncompressedSize;
						br = GetSmallestBundle();
						while (br.ValidSize >= maxSize)
							maxSize *= 2;
						ms = new MemoryStream(br.Bundle.ReadData());
						ms.SetLength(br.ValidSize);
					}
					var b = File.ReadAllBytes(path);
					ms.Write(b);
					fr.Redirect(br, (int)ms.Length, b.Length);
				}
				br.Bundle.SaveData(new(ms.GetBuffer(), 0, (int)ms.Length));
				ms.Close();
				br.UncompressedSize = br.Bundle.UncompressedSize;
				br.ValidSize = br.UncompressedSize;
			}
			Save();
		}

		public virtual void Replace(IReadOnlyCollection<ZipArchiveEntry> zipEntries, DirectoryNode? parent = null) {
			var maxSize = 300000000; //300MB
			var br = GetSmallestBundle();
			while (br.ValidSize >= maxSize)
				maxSize *= 2;
			var ms = new MemoryStream(br.Bundle.ReadData());
			ms.SetLength(br.ValidSize);
			foreach (var zip in zipEntries) {
				if (zip.FullName.EndsWith('/'))
					continue;
				var n = FindNode(zip.FullName, parent);
				if (n is not FileNode f)
					continue;
				if (ms.Length >= maxSize) {
					br.Bundle.SaveData(new(ms.GetBuffer(), 0, (int)ms.Length));
					ms.Close();
					br.UncompressedSize = br.Bundle.UncompressedSize;
					br.ValidSize = br.UncompressedSize;
					br = GetSmallestBundle();
					while (br.ValidSize >= maxSize)
						maxSize *= 2;
					ms = new MemoryStream(br.Bundle.ReadData());
					ms.SetLength(br.ValidSize);
				}
				var b = zip.Open();
				b.CopyTo(ms);
				f.Record.Redirect(br, (int)ms.Length, (int)zip.Length);
			}
			br.Bundle.SaveData(new(ms.GetBuffer(), 0, (int)ms.Length));
			ms.Close();
			br.UncompressedSize = br.Bundle.UncompressedSize;
			br.ValidSize = br.UncompressedSize;

			Save();
		}

		public virtual Node? FindNode(string path, DirectoryNode? parent = null) {
			parent ??= Root;
			var SplittedPath = path.Split('/', '\\');
			foreach (var name in SplittedPath) {
				var next = parent.Children.FirstOrDefault(n => n.Name == name);
				if (next is not DirectoryNode dn)
					return next;
				parent = dn;
			}
			return parent;
		}

		public static void RecursiveList(Node node, string path, ICollection<KeyValuePair<FileRecord, string>> list, bool createDirectory = false) {
			if (node is FileNode fn)
				list.Add(new(fn.Record, path));
			else if (node is DirectoryNode dn) {
				if (createDirectory)
					Directory.CreateDirectory(path);
				foreach (var n in dn.Children)
					RecursiveList(n, path + "/" + n.Name, list, createDirectory);
			}
		}

		public static ulong FNV1a64Hash(string str) {
			if (str.EndsWith('/'))
				str = str.TrimEnd('/') + "++";
			else
				str = str.ToLower() + "++";

			var bs = Encoding.UTF8.GetBytes(str);
			var hash = 0xCBF29CE484222325UL;
			foreach (var by in bs)
				hash = (hash ^ by) * 0x100000001B3UL;
			// Equals to: bs.Aggregate(0xCBF29CE484222325UL, (current, by) => (current ^ by) * 0x100000001B3);
			return hash;
		}

		public virtual BundleRecord GetSmallestBundle() {
			if (Bundles is null || Bundles.Length == 0)
				throw new("Unable to find a valid bundle");
			var bundles = new List<BundleRecord>(Bundles);
			var result = bundles[0];
			var l = result.UncompressedSize;
			while (bundles.Count > 0) {
				foreach (var b in Bundles)
					if (b.UncompressedSize < l) {
						l = b.UncompressedSize;
						result = b;
					}
				try {
					_ = result.Bundle;
					return result;
				} catch {
					bundles.Remove(result);
				}
			}
			throw new("Unable to find a valid bundle");
		}

		public virtual void Dispose() {
			GC.SuppressFinalize(this);
			bundle.Dispose();
		}

		~Index() {
			Dispose();
		}

		protected class BundleComparer : IComparer<FileRecord> {
			public static BundleComparer Instance = new();
#pragma warning disable CS8767
			public int Compare(FileRecord x, FileRecord y) {
				return x.BundleRecord.BundleIndex - y.BundleRecord.BundleIndex;
			}
		}
	}
}