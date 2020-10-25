using LibBundle.Records;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibBundle
{
    public class IndexContainer
    {
        public BundleContainer BundleContainer;
        public BundleRecord[] Bundles;
        public FileRecord[] Files;
        public DirectoryRecord[] Directorys;
        public Dictionary<ulong, FileRecord> FindFiles = new Dictionary<ulong, FileRecord>();
        public HashSet<string> Paths = new HashSet<string>();
        public byte[] directoryBundleData;

        private static BinaryReader tmp;
        public IndexContainer(string path) : this(tmp = new BinaryReader(File.OpenRead(path)))
        {
            tmp.Close();
            tmp = null;
        }
        public IndexContainer(BinaryReader br)
        {
            BundleContainer = new BundleContainer(br);
            var data = BundleContainer.Read(br);
            data.Seek(0, SeekOrigin.Begin);
            var databr = new BinaryReader(data);

            int bundleCount = databr.ReadInt32();
            Bundles = new BundleRecord[bundleCount];
            for (int i = 0; i < bundleCount; i++)
                Bundles[i] = new BundleRecord(databr) { bundleIndex = i };

            int fileCount = databr.ReadInt32();
            Files = new FileRecord[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                var f = new FileRecord(databr);
                Files[i] = f;
                FindFiles[f.Hash] = f;
                f.bundleRecord = Bundles[f.BundleIndex];
                Bundles[f.BundleIndex].Files.Add(f);
            }

            int directoryCount = databr.ReadInt32();
            Directorys = new DirectoryRecord[directoryCount];
            for (int i = 0; i < directoryCount; i++)
                Directorys[i] = new DirectoryRecord(databr);

            var tmp = databr.BaseStream.Position;
            directoryBundleData = databr.ReadBytes((int)(databr.BaseStream.Length - tmp));
            databr.BaseStream.Seek(tmp, SeekOrigin.Begin);

            var directoryBundle = new BundleContainer(databr);
            var br2 = new BinaryReader(directoryBundle.Read(databr));
            // Array.Sort(Directorys, new Comparison<DirectoryRecord>((dr1, dr2) => { return dr1.Offset > dr2.Offset ? 1 : -1; }));
            foreach (var d in Directorys)
            {
                var temp = new List<string>();
                bool Base = false;
                br2.BaseStream.Seek(d.Offset, SeekOrigin.Begin);
                while (br2.BaseStream.Position - d.Offset <= d.Size - 4)
                {
                    int index = br2.ReadInt32();
                    if (index == 0)
                    {
                        Base = !Base;
                        if (Base)
                            temp = new List<string>();
                    }
                    else
                    {
                        index -= 1;
                        var sb = new StringBuilder();
                        char c;
                        while ((c = br2.ReadChar()) != 0)
                            sb.Append(c);
                        var str = sb.ToString();
                        if (index < temp.Count)
                            str = temp[index] + str;
                        if (Base)
                            temp.Add(str);
                        else
                        {
                            Paths.Add(str);
                            var f = FindFiles[FNV1a64Hash(str)];
                            f.path = str;
                            d.children.Add(f);
                            f.parent = d;
                        }
                    }
                }
            }
            br2.Close();
        }

        public virtual void Save(string path)
        {
            var bw = new BinaryWriter(new MemoryStream());
            bw.Write(Bundles.Length);
            foreach (var b in Bundles)
            {
                bw.Write(b.nameLength);
                bw.Write(Encoding.UTF8.GetBytes(b.Name), 0, b.nameLength);
                bw.Write(b.UncompressedSize);
            }
            bw.Write(Files.Length);
            foreach (var f in Files)
            {
                bw.Write(f.Hash);
                bw.Write(f.BundleIndex);
                bw.Write(f.Offset);
                bw.Write(f.Size);
            }
            bw.Write(Directorys.Length);
            foreach (var d in Directorys)
            {
                bw.Write(d.Hash);
                bw.Write(d.Offset);
                bw.Write(d.Size);
                bw.Write(d.RecursiveSize);
            }
            bw.Write(directoryBundleData);
            bw.Flush();

            BundleContainer.Save(bw.BaseStream, path);
            bw.Close();
        }
        public virtual byte[] Save()
        {
            using var bw = new BinaryWriter(new MemoryStream());
            bw.Write(Bundles.Length);
            foreach (var b in Bundles)
            {
                bw.Write(b.nameLength);
                bw.Write(Encoding.UTF8.GetBytes(b.Name), 0, b.nameLength);
                bw.Write(b.UncompressedSize);
            }
            bw.Write(Files.Length);
            foreach (var f in Files)
            {
                bw.Write(f.Hash);
                bw.Write(f.BundleIndex);
                bw.Write(f.Offset);
                bw.Write(f.Size);
            }
            bw.Write(Directorys.Length);
            foreach (var d in Directorys)
            {
                bw.Write(d.Hash);
                bw.Write(d.Offset);
                bw.Write(d.Size);
                bw.Write(d.RecursiveSize);
            }
            bw.Write(directoryBundleData);
            bw.Flush();

            return BundleContainer.Save(bw.BaseStream);
        }

        public BundleRecord GetSmallestBundle(IList<BundleRecord> Bundles = null)
        {
            if (Bundles == null)
                Bundles = this.Bundles;
            var result = Bundles.ElementAt(0);
            int l = Bundles[0].UncompressedSize;
            foreach (var b in Bundles)
                if (b.UncompressedSize < l)
                {
                    l = b.UncompressedSize;
                    result = b;
                }
            return result;
        }

        public static ulong FNV1a64Hash(string str)
        {
            if (str.EndsWith("/"))
            {
                str.TrimEnd(new char[] { '/' });
                str += "++";
            }
            else
                str = str.ToLower() + "++";

            var bs = Encoding.UTF8.GetBytes(str);
            ulong hash = 0xcbf29ce484222325;
            foreach (var by in bs)
                hash = (hash ^ by) * 0x100000001b3;

            return hash;
        }
    }
}