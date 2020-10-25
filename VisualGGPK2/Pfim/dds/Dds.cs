using System;
using System.IO;
using Pfim.dds;

namespace Pfim
{
    /// <summary>
    /// Class that represents direct draw surfaces
    /// </summary>
    public abstract class Dds : IImage
    {
        private readonly PfimConfig _config;

        /// <summary>
        /// Instantiates a direct draw surface image from a header, the data,
        /// and additional info.
        /// </summary>
        protected Dds(DdsHeader header, PfimConfig config)
        {
            Header = header;
            _config = config;
        }

        protected PfimConfig Config => _config;
        public DdsHeader Header { get; }
        public abstract int BitsPerPixel { get; }
        public int BytesPerPixel => BitsPerPixel / 8;
        public virtual int Stride => Util.Stride((int)Header.Width, BitsPerPixel);
        public virtual byte[] Data { get; protected set; }
        public int DataLen { get; protected set; }
        public int Width => (int)Header.Width;
        public int Height => (int)Header.Height;
        public abstract ImageFormat Format { get; }
        public abstract bool Compressed { get; }
        public abstract void Decompress();
        public DdsHeaderDxt10 Header10 { get; private set; }

        public static Dds Create(byte[] data, PfimConfig config)
        {
            return Create(Util.CreateExposed(data), config);
        }

        /// <summary>Create a direct draw image from a stream</summary>
        public static Dds Create(Stream stream, PfimConfig config)
        {
            DdsHeader header = new DdsHeader(stream);
            return DecodeDds(stream, config, header);
        }

        /// <summary>
        /// Same as a regular create except assumes that the magic number has already been consumed
        /// </summary>
        internal static IImage CreateSkipMagic(Stream stream, PfimConfig config)
        {
            DdsHeader header = new DdsHeader(stream, true);
            return DecodeDds(stream, config, header);
        }

        private static Dds DecodeDds(Stream stream, PfimConfig config, DdsHeader header)
        {
            Dds dds;
            switch (header.PixelFormat.FourCC)
            {
                case CompressionAlgorithm.D3DFMT_DXT1:
                    dds = new Dxt1Dds(header, config);
                    break;

                case CompressionAlgorithm.D3DFMT_DXT2:
                case CompressionAlgorithm.D3DFMT_DXT4:
                    throw new ArgumentException("Cannot support DXT2 or DXT4");
                case CompressionAlgorithm.D3DFMT_DXT3:
                    dds = new Dxt3Dds(header, config);
                    break;

                case CompressionAlgorithm.D3DFMT_DXT5:
                    dds = new Dxt5Dds(header, config);
                    break;

                case CompressionAlgorithm.None:
                    dds = new UncompressedDds(header, config);
                    break;

                case CompressionAlgorithm.DX10:
                    var header10 = new DdsHeaderDxt10(stream);
                    dds = header10.NewDecoder(header, config);
                    dds.Header10 = header10;
                    break;

                case CompressionAlgorithm.ATI1:
                case CompressionAlgorithm.BC4U:
                    dds = new Bc4Dds(header, config);
                    break;
                case CompressionAlgorithm.BC4S:
                    dds = new Bc4sDds(header, config);
                    break;

                case CompressionAlgorithm.ATI2:
                case CompressionAlgorithm.BC5U:
                    dds = new Bc5Dds(header, config);
                    break;
                case CompressionAlgorithm.BC5S:
                    dds = new Bc5sDds(header, config);
                    break;

                default:
                    throw new ArgumentException($"FourCC: {header.PixelFormat.FourCC} not supported.");
            }

            dds.Decode(stream, config);
            return dds;
        }

        protected abstract void Decode(Stream stream, PfimConfig config);

        public void ApplyColorMap()
        {
        }

        public abstract MipMapOffset[] MipMaps { get; }

        public void Dispose()
        {
            _config.Allocator.Return(Data);
        }
    }
}
