using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGGPK3.Records;

namespace LibGGPK3 {
	public class GGPKContainer {
        internal FileStream FileStream;
        internal BinaryReader Reader;
        internal BinaryWriter Writer;
        public readonly GGPKRecord GgpkRecord;
        public readonly DirectoryRecord RootDirectory;
        public readonly LinkedList<FreeRecord> LinkedFreeRecords;

        public GGPKContainer(string pathToGGPK) {
            // Open File
            FileStream = File.Open(pathToGGPK, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            Reader = new BinaryReader(FileStream);
            Writer = new BinaryWriter(FileStream);

            // Read ROOT Directory Record
            BaseRecord ggpk;
            while ((ggpk = GetRecord()) is not GGPKRecord);
            GgpkRecord = ggpk as GGPKRecord;
            RootDirectory = GetRecord(GgpkRecord.RootDirectoryOffset) as DirectoryRecord;
            RootDirectory.Name = "ROOT";

            // Build Linked FreeRecord List
            LinkedFreeRecords = new();
            var NextFreeOffset = GgpkRecord.FirstFreeRecordOffset;
            while (NextFreeOffset > 0) {
                FreeRecord current = GetRecord(NextFreeOffset) as FreeRecord;
                LinkedFreeRecords.AddLast(current);
                NextFreeOffset = current.NextFreeOffset;
            }
        }

        /// <summary>
        /// Read a record from GGPK at <paramref name="offset"/>
        /// </summary>
        /// <param name="offset">Record offset, null for current stream position</param>
        public virtual BaseRecord GetRecord(long? offset = null) {
            if (offset.HasValue)
                FileStream.Seek(offset.Value, SeekOrigin.Begin);
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
                throw new Exception("Invalid Record Tag: " + Encoding.UTF8.GetString(tag) + " at offset: " + (FileStream.Position - 8).ToString());
        }

        /// <summary>
        /// Find the record with a <paramref name="path"/>
        /// </summary>
        /// <param name="path">Path in GGPK under <paramref name="parent"/></param>
        /// <param name="parent">Where to start searching, null for ROOT directory in GGPK</param>
        /// <returns>null if not found</returns>
        public virtual TreeNode FindRecord(string path, TreeNode parent = null) {
            parent ??= RootDirectory;
            var SplittedPath = path.Split('/', '\\');
            foreach (var name in SplittedPath) {
                if (parent is not DirectoryRecord dr)
                    return null;
                parent = dr.GetChildItem(name);
            }
            return parent;
        }
    }
}