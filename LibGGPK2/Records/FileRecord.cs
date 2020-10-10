using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace LibGGPK2.Records
{
    public class FileRecord : RecordTreeNode
    {
        public static readonly byte[] Tag = Encoding.ASCII.GetBytes("FILE");
        public static readonly SHA256 Hash256 = SHA256.Create();

        /// <summary>
        /// Offset in pack file where the raw data begins
        /// </summary>
        public long DataBegin;
        /// <summary>
        /// Length of the raw file data
        /// </summary>
        public int DataLength;
        /// <summary>
        /// Directory this file resides in
        /// </summary>

        public FileRecord(int length, GGPKContainer ggpk)
        {
            ggpkContainer = ggpk;
            RecordBegin = ggpk.fileStream.Position - 8;
            Length = length;
            Read();
        }

        public override DirectoryRecord Parent { get; internal set; }

        protected override void Read()
        {
            var br = ggpkContainer.Reader;
            var nameLength = br.ReadInt32();
            Hash = br.ReadBytes(32);
            Name = Encoding.Unicode.GetString(br.ReadBytes(2 * (nameLength - 1)));
            br.BaseStream.Seek(2, SeekOrigin.Current); // Null terminator
            DataBegin = br.BaseStream.Position;
            DataLength = Length - (nameLength * 2 + 44); //Length - (8 + nameLength * 2 + 32 + 4)
            br.BaseStream.Seek(DataLength, SeekOrigin.Current);
        }

        internal override void Write(BinaryWriter bw = null)
        {
            if (bw == null)
                bw = ggpkContainer.Writer;
            RecordBegin = bw.BaseStream.Position;
            bw.Write(Length);
            bw.Write(Tag);
            bw.Write(Name.Length + 1);
            bw.Write(Hash);
            bw.Write(Encoding.Unicode.GetBytes(Name));
            bw.Write((short)0); // Null terminator
            DataBegin = bw.BaseStream.Position;
            // Actual file content writing of FileRecord isn't here
        }

        public virtual byte[] ReadFileContent()
        {
            var buffer = new byte[DataLength];
            ggpkContainer.fileStream.Seek(DataBegin, SeekOrigin.Begin);
            ggpkContainer.Reader.Read(buffer, 0, (int)DataLength);
            return buffer;
        }

        public virtual void ReplaceContent(byte[] NewContent)
        {
            var bw = ggpkContainer.Writer;

            Hash = Hash256.ComputeHash(NewContent);

            if (NewContent.Length == DataLength)
            {
                bw.BaseStream.Seek(DataBegin, SeekOrigin.Begin);
                bw.Write(NewContent);
            }
            else
            {
                var oldOffset = RecordBegin;
                MarkAsFreeRecord();
                DataLength = NewContent.Length;
                Length = 44 + (Name.Length + 1) * 2 + DataLength;  //(8 + (Name + "\0").Length * 2 + 32 + 4) + DataLength

                LinkedListNode<FreeRecord> bestNode = null;
                var currentNode = ggpkContainer.LinkedFreeRecords.First;
                int space = int.MaxValue;
                do
                {
                    if (currentNode.Value.Length == Length)
                    {
                        bestNode = currentNode;
                        space = 0;
                        break;
                    }
                    int tmpSpace = currentNode.Value.Length - Length;
                    if (tmpSpace < space && /*For old libggpk =>*/ tmpSpace >= 16 /*<= For old libggpk */)
                    {
                        bestNode = currentNode;
                        space = tmpSpace;
                    }
                } while ((currentNode = currentNode.Next) != null);

                if (bestNode == null)
                {
                    bw.BaseStream.Seek(0, SeekOrigin.End);
                    Write();
                    DataBegin = bw.BaseStream.Position;
                    bw.Write(NewContent);
                }
                else
                {
                    FreeRecord f = bestNode.Value;
                    bw.BaseStream.Seek(f.RecordBegin, SeekOrigin.Begin);
                    Write();
                    bw.Write(NewContent);
                    f.Length = space;
                    f.Write();
                }

                UpdateOffset(oldOffset);
            }
        }

        public virtual void MarkAsFreeRecord()
        {
            var bw = ggpkContainer.Writer;
            bw.BaseStream.Seek(RecordBegin, SeekOrigin.Begin);
            var free = new FreeRecord(Length, ggpkContainer, 0, RecordBegin);
            free.Write();
            var lastFree = ggpkContainer.LinkedFreeRecords.Last?.Value;
            if (lastFree == null)
            {
                ggpkContainer.ggpkRecord.FirstFreeRecordOffset = RecordBegin;
                ggpkContainer.fileStream.Seek(ggpkContainer.ggpkRecord.RecordBegin + 20, SeekOrigin.Begin);
                ggpkContainer.Writer.Write(RecordBegin);
            }
            else
            {
                lastFree.NextFreeOffset = RecordBegin;
                ggpkContainer.fileStream.Seek(lastFree.RecordBegin + 8, SeekOrigin.Begin);
                ggpkContainer.Writer.Write(RecordBegin);
            }
            ggpkContainer.LinkedFreeRecords.AddLast(free);
        }

        public virtual void UpdateOffset(long OldOffset)
        {
            for (int i=0; i< Parent.Entries.Length; i++)
            {
                if (Parent.Entries[i].Offset == OldOffset)
                {
                    Parent.Entries[i].Offset = RecordBegin;
                    ggpkContainer.fileStream.Seek(Parent.EntriesBegin + i * 12 + 4, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write(RecordBegin);
                    return;
                }
            }
            //Not Found Entry??
        }
    }
}