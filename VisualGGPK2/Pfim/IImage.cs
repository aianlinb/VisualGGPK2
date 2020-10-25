using System;

namespace Pfim
{
    /// <summary>
    /// Defines a common interface that all images are decoded into
    /// </summary>
    public interface IImage : IDisposable
    {
        /// <summary>The raw data</summary>
        byte[] Data { get; }

        /// <summary>
        /// Length of the raw data. Unless decoding with a custom allocator
        /// this will be equivalent to `Data.Length`
        /// </summary>
        int DataLen { get; }

        /// <summary>Width of the image in pixels</summary>
        int Width { get; }

        /// <summary>Height of the image in pixels</summary>
        int Height { get; }

        /// <summary>The number of bytes that compose one line</summary>
        int Stride { get; }

        /// <summary>The number of bits that compose a pixel</summary>
        int BitsPerPixel { get; }

        /// <summary>The format of the raw data</summary>
        ImageFormat Format { get; }

        /// <summary>If the image format is compressed</summary>
        bool Compressed { get; }

        /// <summary>Decompress the image. Will have no effect if not compressed</summary>
        void Decompress();
          
        ///<summary>Apply colormap, may change data and image format</summary>
        void ApplyColorMap();

        MipMapOffset[] MipMaps { get; }
    }
}
