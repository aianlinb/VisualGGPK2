using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibGGPK3.Records {
	/// <summary>
	/// A free record represents space in the pack file that has been marked as deleted. It's much cheaper to just
	/// mark areas as free and append data to the end of the pack file than it is to rebuild the entire pack file just
	/// to remove a piece of data.
	/// </summary>
	public class FreeRecord : BaseRecord {
		public static readonly byte[] Tag = Encoding.ASCII.GetBytes("FREE");

		/// <summary>
		/// Offset of next FreeRecord
		/// </summary>
		protected internal long NextFreeOffset;

		protected internal FreeRecord(int length, GGPK ggpk) : base(length, ggpk) {
			Offset = ggpk.FileStream.Position - 8;
			var br = Ggpk.Reader;
			NextFreeOffset = br.ReadInt64();
			br.BaseStream.Seek(Length - 16, SeekOrigin.Current);
		}

		protected internal FreeRecord(int length, GGPK ggpk, long nextFreeOffset, long recordBegin) : base(length, ggpk) {
			Offset = recordBegin;
			NextFreeOffset = nextFreeOffset;
		}

		protected internal override void Write(BinaryWriter? bw = null) {
			bw ??= Ggpk.Writer;
			Offset = bw.BaseStream.Position;
			bw.Write(Length);
			bw.Write(Tag);
			bw.Write(NextFreeOffset);
			bw.BaseStream.Seek(Length - 16, SeekOrigin.Current);
		}

		/// <summary>
		/// Remove this FreeRecord from the Linked FreeRecord List
		/// </summary>
		/// <param name="node">Node in <see cref="GGPK.FreeRecords"/> to remove</param>
		public virtual void Remove(LinkedListNode<FreeRecord>? node = null) {
			node ??= Ggpk.FreeRecords.Find(this);
			if (node is null)
				return;
			var previous = node.Previous?.Value;
			var next = node.Next?.Value;
			if (next == null)
				if (previous == null) {
					Ggpk.GgpkRecord.FirstFreeRecordOffset = 0;
					Ggpk.FileStream.Seek(Ggpk.GgpkRecord.Offset + 20, SeekOrigin.Begin);
					Ggpk.Writer.Write((long)0);
				} else {
					previous.NextFreeOffset = 0;
					Ggpk.FileStream.Seek(previous.Offset + 8, SeekOrigin.Begin);
					Ggpk.Writer.Write((long)0);
				}
			else
				if (previous == null) {
				Ggpk.GgpkRecord.FirstFreeRecordOffset = next.Offset;
				Ggpk.FileStream.Seek(Ggpk.GgpkRecord.Offset + 20, SeekOrigin.Begin);
				Ggpk.Writer.Write(next.Offset);
			} else {
				previous.NextFreeOffset = next.Offset;
				Ggpk.FileStream.Seek(previous.Offset + 8, SeekOrigin.Begin);
				Ggpk.Writer.Write(next.Offset);
			}
			Ggpk.FreeRecords.Remove(node);
		}
	}
}