﻿using LibBundle.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibBundle
{
    public class IndexContainer
    {
        public readonly BundleContainer BundleContainer;
        public readonly BundleRecord[] Bundles;
        public readonly FileRecord[] Files;
        public readonly DirectoryRecord[] Directories;
        public readonly Dictionary<ulong, FileRecord> FindFiles = new();
        public readonly HashSet<string> Paths = new();
        public readonly byte[] directoryBundleData;

        protected static BinaryReader tmp;
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

            var bundleCount = databr.ReadInt32();
            Bundles = new BundleRecord[bundleCount];
            for (var i = 0; i < bundleCount; i++)
                Bundles[i] = new BundleRecord(databr) { bundleIndex = i };

            var fileCount = databr.ReadInt32();
            Files = new FileRecord[fileCount];
            for (var i = 0; i < fileCount; i++)
            {
                var f = new FileRecord(databr);
                Files[i] = f;
                FindFiles[f.NameHash] = f;
                var b = Bundles[f.BundleIndex];
                f.bundleRecord = b;
                b.Files.Add(f);
            }

            var directoryCount = databr.ReadInt32();
            Directories = new DirectoryRecord[directoryCount];
            for (var i = 0; i < directoryCount; i++)
                Directories[i] = new DirectoryRecord(databr);

            var tmp = databr.BaseStream.Position;
            directoryBundleData = databr.ReadBytes((int)(databr.BaseStream.Length - tmp));
            databr.BaseStream.Seek(tmp, SeekOrigin.Begin);

            var directoryBundle = new BundleContainer(databr);
            var br2 = new BinaryReader(directoryBundle.Read(databr), Encoding.UTF8);
            // Array.Sort(Directorys, new Comparison<DirectoryRecord>((dr1, dr2) => { return dr1.Offset > dr2.Offset ? 1 : -1; }));
            foreach (var d in Directories)
            {
                var temp = new List<string>();
                var Base = false;
                br2.BaseStream.Seek(d.Offset, SeekOrigin.Begin);
                while (br2.BaseStream.Position - d.Offset <= d.Size - 4)
                {
                    var index = br2.ReadInt32();
                    if (index == 0)
                    {
                        Base = !Base;
                        if (Base) temp.Clear();
                    }
                    else
                    {
                        index -= 1;
                        var sb = new StringBuilder();
                        char c;
                        while ((c = br2.ReadChar()) != 0) sb.Append(c);
                        var str = sb.ToString();
                        if (index < temp.Count) str = temp[index] + str;
                        if (Base)
                            temp.Add(str);
                        else {
                            Paths.Add(str);
							var f = FindFiles[NameHash(str)];
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
                bw.Write(b.NameLength);
                bw.Write(Encoding.UTF8.GetBytes(b.Name), 0, b.NameLength);
                bw.Write(b.UncompressedSize);
            }

            bw.Write(Files.Length);
            foreach (var f in Files)
            {
                bw.Write(f.NameHash);
                bw.Write(f.BundleIndex);
                bw.Write(f.Offset);
                bw.Write(f.Size);
            }

            bw.Write(Directories.Length);
            foreach (var d in Directories)
            {
                bw.Write(d.NameHash);
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
                bw.Write(b.NameLength);
                bw.Write(Encoding.UTF8.GetBytes(b.Name), 0, b.NameLength);
                bw.Write(b.UncompressedSize);
            }

            bw.Write(Files.Length);
            foreach (var f in Files)
            {
                bw.Write(f.NameHash);
                bw.Write(f.BundleIndex);
                bw.Write(f.Offset);
                bw.Write(f.Size);
            }

            bw.Write(Directories.Length);
            foreach (var d in Directories)
            {
                bw.Write(d.NameHash);
                bw.Write(d.Offset);
                bw.Write(d.Size);
                bw.Write(d.RecursiveSize);
            }

            bw.Write(directoryBundleData);
            bw.Flush();

            return BundleContainer.Save(bw.BaseStream);
        }

        public virtual BundleRecord GetSmallestBundle(IList<BundleRecord> Bundles = null)
        {
            Bundles ??= this.Bundles;
            if (Bundles.Count == 0)
                return null;
            var result = Bundles[0];
            var l = Bundles[0].UncompressedSize;
            foreach (var b in Bundles)
                if (b.UncompressedSize < l)
                {
                    l = b.UncompressedSize;
                    result = b;
                }
            return result;
        }

		/// <summary>
		/// Get the hash of a file path
		/// </summary>
		public unsafe ulong NameHash(string name) {
			return Directories[0].NameHash switch {
				0xF42A94E69CFF42FE => MurmurHash64A(Encoding.UTF8.GetBytes(name.ToLowerInvariant())),
				0x07E47507B4A92E53 => FNV1a64Hash(name),
                _ => throw new("Unable to detect the namehash algorithm")
			};
		}

		/// <summary>
		/// Get the hash of a file path, <paramref name="utf8Str"/> must be lowercased
		/// </summary>
		protected static unsafe ulong MurmurHash64A(ReadOnlySpan<byte> utf8Str, ulong seed = 0x1337B33F) {
			if (utf8Str[^1] == '/') // TrimEnd('/')
				utf8Str = utf8Str[..^1];
			var length = utf8Str.Length;

			const ulong m = 0xC6A4A7935BD1E995UL;
			const int r = 47;

			ulong h = seed ^ ((ulong)length * m);

			fixed (byte* data = utf8Str) {
				ulong* p = (ulong*)data;
				int numberOfLoops = length >> 3; // div 8
				while (numberOfLoops-- != 0) {
					ulong k = *p++;
					k *= m;
					k ^= k >> r;
					k *= m;

					h ^= k;
					h *= m;
				}

				int remainingBytes = length & 0b111; // mod 8
				if (remainingBytes != 0) {
					int offset = (8 - remainingBytes) << 3; // mul 8
					h ^= *p & (0xFFFFFFFFFFFFFFFFUL >> offset);
					h *= m;
				}
			}

			h ^= h >> r;
			h *= m;
			h ^= h >> r;

			return h;
		}

		/// <summary>
		/// Get the hash of a file path with ggpk before patch 3.21.2
		/// </summary>
		protected static unsafe ulong FNV1a64Hash(string str) {
			byte[] b;
			if (str.EndsWith('/'))
				b = Encoding.UTF8.GetBytes(str, 0, str.Length - 1);
			else
				b = Encoding.UTF8.GetBytes(str.ToLowerInvariant());

			// Equals to: b.Aggregate(0xCBF29CE484222325UL, (current, by) => (current ^ by) * 0x100000001B3UL);
			var hash = 0xCBF29CE484222325UL;
			foreach (var by in b)
				hash = (hash ^ by) * 0x100000001B3UL;
			return (((hash ^ 43) * 0x100000001B3UL) ^ 43) * 0x100000001B3UL; // "++"  -->  '+'==43
		}
	}
}