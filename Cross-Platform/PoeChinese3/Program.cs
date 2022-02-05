using LibBundledGGPK;
using LibDat2;
using System;
using System.IO;

#nullable disable
namespace PoeChinese3 {
	public class Program {
		public static void Main(string[] args) {
			Console.WriteLine("PoeChinese3  Copyright (C) 2022 aianlinb."); // ©
			Console.WriteLine();
			if (args.Length == 0) {
				args = new string[1];
				Console.Write("Path To GGPK: ");
				args[0] = Console.ReadLine()!;
				Console.WriteLine();
			}
			if (!File.Exists(args[0])) {
				Console.WriteLine("File Not Found: " + args[0]);
				Console.WriteLine("Enter to exit . . .");
				Console.ReadLine();
				return;
			}

			Console.WriteLine("GGPK: " + args[0]);
			Console.WriteLine("Reading ggpk file . . .");
			var ggpk = new BundledGGPK(args[0]);
			Console.WriteLine("Modifying . . .");

			if (!ggpk.index.TryGetFile("Data/Languages.dat", out var lang))
				throw new("Cannot find file: Data/Languages.dat");
			var dat = new DatContainer(lang.Read().ToArray(), "Languages.dat");
			int frn = 1, tch = 6;
			for (var i = 0; i < dat.FieldDatas.Count; ++i) {
				var s = (string)dat.FieldDatas[i][1].Value;
				if (s == "French")
					frn = i;
				else if (s == "Traditional Chinese")
					tch = i;
			}
			var rowFrn = dat.FieldDatas[frn];
			var rowTch = dat.FieldDatas[tch];
			var tmp = rowFrn[1];
			rowFrn[1] = rowTch[1];
			rowTch[1] = tmp;
			tmp = rowFrn[2];
			rowFrn[2] = rowTch[2];
			rowTch[2] = tmp;
			lang.Write(dat.Save(false, false)); // also saved the index

			Console.WriteLine("Done!");
			Console.WriteLine();
			Console.WriteLine("Enter to exit . . .");
			Console.ReadLine();
		}
	}
}