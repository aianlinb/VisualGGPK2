using LibGGPK3.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibGGPK3 {
	public class GGPK {
		protected internal Stream FileStream;
		public readonly GGPKRecord GgpkRecord;
		public readonly DirectoryRecord Root;
		public readonly LinkedList<FreeRecord> FreeRecords;

		/// <param name="filePath">Path to Content.ggpk</param>
		public GGPK(string filePath) {
			// Open File
			FileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

			// Read ROOT Directory Record
			BaseRecord ggpk;
			while ((ggpk = ReadRecord()) is not GGPKRecord);
			GgpkRecord = (GGPKRecord)ggpk;
			Root = (DirectoryRecord)ReadRecord(GgpkRecord.RootDirectoryOffset);
			Root.Name = "ROOT";

			// Build Linked FreeRecord List
			FreeRecords = new();
			var NextFreeOffset = GgpkRecord.FirstFreeRecordOffset;
			while (NextFreeOffset > 0) {
				var current = (FreeRecord)ReadRecord(NextFreeOffset);
				FreeRecords.AddLast(current);
				NextFreeOffset = current.NextFreeOffset;
			}
		}

		/// <summary>
		/// Read a record from GGPK at <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">Record offset, null for current stream position</param>
		public unsafe virtual BaseRecord ReadRecord(long? offset = null) {
			if (offset.HasValue)
				FileStream.Seek(offset.Value, SeekOrigin.Begin);
			var length = FileStream.ReadInt32();
			var tag = new byte[4];
			FileStream.Read(tag, 0, 4);
			if (tag.SequenceEqual(FileRecord.Tag))
				return new FileRecord(length, this);
			else if (tag.SequenceEqual(DirectoryRecord.Tag))
				return new DirectoryRecord(length, this);
			else if (tag.SequenceEqual(FreeRecord.Tag))
				return new FreeRecord(length, this);
			else if (tag.SequenceEqual(GGPKRecord.Tag))
				return new GGPKRecord(length, this);
			else
				throw new Exception("Invalid Record Tag: " + Encoding.UTF8.GetString(tag) + " at offset: " + (FileStream.Position - 8).ToString());
		}

		/// <summary>
		/// Find the record with a <paramref name="path"/>
		/// </summary>
		/// <param name="path">Path in GGPK under <paramref name="parent"/></param>
		/// <param name="parent">Where to start searching, null for ROOT directory in GGPK</param>
		/// <returns>null if not found</returns>
		public virtual TreeNode? FindNode(string path, DirectoryRecord? parent = null) {
			parent ??= Root;
			var SplittedPath = path.Split('/', '\\');
			foreach (var name in SplittedPath) {
				var next = parent.Children.FirstOrDefault(t => t.Name == name);
				if (next is not DirectoryRecord dr)
					return next;
				parent = dr;
			}
			return parent;
		}

		/// <summary>
		/// Export file/directory synchronously
		/// </summary>
		/// <param name="record">File/Directory Record to export</param>
		/// <param name="path">Path to save</param>
		/// <param name="ProgressStep">It will be executed every time a file is exported</param>
		/// <returns>Number of files exported</returns>
		public static int Extract(TreeNode record, string path) {
			if (record is FileRecord fr) {
				File.WriteAllBytes(path, fr.ReadFileContent());
				return 1;
			} else {
				var count = 0;
				Directory.CreateDirectory(path);
				foreach (var f in ((DirectoryRecord)record).Children)
					count += Extract(f, path + "\\" + f.Name);
				return count;
			}
		}

		/// <summary>
		/// Replace file/directory synchronously
		/// </summary>
		/// <param name="record">File/Directory Record to replace</param>
		/// <param name="path">Path to file to import</param>
		/// <param name="ProgressStep">It will be executed every time a file is replaced</param>
		/// <returns>Number of files replaced</returns>
		public static int Replace(TreeNode record, string path) {
			if (record is FileRecord fr) {
				if (File.Exists(path)) {
					fr.ReplaceContent(File.ReadAllBytes(path));
					return 1;
				}
				return 0;
			} else {
				var count = 0;
				foreach (var f in ((DirectoryRecord)record).Children)
					count += Replace(f, path + "\\" + f.Name);
				return count;
			}
		}
	}
}