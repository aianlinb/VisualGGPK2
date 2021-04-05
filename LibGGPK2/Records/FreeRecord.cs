using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibGGPK2.Records
{
    /// <summary>
    /// A free record represents space in the pack file that has been marked as deleted. It's much cheaper to just
    /// mark areas as free and append data to the end of the pack file than it is to rebuild the entire pack file just
    /// to remove a piece of data.
    /// </summary>
    public class FreeRecord : BaseRecord
    {
        public static readonly byte[] Tag = Encoding.ASCII.GetBytes("FREE");

        /// <summary>
        /// Offset of next FreeRecord
        /// </summary>
        public long NextFreeOffset;

        public FreeRecord(int length, GGPKContainer ggpk)
        {
            ggpkContainer = ggpk;
            Offset = ggpk.fileStream.Position - 8;
            Length = length;
            Read();
        }

        public FreeRecord(int length, GGPKContainer ggpk, long nextFreeOffset, long recordBegin)
        {
            ggpkContainer = ggpk;
            Offset = recordBegin;
            Length = length;
            NextFreeOffset = nextFreeOffset;
        }

        protected override void Read()
        {
            var br = ggpkContainer.Reader;
            NextFreeOffset = br.ReadInt64();
            br.BaseStream.Seek(Length - 16, SeekOrigin.Current);
        }

        internal override void Write(BinaryWriter bw = null)
        {
            bw ??= ggpkContainer.Writer;
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
        public virtual void Remove(LinkedListNode<FreeRecord> node = null)
        {
            node ??= ggpkContainer.LinkedFreeRecords.Find(this);
            var previous = node.Previous?.Value;
            var next = node.Next?.Value;
            if (next == null)
                if (previous == null)
                {
                    ggpkContainer.ggpkRecord.FirstFreeRecordOffset = 0;
                    ggpkContainer.fileStream.Seek(ggpkContainer.ggpkRecord.Offset + 20, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write((long)0);
                }
                else
                {
                    previous.NextFreeOffset = 0;
                    ggpkContainer.fileStream.Seek(previous.Offset + 8, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write((long)0);
                }
            else
                if (previous == null)
                {
                    ggpkContainer.ggpkRecord.FirstFreeRecordOffset = next.Offset;
                    ggpkContainer.fileStream.Seek(ggpkContainer.ggpkRecord.Offset + 20, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write(next.Offset);
                }
                else
                {
                    previous.NextFreeOffset = next.Offset;
                    ggpkContainer.fileStream.Seek(previous.Offset + 8, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write(next.Offset);
                }
            ggpkContainer.LinkedFreeRecords.Remove(node);
        }
    }
}