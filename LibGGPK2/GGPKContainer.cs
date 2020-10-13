using LibGGPK2.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibGGPK2
{
    public class GGPKContainer : IDisposable
    {
        public readonly FileStream fileStream;
        public readonly BinaryReader Reader;
        public readonly BinaryWriter Writer;
        public readonly GGPKRecord ggpkRecord;
        public readonly DirectoryRecord rootDirectory;
        public readonly LinkedList<FreeRecord> LinkedFreeRecords = new LinkedList<FreeRecord>();

        public GGPKContainer(string path)
        {
            fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Reader = new BinaryReader(fileStream);
            Writer = new BinaryWriter(fileStream);

            BaseRecord ggpk;
            while (!((ggpk = GetRecord()) is GGPKRecord));
            ggpkRecord = ggpk as GGPKRecord;
            rootDirectory = GetRecord(ggpkRecord.RootDirectoryOffset) as DirectoryRecord;

            long NextFreeOffset = ggpkRecord.FirstFreeRecordOffset;
            while (NextFreeOffset > 0)
            {
                FreeRecord current = GetRecord(NextFreeOffset) as FreeRecord;
                LinkedFreeRecords.AddLast(current);
                NextFreeOffset = current.NextFreeOffset;
            }
        }

        public BaseRecord GetRecord(long? offset = null)
        {
            if (offset.HasValue)
                fileStream.Seek(offset.Value, SeekOrigin.Begin);
            var length = Reader.ReadInt32();
            var tag = Reader.ReadBytes(4);
            if (tag.SequenceEqual(FileRecord.Tag))
                return new FileRecord(length, this);
            else if (tag.SequenceEqual(FreeRecord.Tag))
                return new FreeRecord(length, this);
            else if (tag.SequenceEqual(DirectoryRecord.Tag))
                return new DirectoryRecord(length, this);
            else if (tag.SequenceEqual(GGPKRecord.Tag))
                return new GGPKRecord(length, this);
            else
                throw new Exception("Invalid Record Tag: " + Encoding.ASCII.GetString(tag));
        }

        public async Task SaveAsync(string pathToNewGGPK)
        {
            throw new NotImplementedException();
            var bw = new BinaryWriter(File.OpenWrite(pathToNewGGPK));
            new GGPKRecord().Write(bw);
            //TODO
            bw.Close();
        }

        public void Dispose()
        {
            Writer.Flush();
            Writer.Close();
            Reader.Close();
        }
    }
}