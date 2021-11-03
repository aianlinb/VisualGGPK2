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
        public GGPKContainer GGPK;

        /// <summary>
        /// Read the record data from GGPK
        /// </summary>
        protected abstract void Read();

        /// <summary>
        /// Write the record data to GGPK
        /// </summary>
        protected internal abstract void Write(BinaryWriter bw = null);
    }
}