using System;
using System.IO;
using System.IO.Compression;
using LibGGPK3;
using LibGGPK3.Records;

namespace PatchGGPK3 {
	public class Program {
		public static void Main(string[] args) {
			Console.WriteLine("PatchGGPK3  Copyright (C) 2021 aianlinb."); // ©
			if (args.Length != 2) {
				Console.WriteLine("Usage: PatchGGPK3 <PathToGGPK> <ZipFile>");
				return;
			}
			if (!File.Exists(args[0])) {
				Console.WriteLine("FileNotFound: " + args[0]);
				return;
			}
			if (!File.Exists(args[1])) {
				Console.WriteLine("FileNotFound: " + args[1]);
				return;
			}

			Console.WriteLine("GGPK: " + args[0]);
			Console.WriteLine("Patch file: " + args[1]);
			Console.WriteLine("Reading ggpk file . . .");
			var ggpk = new GGPKContainer(args[0]);
			Console.WriteLine("Replacing files . . .");
			var zip = ZipFile.OpenRead(args[1]);

			int successed = 0, failed = 0;
			Console.WriteLine();
			foreach (var e in zip.Entries) {
				if (e.FullName.EndsWith('/'))
					continue;
				Console.Write("Replacing " + e.FullName + " . . . ");
				if (ggpk.FindRecord(e.FullName) is not FileRecord fr) {
					++failed;
					Console.WriteLine();
					Console.WriteLine("Not found in GGPK!");
					continue;
				}
				var fs = e.Open();
				var b = new byte[e.Length];
				fs.Read(b, 0, b.Length);
				fs.Close();
				fr.ReplaceContent(b);
				++successed;
				Console.WriteLine("Done");
			}
			Console.WriteLine();
			Console.WriteLine("All finished!");
			Console.WriteLine($"Replaced {successed} files, {failed} files failed");
		}
	}
}