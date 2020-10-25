using Pfim.dds;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pfim
{
    public class DdsHeaderDxt10
    {
        public DxgiFormat DxgiFormat { get; }
        public D3D10ResourceDimension ResourceDimension { get; }
        public uint MiscFlag { get; }
        public uint ArraySize { get; }
        public uint MiscFlags2 { get; }

        public unsafe DdsHeaderDxt10(Stream stream)
        {
            byte[] buffer = new byte[5 * 4];
            if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new Exception($"Need at least {buffer.Length} bytes for a valid DDS DX10 header");
            }

            fixed (byte* bufferPtr = buffer)
            {
                uint* ptr = (uint*) bufferPtr;

                DxgiFormat = (DxgiFormat) (*ptr++);
                ResourceDimension = (D3D10ResourceDimension) (*ptr++);
                MiscFlag = (*ptr++);
                ArraySize = (*ptr++);
                MiscFlags2 = (*ptr);
            }
        }

        internal Dds NewDecoder(DdsHeader header, PfimConfig config)
        {
            switch (DxgiFormat)
            {
                case DxgiFormat.BC1_TYPELESS:
                case DxgiFormat.BC1_UNORM_SRGB:
                case DxgiFormat.BC1_UNORM:
                    return new Dxt1Dds(header, config);

                case DxgiFormat.BC2_TYPELESS:
                case DxgiFormat.BC2_UNORM:
                case DxgiFormat.BC2_UNORM_SRGB:
                    return new Dxt3Dds(header, config);

                case DxgiFormat.BC3_TYPELESS:
                case DxgiFormat.BC3_UNORM:
                case DxgiFormat.BC3_UNORM_SRGB:
                    return new Dxt5Dds(header, config);

                case DxgiFormat.BC4_TYPELESS:
                case DxgiFormat.BC4_UNORM:
                    return new Bc4Dds(header, config);
                case DxgiFormat.BC4_SNORM:
                    return new Bc4sDds(header, config);

                case DxgiFormat.BC5_TYPELESS:
                case DxgiFormat.BC5_UNORM:
                    return new Bc5Dds(header, config);
                case DxgiFormat.BC5_SNORM:
                    return new Bc5sDds(header, config);

                case DxgiFormat.BC6H_TYPELESS:
                case DxgiFormat.BC6H_UF16:
                case DxgiFormat.BC6H_SF16:
                    return new Bc6hDds(header, config);

                case DxgiFormat.BC7_TYPELESS:
                case DxgiFormat.BC7_UNORM:
                case DxgiFormat.BC7_UNORM_SRGB:
                    return new Bc7Dds(header, config);

                case DxgiFormat.R8G8B8A8_TYPELESS:
                case DxgiFormat.R8G8B8A8_UNORM:
                case DxgiFormat.R8G8B8A8_UNORM_SRGB:
                case DxgiFormat.R8G8B8A8_UINT:
                case DxgiFormat.R8G8B8A8_SNORM:
                case DxgiFormat.R8G8B8A8_SINT:
                    return new UncompressedDds(header, config, 32, true);
                case DxgiFormat.B8G8R8A8_TYPELESS:
                case DxgiFormat.B8G8R8A8_UNORM:
                case DxgiFormat.B8G8R8A8_UNORM_SRGB:
                    return new UncompressedDds(header, config, 32, false);

                case DxgiFormat.UNKNOWN:
                case DxgiFormat.R32G32B32A32_TYPELESS:
                case DxgiFormat.R32G32B32A32_FLOAT:
                case DxgiFormat.R32G32B32A32_UINT:
                case DxgiFormat.R32G32B32A32_SINT:
                case DxgiFormat.R32G32B32_TYPELESS:
                case DxgiFormat.R32G32B32_FLOAT:
                case DxgiFormat.R32G32B32_UINT:
                case DxgiFormat.R32G32B32_SINT:
                case DxgiFormat.R16G16B16A16_TYPELESS:
                case DxgiFormat.R16G16B16A16_FLOAT:
                case DxgiFormat.R16G16B16A16_UNORM:
                case DxgiFormat.R16G16B16A16_UINT:
                case DxgiFormat.R16G16B16A16_SNORM:
                case DxgiFormat.R16G16B16A16_SINT:
                case DxgiFormat.R32G32_TYPELESS:
                case DxgiFormat.R32G32_FLOAT:
                case DxgiFormat.R32G32_UINT:
                case DxgiFormat.R32G32_SINT:
                case DxgiFormat.R32G8X24_TYPELESS:
                case DxgiFormat.D32_FLOAT_S8X24_UINT:
                case DxgiFormat.R32_FLOAT_X8X24_TYPELESS:
                case DxgiFormat.X32_TYPELESS_G8X24_UINT:
                case DxgiFormat.R10G10B10A2_TYPELESS:
                case DxgiFormat.R10G10B10A2_UNORM:
                case DxgiFormat.R10G10B10A2_UINT:
                case DxgiFormat.R11G11B10_FLOAT:
                case DxgiFormat.R16G16_TYPELESS:
                case DxgiFormat.R16G16_FLOAT:
                case DxgiFormat.R16G16_UNORM:
                case DxgiFormat.R16G16_UINT:
                case DxgiFormat.R16G16_SNORM:
                case DxgiFormat.R16G16_SINT:
                case DxgiFormat.R32_TYPELESS:
                case DxgiFormat.D32_FLOAT:
                case DxgiFormat.R32_FLOAT:
                case DxgiFormat.R32_UINT:
                case DxgiFormat.R32_SINT:
                case DxgiFormat.R24G8_TYPELESS:
                case DxgiFormat.D24_UNORM_S8_UINT:
                case DxgiFormat.R24_UNORM_X8_TYPELESS:
                case DxgiFormat.X24_TYPELESS_G8_UINT:
                case DxgiFormat.R8G8_TYPELESS:
                case DxgiFormat.R8G8_UNORM:
                case DxgiFormat.R8G8_UINT:
                case DxgiFormat.R8G8_SNORM:
                case DxgiFormat.R8G8_SINT:
                case DxgiFormat.R16_TYPELESS:
                case DxgiFormat.R16_FLOAT:
                case DxgiFormat.D16_UNORM:
                case DxgiFormat.R16_UNORM:
                case DxgiFormat.R16_UINT:
                case DxgiFormat.R16_SNORM:
                case DxgiFormat.R16_SINT:
                case DxgiFormat.R8_TYPELESS:
                case DxgiFormat.R8_UNORM:
                case DxgiFormat.R8_UINT:
                case DxgiFormat.R8_SNORM:
                case DxgiFormat.R8_SINT:
                case DxgiFormat.A8_UNORM:
                case DxgiFormat.R1_UNORM:
                case DxgiFormat.R9G9B9E5_SHAREDEXP:
                case DxgiFormat.R8G8_B8G8_UNORM:
                case DxgiFormat.G8R8_G8B8_UNORM:
                case DxgiFormat.B8G8R8X8_UNORM:
                case DxgiFormat.R10G10B10_XR_BIAS_A2_UNORM:
                case DxgiFormat.B8G8R8X8_TYPELESS:
                case DxgiFormat.B8G8R8X8_UNORM_SRGB:
                case DxgiFormat.NV12:
                case DxgiFormat.P010:
                case DxgiFormat.P016:
                case DxgiFormat.OPAQUE_420:
                case DxgiFormat.YUY2:
                case DxgiFormat.Y210:
                case DxgiFormat.Y216:
                case DxgiFormat.NV11:
                case DxgiFormat.AI44:
                case DxgiFormat.IA44:
                case DxgiFormat.P8:
                case DxgiFormat.A8P8:
                case DxgiFormat.B4G4R4A4_UNORM:
                case DxgiFormat.P208:
                case DxgiFormat.V208:
                case DxgiFormat.V408:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
