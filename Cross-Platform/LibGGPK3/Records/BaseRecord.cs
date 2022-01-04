using System.IO;

namespace LibGGPK3.Records {
	/// <summary>
	/// BaseType of all records
	/// </summary>
	public abstract class BaseRecord {
		/// <summary>
		/// Length of the entire record in bytes
		/// </summary>
		public int Length;

		/// <summary>
		/// Offset in pack file where record begins
		/// </summary>
		public long Offset;

		/// <summary>
		/// GGPK which contains this record
		/// </summary>
		public GGPK Ggpk;

		protected BaseRecord(int length, GGPK ggpk) {
			Length = length;
			Ggpk = ggpk;
		}

		/// <summary>
		/// Write the record data to GGPK
		/// </summary>
		protected internal abstract void Write(Stream? writeTo = null);
	}
}