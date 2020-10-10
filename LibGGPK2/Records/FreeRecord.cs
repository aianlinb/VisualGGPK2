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
        /// Offset of next FREE record
        /// </summary>
        public long NextFreeOffset;

        public FreeRecord(int length, GGPKContainer ggpk)
        {
            ggpkContainer = ggpk;
            RecordBegin = ggpk.fileStream.Position - 8;
            Length = length;
            Read();
        }

        public FreeRecord(int length, GGPKContainer ggpk, long nextFreeOffset, long recordBegin)
        {
            ggpkContainer = ggpk;
            RecordBegin = recordBegin;
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
            if (bw == null)
                bw = ggpkContainer.Writer;
            RecordBegin = bw.BaseStream.Position;
            bw.Write(Length);
            bw.Write(Tag);
            bw.Write(NextFreeOffset);
            bw.BaseStream.Seek(Length - 16, SeekOrigin.Current);
        }

        public void Remove()
        {
            var node = ggpkContainer.LinkedFreeRecords.Find(this);
            var previous = node.Previous?.Value;
            var next = node.Next?.Value;
            if (next == null)
                if (previous == null)
                {
                    ggpkContainer.ggpkRecord.FirstFreeRecordOffset = 0;
                    ggpkContainer.fileStream.Seek(ggpkContainer.ggpkRecord.RecordBegin + 20, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write((long)0);
                }
                else
                {
                    previous.NextFreeOffset = 0;
                    ggpkContainer.fileStream.Seek(previous.RecordBegin + 8, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write((long)0);
                }
            else
                if (previous == null)
                {
                    ggpkContainer.ggpkRecord.FirstFreeRecordOffset = next.NextFreeOffset;
                    ggpkContainer.fileStream.Seek(ggpkContainer.ggpkRecord.RecordBegin + 20, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write(next.NextFreeOffset);
                }
                else
                {
                    previous.NextFreeOffset = next.NextFreeOffset;
                    ggpkContainer.fileStream.Seek(previous.RecordBegin + 8, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write(next.NextFreeOffset);
                }
            ggpkContainer.LinkedFreeRecords.Remove(node);
        }
    }
}