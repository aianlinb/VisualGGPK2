using LibBundle3;
using LibGGPK3;
using LibGGPK3.Records;

namespace LibBundledGGPK {
	public class BundledGGPK : GGPK {
		public Index index;
		public BundledGGPK(string filePath) : base(filePath) {
			var f = (FileRecord)FindNode("Bundles2/_.index.bin")!;
			index = new(new GGFileStream(f));
			index.FuncReadBundle = (br) => new(new GGFileStream((FileRecord)FindNode("Bundles2/" + br.Path + ".bundle.bin")!));
		}
	}
}