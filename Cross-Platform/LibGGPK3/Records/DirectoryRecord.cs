using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGGPK3.Records {
	public class DirectoryRecord : TreeNode {
		public struct DirectoryEntry {
			/// <summary>
			/// Murmur2 hash of lowercase entry name
			/// </summary>
			public uint EntryNameHash;
			/// <summary>
			/// Offset in pack file where the record begins
			/// </summary>
			public long Offset;

			public DirectoryEntry(uint entryNameHash, long offset) {
				EntryNameHash = entryNameHash;
				Offset = offset;
			}
		}

		public static readonly byte[] Tag = Encoding.ASCII.GetBytes("PDIR");

		/// <summary>
		/// Records (File/Directory) this directory contains.
		/// </summary>
		public DirectoryEntry[] Entries;
		/// <summary>
		/// Offset in pack file where entries list begins. This is only here because it makes rewriting the entries list easier.
		/// </summary>
		public long EntriesBegin;

		/// <summary>
		/// Read a DirectoryRecord from GGPK
		/// </summary>
		public DirectoryRecord(int length, GGPK ggpk) : base(length, ggpk) {
			Offset = ggpk.FileStream.Position - 8;
			var nameLength = ggpk.FileStream.ReadInt32();
			var totalEntries = ggpk.FileStream.ReadInt32();

			Hash = new byte[32];
			ggpk.FileStream.Read(Hash, 0, 32);
			var name = new byte[2 * (nameLength - 1)];
			ggpk.FileStream.Read(name, 0, name.Length);
			Name = Encoding.Unicode.GetString(name);
			ggpk.FileStream.Seek(2, SeekOrigin.Current); // Null terminator

			EntriesBegin = ggpk.FileStream.Position;
			Entries = new DirectoryEntry[totalEntries];
			for (var i = 0; i < totalEntries; i++)
				Entries[i] = new DirectoryEntry((uint)ggpk.FileStream.ReadInt32(), ggpk.FileStream.ReadInt64());
		}

		private SortedSet<TreeNode>? _Children;
		public virtual SortedSet<TreeNode> Children {
			get {
				if (_Children == null) {
					_Children = new SortedSet<TreeNode>(NodeComparer.Instance);
					foreach (var e in Entries) {
						var b = (TreeNode)Ggpk.ReadRecord(e.Offset);
						b.Parent = this;
						_Children.Add(b);
					}
				}
				return _Children;
			}
		}

		protected internal override void Write(Stream? writeTo = null) {
			writeTo ??= Ggpk.FileStream;
			Offset = writeTo.Position;
			writeTo.Write(Length);
			writeTo.Write(Tag);
			writeTo.Write(Name.Length + 1);
			writeTo.Write(Entries.Length);
			writeTo.Write(Hash);
			writeTo.Write(Encoding.Unicode.GetBytes(Name));
			writeTo.Write((short)0); // Null terminator
			foreach (var entry in Entries) {
				writeTo.Write(entry.EntryNameHash);
				writeTo.Write(entry.Offset);
			}
		}

		/// <summary>
		/// Use to sort the children of directory.
		/// </summary>
		protected sealed class NodeComparer : IComparer<TreeNode> {
			public static readonly IComparer<TreeNode> Instance = OperatingSystem.IsWindows() ? new NodeComparer_Windows() : new NodeComparer();

#pragma warning disable CS8767
			public int Compare(TreeNode x, TreeNode y) {
				if (x is DirectoryRecord)
					if (y is DirectoryRecord)
						return string.Compare(x.Name, y.Name);
					else
						return -1;
				else
					if (y is DirectoryRecord)
						return 1;
					else
						return string.Compare(x.Name, y.Name);
			}

			public sealed class NodeComparer_Windows : IComparer<TreeNode> {
				[DllImport("shlwapi", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
				public static extern int StrCmpLogicalW(string x, string y);
				public int Compare(TreeNode x, TreeNode y) {
					if (x is DirectoryRecord)
						if (y is DirectoryRecord)
							return StrCmpLogicalW(x.Name, y.Name);
						else
							return -1;
					else
						if (y is DirectoryRecord)
							return 1;
						else
							return StrCmpLogicalW(x.Name, y.Name);
				}
			}
		}
	}
}