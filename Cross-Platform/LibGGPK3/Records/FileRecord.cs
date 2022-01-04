using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System;

namespace LibGGPK3.Records {
	/// <summary>
	/// Record contains the data of a file.
	/// </summary>
	public class FileRecord : TreeNode {
		public static readonly byte[] Tag = Encoding.ASCII.GetBytes("FILE");
		public static readonly SHA256 Hash256 = SHA256.Create();

		/// <summary>
		/// Offset in pack file where the raw data begins
		/// </summary>
		public long DataOffset;
		/// <summary>
		/// Length of the raw file data
		/// </summary>
		public int DataLength;

		public FileRecord(int length, GGPK ggpk) : base(length, ggpk) {
			Offset = ggpk.FileStream.Position - 8;
			var br = Ggpk.Reader;
			var nameLength = br.ReadInt32();
			Hash = br.ReadBytes(32);
			Name = Encoding.Unicode.GetString(br.ReadBytes(2 * (nameLength - 1)));
			br.BaseStream.Seek(2, SeekOrigin.Current); // Null terminator
			DataOffset = br.BaseStream.Position;
			DataLength = Length - (nameLength * 2 + 44); // Length - (8 + nameLength * 2 + 32 + 4)
			br.BaseStream.Seek(DataLength, SeekOrigin.Current);
		}

		protected internal override void Write(Stream? writeTo = null) {
			writeTo ??= Ggpk.FileStream;
			Offset = writeTo.Position;
			writeTo.Write(Length);
			writeTo.Write(Tag);
			writeTo.Write(Name.Length + 1);
			writeTo.Write(Hash);
			writeTo.Write(Encoding.Unicode.GetBytes(Name));
			writeTo.Write((short)0); // Null terminator
			DataOffset = writeTo.Position;
			// Actual file content writing of FileRecord isn't here
		}

		/// <summary>
		/// Get the file content of this record
		/// </summary>
		/// <param name="ggpkStream">Stream of GGPK file</param>
		public virtual byte[] ReadFileContent(Stream? ggpkStream = null) {
			var buffer = new byte[DataLength];
			ggpkStream ??= Ggpk.FileStream;
			ggpkStream.Seek(DataOffset, SeekOrigin.Begin);
			for (var l = 0; l < DataLength;)
				ggpkStream.Read(buffer, l, DataLength - l);
			return buffer;
		}

		/// <summary>
		/// Replace the file content with a new content,
		/// and move the record to the FreeRecord with most suitable size.
		/// </summary>
		public virtual void ReplaceContent(ReadOnlySpan<byte> NewContent) {
			var bw = Ggpk.Writer;

			if (!Hash256.TryComputeHash(NewContent, Hash, out _))
				throw new("Unable to compute hash of the content");

			if (NewContent.Length == DataLength) { // Replace in situ
				bw.BaseStream.Seek(DataOffset, SeekOrigin.Begin);
				bw.Write(NewContent);
			} else { // Replace a FreeRecord
				var oldOffset = Offset;
				MarkAsFreeRecord();
				DataLength = NewContent.Length;
				Length = 44 + (Name.Length + 1) * 2 + DataLength; // (8 + (Name + "\0").Length * 2 + 32 + 4) + DataLength

				LinkedListNode<FreeRecord>? bestNode = null; // Find the FreeRecord with most suitable size
				var currentNode = Ggpk.FreeRecords.First!;
				var space = int.MaxValue;
				do {
					if (currentNode.Value.Length == Length) {
						bestNode = currentNode;
						space = 0;
						break;
					}
					var tmpSpace = currentNode.Value.Length - Length;
					if (tmpSpace < space && tmpSpace >= 16) {
						bestNode = currentNode;
						space = tmpSpace;
					}
				} while ((currentNode = currentNode.Next) != null);

				if (bestNode == null) {
					bw.BaseStream.Seek(0, SeekOrigin.End); // Write to the end of GGPK
					Write();
					bw.Write(NewContent);
				} else {
					FreeRecord free = bestNode.Value;
					bw.BaseStream.Seek(free.Offset + free.Length - Length, SeekOrigin.Begin); // Write to the FreeRecord
					Write();
					bw.Write(NewContent);
					free.Length = space;
					if (space >= 16) { // Update offset of FreeRecord
						bw.BaseStream.Seek(free.Offset, SeekOrigin.Begin);
						bw.Write(free.Length);
					} else // Remove the FreeRecord
						free.Remove(bestNode);
				}

				UpdateOffset(oldOffset); // Update the offset of FileRecord in Parent.Entries/>
			}
			bw.Flush();
		}

		/// <summary>
		/// Set the record to a FreeRecord
		/// </summary>
		public virtual void MarkAsFreeRecord() {
			var bw = Ggpk.Writer;
			bw.BaseStream.Seek(Offset, SeekOrigin.Begin);
			var free = new FreeRecord(Length, Ggpk, 0, Offset);
			free.Write();
			var lastFree = Ggpk.FreeRecords.Last?.Value;
			if (lastFree == null) // No FreeRecord
			{
				Ggpk.GgpkRecord.FirstFreeRecordOffset = Offset;
				Ggpk.FileStream.Seek(Ggpk.GgpkRecord.Offset + 20, SeekOrigin.Begin);
				Ggpk.Writer.Write(Offset);
			} else {
				lastFree.NextFreeOffset = Offset;
				Ggpk.FileStream.Seek(lastFree.Offset + 8, SeekOrigin.Begin);
				Ggpk.Writer.Write(Offset);
			}
			Ggpk.FreeRecords.AddLast(free);
		}

		/// <summary>
		/// Update the offset of the record in <see cref="DirectoryRecord.Entries"/>
		/// </summary>
		/// <param name="OldOffset">The original offset to be update</param>
		public virtual void UpdateOffset(long OldOffset) {
			var Parent = (DirectoryRecord)this.Parent!;
			for (int i = 0; i < Parent.Entries.Length; i++) {
				if (Parent.Entries[i].Offset == OldOffset) {
					Parent.Entries[i].Offset = Offset;
					Ggpk.FileStream.Seek(Parent.EntriesBegin + i * 12 + 4, SeekOrigin.Begin);
					Ggpk.Writer.Write(Offset);
					return;
				}
			}
			throw new System.Exception(GetPath() + " update offset faild:" + OldOffset.ToString() + " => " + Offset.ToString());
		}

		public enum DataFormats {
			Unknown,
			Image,
			Ascii,
			Unicode,
			OGG,
			Dat,
			TextureDds,
			BK2,
			BANK
		}

		protected DataFormats? _DataFormat;
		/// <summary>
		/// Content data format of this file
		/// </summary>
		public virtual DataFormats DataFormat {
			get {
				_DataFormat ??= GetDataFormat(Name);
				return _DataFormat.Value;
			}
		}

		public static DataFormats GetDataFormat(string name) {
#pragma warning disable IDE0066 // 將 switch 陳述式轉換為運算式
			switch (Path.GetExtension(name).ToLower()) {
				case ".act":
				case ".ais":
				case ".amd": // Animated Meta Data
				case ".ao": // Animated Object
				case ".aoc": // Animated Object Controller
				case ".arl":
				case ".arm": // Rooms
				case ".atlas":
				case ".cht": // ChestData
				case ".clt":
				case ".dct": // Decals
				case ".ddt": // Doodads
				case ".dgr":
				case ".dlp":
				case ".ecf":
				case ".edp":
				case ".env": // Environment
				case ".epk":
				case ".et":
				case ".ffx": // FFX Render
				case ".fxgraph":
				case ".gft":
				case ".gt": // Ground Types
				case ".idl":
				case ".idt":
				case ".json":
				case ".mat": // Materials
				case ".mtd":
				case ".ot":
				case ".otc":
				case ".pet":
				case ".red":
				case ".rs": // Room Set
				case ".sm": // Skin Mesh
				case ".tgr":
				case ".tgt":
				case ".trl": // Trails Effect
				case ".tsi":
				case ".tst":
				case ".txt":
				case ".ui": // User Interface
				case ".xml":
					return DataFormats.Unicode;
				case ".csv":
				case ".filter": // Item/loot Filter
				case ".fx": // Shader
				case ".hlsl": // Shader
				case ".mel": // Maya Embedded Language
				case ".properties":
				case ".slt":
					return DataFormats.Ascii;
				case ".dat":
				case ".dat64":
				case ".datl":
				case ".datl64":
					return DataFormats.Dat;
				case ".dds":
				case ".header":
					return DataFormats.TextureDds;
				case ".jpg":
				case ".png":
				case ".bmp":
					return DataFormats.Image;
				case ".ogg":
					return DataFormats.OGG;
				case ".bk2":
					return DataFormats.BK2;
				case ".bank":
					return DataFormats.BANK;
				case ".fmt":
				case ".mtp":
				case ".smd": // Skin Mesh Data
				default:
					return DataFormats.Unknown;
			}
		}
	}
}