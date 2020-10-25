namespace LibGGPK2.Records
{
    /// <summary>
    /// FileRecord or BundleFileNode
    /// </summary>
    public interface IFileRecord
    {
        public byte[] ReadFileContent(System.IO.Stream stream = null);
        public void ReplaceContent(byte[] NewContent);
        public enum DataFormats
        {
            Unknown,
            Image,
            Ascii,
            Unicode,
            OGG,
            Dat,
            TextureDds,
            BK2,
            BANK
        }
        public DataFormats DataFormat { get; }
    }
}
