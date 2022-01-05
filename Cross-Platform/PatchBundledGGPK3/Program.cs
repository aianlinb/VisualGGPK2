using LibBundledGGPK;
using System;
using System.IO;
using System.IO.Compression;

namespace PatchGGPK3 {
	public class Program {
		public static void Main(string[] args) {
			Console.WriteLine("PatchBundledGGPK3  Copyright (C) 2021 aianlinb."); // ©
			if (args.Length != 2) {
				Console.WriteLine("Usage: PatchBundledGGPK3 <PathToGGPK> <ZipFile>");
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
			var ggpk = new BundledGGPK(args[0]);
			Console.WriteLine("Replacing files . . .");
			var zip = ZipFile.OpenRead(args[1]);

			ggpk.index.Replace(zip.Entries);
			Console.WriteLine("Done!");
		}
	}
}