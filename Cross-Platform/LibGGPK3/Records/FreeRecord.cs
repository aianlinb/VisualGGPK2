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
        public long NextFreeOffset;

        public FreeRecord(int length, GGPKContainer ggpk) {
            GGPK = ggpk;
            Offset = ggpk.FileStream.Position - 8;
            Length = length;
            Read();
        }

        public FreeRecord(int length, GGPKContainer ggpk, long nextFreeOffset, long recordBegin) {
            GGPK = ggpk;
            Offset = recordBegin;
            Length = length;
            NextFreeOffset = nextFreeOffset;
        }

        protected override void Read() {
            var br = GGPK.Reader;
            NextFreeOffset = br.ReadInt64();
            br.BaseStream.Seek(Length - 16, SeekOrigin.Current);
        }

        protected internal override void Write(BinaryWriter bw = null) {
            bw ??= GGPK.Writer;
            Offset = bw.BaseStream.Position;
            bw.Write(Length);
            bw.Write(Tag);
            bw.Write(NextFreeOffset);
            bw.BaseStream.Seek(Length - 16, SeekOrigin.Current);
        }

        /// <summary>
        /// Remove this FreeRecord from the Linked FreeRecord List
        /// </summary>
        /// <param name="node">Node in <see cref="GGPKContainer.LinkedFreeRecords"/> to remove</param>
        public virtual void Remove(LinkedListNode<FreeRecord> node = null) {
            node ??= GGPK.LinkedFreeRecords.Find(this);
            var previous = node.Previous?.Value;
            var next = node.Next?.Value;
            if (next == null)
                if (previous == null) {
                    GGPK.GgpkRecord.FirstFreeRecordOffset = 0;
                    GGPK.FileStream.Seek(GGPK.GgpkRecord.Offset + 20, SeekOrigin.Begin);
                    GGPK.Writer.Write((long)0);
                } else {
                    previous.NextFreeOffset = 0;
                    GGPK.FileStream.Seek(previous.Offset + 8, SeekOrigin.Begin);
                    GGPK.Writer.Write((long)0);
                }
            else
                if (previous == null) {
                GGPK.GgpkRecord.FirstFreeRecordOffset = next.Offset;
                GGPK.FileStream.Seek(GGPK.GgpkRecord.Offset + 20, SeekOrigin.Begin);
                GGPK.Writer.Write(next.Offset);
            } else {
                previous.NextFreeOffset = next.Offset;
                GGPK.FileStream.Seek(previous.Offset + 8, SeekOrigin.Begin);
                GGPK.Writer.Write(next.Offset);
            }
            GGPK.LinkedFreeRecords.Remove(node);
        }
    }
}