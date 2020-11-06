using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using static LibGGPK2.Records.IFileRecord;

namespace LibGGPK2.Records
{
    /// <summary>
    /// Record contains the data of a file.
    /// </summary>
    public class FileRecord : RecordTreeNode, IFileRecord
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
        /// There is no child with a file
        /// </summary>
        public override SortedSet<RecordTreeNode> Children => null;

        public FileRecord(int length, GGPKContainer ggpk)
        {
            ggpkContainer = ggpk;
            Offset = ggpk.fileStream.Position - 8;
            Length = length;
            Read();
        }

        protected override void Read()
        {
            var br = ggpkContainer.Reader;
            var nameLength = br.ReadInt32();
            Hash = br.ReadBytes(32);
            Name = Encoding.Unicode.GetString(br.ReadBytes(2 * (nameLength - 1)));
            br.BaseStream.Seek(2, SeekOrigin.Current); // Null terminator
            DataBegin = br.BaseStream.Position;
            DataLength = Length - (nameLength * 2 + 44); // Length - (8 + nameLength * 2 + 32 + 4)
            br.BaseStream.Seek(DataLength, SeekOrigin.Current);
        }

        internal override void Write(BinaryWriter bw = null)
        {
            if (bw == null)
                bw = ggpkContainer.Writer;
            Offset = bw.BaseStream.Position;
            bw.Write(Length);
            bw.Write(Tag);
            bw.Write(Name.Length + 1);
            bw.Write(Hash);
            bw.Write(Encoding.Unicode.GetBytes(Name));
            bw.Write((short)0); // Null terminator
            DataBegin = bw.BaseStream.Position;
            // Actual file content writing of FileRecord isn't here
        }

        /// <summary>
        /// Get the file content of this record
        /// </summary>
        /// <param name="stream">Stream of GGPK file</param>
        public virtual byte[] ReadFileContent(Stream stream = null)
        {
            var buffer = new byte[DataLength];
            if (stream == null)
                stream = ggpkContainer.fileStream;
            stream.Seek(DataBegin, SeekOrigin.Begin);
            stream.Read(buffer, 0, DataLength);
            return buffer;
        }

        /// <summary>
        /// Replace the file content with a new content,
        /// and move the record to the FreeRecord with most suitable size.
        /// </summary>
        public virtual void ReplaceContent(byte[] NewContent)
        {
            var bw = ggpkContainer.Writer;

            Hash = Hash256.ComputeHash(NewContent); // New Hash

            if (NewContent.Length == DataLength) // Replace in situ
            {
                bw.BaseStream.Seek(DataBegin, SeekOrigin.Begin);
                bw.Write(NewContent);
            }
            else // Replace a FreeRecord
            {
                var oldOffset = Offset;
                MarkAsFreeRecord();
                DataLength = NewContent.Length;
                Length = 44 + (Name.Length + 1) * 2 + DataLength; // (8 + (Name + "\0").Length * 2 + 32 + 4) + DataLength

                LinkedListNode<FreeRecord> bestNode = null; // Find the FreeRecord with most suitable size
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
                    if (tmpSpace < space && tmpSpace >= 16)
                    {
                        bestNode = currentNode;
                        space = tmpSpace;
                    }
                } while ((currentNode = currentNode.Next) != null);

                if (bestNode == null)
                {
                    bw.BaseStream.Seek(0, SeekOrigin.End); // Write to the end of GGPK
                    Write();
                    DataBegin = bw.BaseStream.Position;
                    bw.Write(NewContent);
                }
                else
                {
                    FreeRecord free = bestNode.Value;
                    bw.BaseStream.Seek(free.Offset + free.Length - Length, SeekOrigin.Begin); // Write to the FreeRecord
                    Write();
                    DataBegin = bw.BaseStream.Position;
                    bw.Write(NewContent);
                    free.Length = space;
                    if (space >= 16) // Update offset of FreeRecord
                    {
                        bw.BaseStream.Seek(free.Offset, SeekOrigin.Begin);
                        bw.Write(free.Length);
                    }
                    else // Remove the FreeRecord
                        free.Remove(bestNode);
                }

                UpdateOffset(oldOffset); // Update the offset of FileRecord in Parent.Entries/>
                bw.Flush();
            }
        }

        /// <summary>
        /// Set the record to a FreeRecord
        /// </summary>
        public virtual void MarkAsFreeRecord()
        {
            var bw = ggpkContainer.Writer;
            bw.BaseStream.Seek(Offset, SeekOrigin.Begin);
            var free = new FreeRecord(Length, ggpkContainer, 0, Offset);
            free.Write();
            var lastFree = ggpkContainer.LinkedFreeRecords.Last?.Value;
            if (lastFree == null) // No FreeRecord
            {
                ggpkContainer.ggpkRecord.FirstFreeRecordOffset = Offset;
                ggpkContainer.fileStream.Seek(ggpkContainer.ggpkRecord.Offset + 20, SeekOrigin.Begin);
                ggpkContainer.Writer.Write(Offset);
            }
            else
            {
                lastFree.NextFreeOffset = Offset;
                ggpkContainer.fileStream.Seek(lastFree.Offset + 8, SeekOrigin.Begin);
                ggpkContainer.Writer.Write(Offset);
            }
            ggpkContainer.LinkedFreeRecords.AddLast(free);
        }

        /// <summary>
        /// Update the offset of the record in <see cref="DirectoryRecord.Entries"/>
        /// </summary>
        /// <param name="OldOffset">The original offset to be update</param>
        public virtual void UpdateOffset(long OldOffset)
        {
            var Parent = this.Parent as DirectoryRecord;
            for (int i=0; i< Parent.Entries.Length; i++)
            {
                if (Parent.Entries[i].Offset == OldOffset)
                {
                    Parent.Entries[i].Offset = Offset;
                    ggpkContainer.fileStream.Seek(Parent.EntriesBegin + i * 12 + 4, SeekOrigin.Begin);
                    ggpkContainer.Writer.Write(Offset);
                    return;
                }
            }
            throw new System.Exception(GetPath() + " updateOffset faild:" + OldOffset.ToString() + " => " + Offset.ToString());
        }

        private DataFormats? _DataFormat = null;
        /// <summary>
        /// Content data format of this file
        /// </summary>
        public virtual DataFormats DataFormat
        {
            get
            {
                if (_DataFormat == null)
                {
                    switch (Path.GetExtension(Name).ToLower())
                    {
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
                        case ".fmt":
                        case ".fxgraph":
                        case ".gft":
                        case ".gt": // Ground Types
                        case ".idl":
                        case ".idt":
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
                        case ".trl": // Trace log?
                        case ".tsi":
                        case ".tst":
                        case ".txt":
                        case ".ui": // User Interface
                        case ".xml":
                            _DataFormat = DataFormats.Unicode;
                            break;
                        case ".ast":
                        case ".csv":
                        case ".filter": // Item/loot filter
                        case ".fx": // Shader
                        case ".hlsl": // Shader
                        case ".mel": // Maya Embedded Language
                        case ".mtp":
                        case ".properties":
                        case ".slt":
                        case ".smd":
                            _DataFormat = DataFormats.Ascii;
                            break;
                        case ".dat":
                        case ".dat64":
                            _DataFormat = DataFormats.Dat;
                            break;
                        case ".dds":
                            _DataFormat = DataFormats.TextureDds;
                            break;
                        case ".jpg":
                        case ".png":
                            _DataFormat = DataFormats.Image;
                            break;
                        case ".ogg":
                            _DataFormat = DataFormats.OGG;
                            break;
                        case ".bk2":
                            _DataFormat = DataFormats.BK2;
                            break;
                        case ".bank":
                            _DataFormat = DataFormats.BANK;
                            break;
                        default:
                            _DataFormat = DataFormats.Unknown;
                            break;
                    }
                }
                return _DataFormat.Value;
            }
        }
    }
}