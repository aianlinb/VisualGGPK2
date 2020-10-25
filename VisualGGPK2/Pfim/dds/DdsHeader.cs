using System;
using System.IO;
namespace Pfim
{
    /// <summary>
    /// Denotes the compression algorithm used in the image. Either the image
    /// is uncompressed or uses some sort of block compression. The
    /// compression used is encoded in the header of image as textual
    /// representation of itself. So a DXT1 image is encoded as "1TXD" so the
    /// enum represents these values directly
    /// </summary>
    public enum CompressionAlgorithm : uint
    {
        /// <summary>
        /// No compression was used in the image.
        /// </summary>
        None = 0,

        /// <summary>
        /// <see cref="Dxt1Dds"/>. Also known as BC1
        /// </summary>
        D3DFMT_DXT1 = 827611204,

        /// <summary>
        /// Not supported. Also known as BC2
        /// </summary>
        D3DFMT_DXT2 = 844388420,

        /// <summary>
        /// <see cref="Dxt3Dds"/>. Also known as BC2
        /// </summary>
        D3DFMT_DXT3 = 861165636,

        /// <summary>
        /// Not supported. Also known as BC3
        /// </summary>
        D3DFMT_DXT4 = 877942852,

        /// <summary>
        /// <see cref="Dxt5Dds"/>. Also known as BC3
        /// </summary>
        D3DFMT_DXT5 = 894720068,

        DX10 = 808540228,

        ATI1 = 826889281,
        BC4U = 1429488450,
        BC4S = 1395934018,

        ATI2 = 843666497,
        BC5U = 1429553986,
        BC5S = 1395999554
    }

    /// <summary>Flags to indicate which members contain valid data.</summary>
    [Flags]
    public enum DdsFlags : uint
    {
        /// <summary>
        /// Required in every .dds file.
        /// </summary>
        Caps = 0x1,

        /// <summary>
        /// Required in every .dds file.
        /// </summary>
        Height = 0x2,

        /// <summary>
        /// Required in every .dds file.
        /// </summary>
        Width = 0x4,

        /// <summary>
        /// Required when pitch is provided for an uncompressed texture.
        /// </summary>
        Pitch = 0x8,

        /// <summary>
        /// Required in every .dds file.
        /// </summary>
        PixelFormat = 0x1000,

        /// <summary>
        /// Required in a mipmapped texture.
        /// </summary>
        MipMapCount = 0x20000,

        /// <summary>
        /// Required when pitch is provided for a compressed texture.
        /// </summary>
        LinearSize = 0x80000,

        /// <summary>
        /// Required in a depth texture.
        /// </summary>
        Depth = 0x800000
    }

    /// <summary>Values which indicate what type of data is in the surface.</summary>
    [Flags]
    public enum DdsPixelFormatFlags : uint
    {
        /// <summary>
        ///     Texture contains alpha data; dwRGBAlphaBitMask contains valid data.
        /// </summary>
        AlphaPixels = 0x1,

        /// <summary>
        ///     Used in some older DDS files for alpha channel only uncompressed data (dwRGBBitCount contains the alpha channel
        ///     bitcount; dwABitMask contains valid data)
        /// </summary>
        Alpha = 0x2,

        /// <summary>
        ///     Texture contains compressed RGB data; dwFourCC contains valid data.
        /// </summary>
        Fourcc = 0x4,

        /// <summary>
        ///     Texture contains uncompressed RGB data; dwRGBBitCount and the RGB masks (dwRBitMask, dwGBitMask, dwBBitMask)
        ///     contain valid data.
        /// </summary>
        Rgb = 0x40,

        /// <summary>
        ///     Used in some older DDS files for YUV uncompressed data (dwRGBBitCount contains the YUV bit count; dwRBitMask
        ///     contains the Y mask, dwGBitMask contains the U mask, dwBBitMask contains the V mask)
        /// </summary>
        Yuv = 0x200,

        /// <summary>
        ///     Used in some older DDS files for single channel color uncompressed data (dwRGBBitCount contains the luminance
        ///     channel bit count; dwRBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel
        ///     DDS file.
        /// </summary>
        Luminance = 0x20000
    }

    /// <summary>
    /// Surface pixel format.
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb943984(v=vs.85).aspx
    /// </summary>
    public struct DdsPixelFormat
    {
        /// <summary>
        /// Structure size; set to 32 (bytes).
        /// </summary>
        public uint Size;

        /// <summary>
        /// Values which indicate what type of data is in the surface. 
        /// </summary>
        public DdsPixelFormatFlags PixelFormatFlags;

        /// <summary>
        /// Four-character codes for specifying compressed or custom formats.
        /// Possible values include: DXT1, DXT2, DXT3, DXT4, or DXT5.  A
        /// FourCC of DX10 indicates the prescense of the DDS_HEADER_DXT10
        /// extended header,  and the dxgiFormat member of that structure
        /// indicates the true format. When using a four-character code,
        /// dwFlags must include DDPF_FOURCC.
        /// </summary>
        public CompressionAlgorithm FourCC;

        /// <summary>
        /// Number of bits in an RGB (possibly including alpha) format.
        /// Valid when dwFlags includes DDPF_RGB, DDPF_LUMINANCE, or DDPF_YUV.
        /// </summary>
        public uint RGBBitCount;

        /// <summary>
        /// Red (or lumiannce or Y) mask for reading color data.
        /// For instance, given the A8R8G8B8 format, the red mask would be 0x00ff0000.
        /// </summary>
        public uint RBitMask;

        /// <summary>
        /// Green (or U) mask for reading color data.
        /// For instance, given the A8R8G8B8 format, the green mask would be 0x0000ff00.
        /// </summary>
        public uint GBitMask;

        /// <summary>
        /// Blue (or V) mask for reading color data.
        /// For instance, given the A8R8G8B8 format, the blue mask would be 0x000000ff.
        /// </summary>
        public uint BBitMask;

        /// <summary>
        /// Alpha mask for reading alpha data. 
        /// dwFlags must include DDPF_ALPHAPIXELS or DDPF_ALPHA. 
        /// For instance, given the A8R8G8B8 format, the alpha mask would be 0xff000000.
        /// </summary>
        public uint ABitMask;
    }

    /// <summary>
    /// The header that accompanies all direct draw images
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb943982(v=vs.85).aspx
    /// </summary>
    public class DdsHeader
    {
        /// <summary>
        /// Size of a Direct Draw Header in number of bytes.
        /// This does not include the magic number
        /// </summary>
        private const int SIZE = 124;

        /// <summary>
        /// The magic number is the 4 bytes that starts off every Direct Draw Surface file.
        /// </summary>
        private const uint DDS_MAGIC = 542327876;

        DdsPixelFormat pixelFormat;

        /// <summary>Create header from stream</summary>
        public DdsHeader(Stream stream, bool skipMagic = false)
        {
            headerInit(stream, skipMagic);
        }

        private unsafe void headerInit(Stream stream, bool skipMagic)
        {
            var headerSize = skipMagic ? SIZE : SIZE + 4;
            byte[] buffer = new byte[headerSize];
            Reserved1 = new uint[11];
            int bufferSize = stream.Read(buffer, 0, headerSize);
            if (bufferSize != headerSize)
            {
                throw new Exception($"Need at least {SIZE + 4} bytes for a valid DDS header");
            }

            fixed (byte* bufferPtr = buffer)
            {
                uint* workingBufferPtr = (uint*)bufferPtr;

                if (!skipMagic)
                {
                    if (*workingBufferPtr++ != DDS_MAGIC)
                        throw new Exception("Not a valid DDS");
                }

                if ((Size = *workingBufferPtr++) != SIZE)
                    throw new Exception("Not a valid header size");
                Flags = (DdsFlags)(*workingBufferPtr++);
                Height = *workingBufferPtr++;
                Width = *workingBufferPtr++;
                PitchOrLinearSize = *workingBufferPtr++;
                Depth = *workingBufferPtr++;
                MipMapCount = *workingBufferPtr++;
                fixed (uint* reservedPtr = Reserved1)
                {
                    uint* workingReservedPtr = reservedPtr;
                    for (int i = 0; i < 11; i++)
                        *workingReservedPtr++ = *workingBufferPtr++;
                }

                pixelFormat.Size = *workingBufferPtr++;
                if (pixelFormat.Size != 32)
                {
                    throw new Exception($"Expected pixel size to be 32, not: ${pixelFormat.Size}");
                }

                pixelFormat.PixelFormatFlags = (DdsPixelFormatFlags)(*workingBufferPtr++);
                pixelFormat.FourCC = (CompressionAlgorithm)(*workingBufferPtr++);
                pixelFormat.RGBBitCount = *workingBufferPtr++;
                pixelFormat.RBitMask = *workingBufferPtr++;
                pixelFormat.GBitMask = *workingBufferPtr++;
                pixelFormat.BBitMask = *workingBufferPtr++;
                pixelFormat.ABitMask = *workingBufferPtr++;

                Caps = *workingBufferPtr++;
                Caps2 = *workingBufferPtr++;
                Caps3 = *workingBufferPtr++;
                Caps4 = *workingBufferPtr++;
                Reserved2 = *workingBufferPtr++;
            }
        }

        /// <summary>
        /// Size of structure. This member must be set to 124.
        /// </summary>
        public uint Size { get; private set; }

        /// <summary>
        /// Flags to indicate which members contain valid data. 
        /// </summary>
        DdsFlags Flags { get;  set; }

        /// <summary>
        /// Surface height in pixels
        /// </summary>
        public uint Height { get; private set; }

        /// <summary>
        /// Surface width in pixels
        /// </summary>
        public uint Width { get; private set; }

        /// <summary>
        /// The pitch or number of bytes per scan line in an uncompressed texture.
        /// The total number of bytes in the top level texture for a compressed texture.
        /// </summary>
        public uint PitchOrLinearSize { get; private set; }

        /// <summary>
        /// Depth of a volume texture (in pixels), otherwise unused. 
        /// </summary>
        public uint Depth { get; private set; }

        /// <summary>
        /// Number of mipmap levels, otherwise unused.
        /// </summary>
        public uint MipMapCount { get; private set; }

        /// <summary>
        /// Unused
        /// </summary>
        public uint[] Reserved1 { get; private set; }

        /// <summary>
        /// The pixel format 
        /// </summary>
        public DdsPixelFormat PixelFormat
        {
            get => pixelFormat;
            set => pixelFormat = value;
        }

        /// <summary>
        /// Specifies the complexity of the surfaces stored.
        /// </summary>
        public uint Caps { get; private set; }

        /// <summary>
        /// Additional detail about the surfaces stored.
        /// </summary>
        public uint Caps2 { get; private set; }

        /// <summary>
        /// Unused
        /// </summary>
        public uint Caps3 { get; private set; }

        /// <summary>
        /// Unused
        /// </summary>
        public uint Caps4 { get; private set; }

        /// <summary>
        /// Unused
        /// </summary>
        public uint Reserved2 { get; private set; }
    }
}
