using System.IO;

namespace LibGGPK2.Records
{
    public abstract class BaseRecord
    {
        /// <summary>
        /// Length of the entire record in bytes
        /// </summary>
        public int Length;

        /// <summary>
        /// Offset in pack file where record begins
        /// </summary>
        public long RecordBegin;

        /// <summary>
        /// GGPK which contains this record
        /// </summary>
        public GGPKContainer ggpkContainer;

        protected abstract void Read();

        internal abstract void Write(BinaryWriter bw = null);
    }
}