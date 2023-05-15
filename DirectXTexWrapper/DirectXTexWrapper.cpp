#include "DirectXTex.h"

namespace DirectXTexWrapper {
	/// <summary>
	/// Managed DXGI_FORMAT
	/// </summary>
	public enum class DXGI_FORMAT_Managed {
		DXGI_FORMAT_UNKNOWN = 0,
		DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
		DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
		DXGI_FORMAT_R32G32B32A32_UINT = 3,
		DXGI_FORMAT_R32G32B32A32_SINT = 4,
		DXGI_FORMAT_R32G32B32_TYPELESS = 5,
		DXGI_FORMAT_R32G32B32_FLOAT = 6,
		DXGI_FORMAT_R32G32B32_UINT = 7,
		DXGI_FORMAT_R32G32B32_SINT = 8,
		DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
		DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
		DXGI_FORMAT_R16G16B16A16_UNORM = 11,
		DXGI_FORMAT_R16G16B16A16_UINT = 12,
		DXGI_FORMAT_R16G16B16A16_SNORM = 13,
		DXGI_FORMAT_R16G16B16A16_SINT = 14,
		DXGI_FORMAT_R32G32_TYPELESS = 15,
		DXGI_FORMAT_R32G32_FLOAT = 16,
		DXGI_FORMAT_R32G32_UINT = 17,
		DXGI_FORMAT_R32G32_SINT = 18,
		DXGI_FORMAT_R32G8X24_TYPELESS = 19,
		DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
		DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
		DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
		DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
		DXGI_FORMAT_R10G10B10A2_UNORM = 24,
		DXGI_FORMAT_R10G10B10A2_UINT = 25,
		DXGI_FORMAT_R11G11B10_FLOAT = 26,
		DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
		DXGI_FORMAT_R8G8B8A8_UNORM = 28,
		DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
		DXGI_FORMAT_R8G8B8A8_UINT = 30,
		DXGI_FORMAT_R8G8B8A8_SNORM = 31,
		DXGI_FORMAT_R8G8B8A8_SINT = 32,
		DXGI_FORMAT_R16G16_TYPELESS = 33,
		DXGI_FORMAT_R16G16_FLOAT = 34,
		DXGI_FORMAT_R16G16_UNORM = 35,
		DXGI_FORMAT_R16G16_UINT = 36,
		DXGI_FORMAT_R16G16_SNORM = 37,
		DXGI_FORMAT_R16G16_SINT = 38,
		DXGI_FORMAT_R32_TYPELESS = 39,
		DXGI_FORMAT_D32_FLOAT = 40,
		DXGI_FORMAT_R32_FLOAT = 41,
		DXGI_FORMAT_R32_UINT = 42,
		DXGI_FORMAT_R32_SINT = 43,
		DXGI_FORMAT_R24G8_TYPELESS = 44,
		DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
		DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
		DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
		DXGI_FORMAT_R8G8_TYPELESS = 48,
		DXGI_FORMAT_R8G8_UNORM = 49,
		DXGI_FORMAT_R8G8_UINT = 50,
		DXGI_FORMAT_R8G8_SNORM = 51,
		DXGI_FORMAT_R8G8_SINT = 52,
		DXGI_FORMAT_R16_TYPELESS = 53,
		DXGI_FORMAT_R16_FLOAT = 54,
		DXGI_FORMAT_D16_UNORM = 55,
		DXGI_FORMAT_R16_UNORM = 56,
		DXGI_FORMAT_R16_UINT = 57,
		DXGI_FORMAT_R16_SNORM = 58,
		DXGI_FORMAT_R16_SINT = 59,
		DXGI_FORMAT_R8_TYPELESS = 60,
		DXGI_FORMAT_R8_UNORM = 61,
		DXGI_FORMAT_R8_UINT = 62,
		DXGI_FORMAT_R8_SNORM = 63,
		DXGI_FORMAT_R8_SINT = 64,
		DXGI_FORMAT_A8_UNORM = 65,
		DXGI_FORMAT_R1_UNORM = 66,
		DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
		DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
		DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
		DXGI_FORMAT_BC1_TYPELESS = 70,
		DXGI_FORMAT_BC1_UNORM = 71,
		DXGI_FORMAT_BC1_UNORM_SRGB = 72,
		DXGI_FORMAT_BC2_TYPELESS = 73,
		DXGI_FORMAT_BC2_UNORM = 74,
		DXGI_FORMAT_BC2_UNORM_SRGB = 75,
		DXGI_FORMAT_BC3_TYPELESS = 76,
		DXGI_FORMAT_BC3_UNORM = 77,
		DXGI_FORMAT_BC3_UNORM_SRGB = 78,
		DXGI_FORMAT_BC4_TYPELESS = 79,
		DXGI_FORMAT_BC4_UNORM = 80,
		DXGI_FORMAT_BC4_SNORM = 81,
		DXGI_FORMAT_BC5_TYPELESS = 82,
		DXGI_FORMAT_BC5_UNORM = 83,
		DXGI_FORMAT_BC5_SNORM = 84,
		DXGI_FORMAT_B5G6R5_UNORM = 85,
		DXGI_FORMAT_B5G5R5A1_UNORM = 86,
		DXGI_FORMAT_B8G8R8A8_UNORM = 87,
		DXGI_FORMAT_B8G8R8X8_UNORM = 88,
		DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
		DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
		DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
		DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
		DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
		DXGI_FORMAT_BC6H_TYPELESS = 94,
		DXGI_FORMAT_BC6H_UF16 = 95,
		DXGI_FORMAT_BC6H_SF16 = 96,
		DXGI_FORMAT_BC7_TYPELESS = 97,
		DXGI_FORMAT_BC7_UNORM = 98,
		DXGI_FORMAT_BC7_UNORM_SRGB = 99,
		DXGI_FORMAT_AYUV = 100,
		DXGI_FORMAT_Y410 = 101,
		DXGI_FORMAT_Y416 = 102,
		DXGI_FORMAT_NV12 = 103,
		DXGI_FORMAT_P010 = 104,
		DXGI_FORMAT_P016 = 105,
		DXGI_FORMAT_420_OPAQUE = 106,
		DXGI_FORMAT_YUY2 = 107,
		DXGI_FORMAT_Y210 = 108,
		DXGI_FORMAT_Y216 = 109,
		DXGI_FORMAT_NV11 = 110,
		DXGI_FORMAT_AI44 = 111,
		DXGI_FORMAT_IA44 = 112,
		DXGI_FORMAT_P8 = 113,
		DXGI_FORMAT_A8P8 = 114,
		DXGI_FORMAT_B4G4R4A4_UNORM = 115,

		DXGI_FORMAT_P208 = 130,
		DXGI_FORMAT_V208 = 131,
		DXGI_FORMAT_V408 = 132,

		DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 189,
		DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 190,

		DXGI_FORMAT_FORCE_UINT = -1
	};

	DXGI_FORMAT DecompressFormat(_In_ DXGI_FORMAT format) noexcept {
		switch (format) {
			case DXGI_FORMAT_BC1_TYPELESS:
			case DXGI_FORMAT_BC1_UNORM:
			case DXGI_FORMAT_BC2_TYPELESS:
			case DXGI_FORMAT_BC2_UNORM:
			case DXGI_FORMAT_BC3_TYPELESS:
			case DXGI_FORMAT_BC3_UNORM:
			case DXGI_FORMAT_BC7_TYPELESS:
			case DXGI_FORMAT_BC7_UNORM:
				return DXGI_FORMAT_B8G8R8A8_UNORM;

			case DXGI_FORMAT_BC1_UNORM_SRGB:
			case DXGI_FORMAT_BC2_UNORM_SRGB:
			case DXGI_FORMAT_BC3_UNORM_SRGB:
			case DXGI_FORMAT_BC7_UNORM_SRGB:
				return DXGI_FORMAT_B8G8R8A8_UNORM_SRGB;

			case DXGI_FORMAT_BC4_TYPELESS:
			case DXGI_FORMAT_BC4_UNORM:
				return DXGI_FORMAT_R8_UNORM;

			case DXGI_FORMAT_BC4_SNORM:
				return DXGI_FORMAT_R8_SNORM;

			case DXGI_FORMAT_BC5_TYPELESS:
			case DXGI_FORMAT_BC5_UNORM:
				return DXGI_FORMAT_R8G8_UNORM;

			case DXGI_FORMAT_BC5_SNORM:
				return DXGI_FORMAT_R8G8_SNORM;

			case DXGI_FORMAT_BC6H_TYPELESS:
			case DXGI_FORMAT_BC6H_UF16:
			case DXGI_FORMAT_BC6H_SF16:
				// We could use DXGI_FORMAT_R32G32B32_FLOAT here since BC6H is always Alpha 1.0,
				// but this format is more supported by viewers
				return DXGI_FORMAT_R32G32B32A32_FLOAT;

			default:
				return DXGI_FORMAT_UNKNOWN;
		}
	}

	/// <summary>
	/// Managed instance of raw image
	/// </summary>
	public value struct Image sealed {
	private:
		void Set(_In_ DirectX::ScratchImage* image) {
			if (scratchImage) {
				scratchImage->Release();
				delete scratchImage;
			}
			scratchImage = image;
			const DirectX::Image* img = image->GetImages();
			width = static_cast<int>(img->width);
			height = static_cast<int>(img->height);
			format = (DXGI_FORMAT_Managed)img->format;
			rowPitch = static_cast<int>(img->rowPitch);
			slicePitch = static_cast<int>(img->slicePitch);
			pixels = (System::IntPtr)img->pixels;
		}
	internal:
		DirectX::ScratchImage* scratchImage;
		DirectX::TexMetadata* metadata;

		HRESULT Set(_In_ DirectX::ScratchImage* image, _In_ const DirectX::TexMetadata& metadata) {
			if (!this->metadata) {
				this->metadata = new (std::nothrow) DirectX::TexMetadata;
				if (!this->metadata)
					return E_OUTOFMEMORY;
			}
			*this->metadata = metadata;
			Set(image);
			return S_OK;
		}
		void Set(_In_ DirectX::ScratchImage* image, _In_ DirectX::TexMetadata* metadata) {
			if (this->metadata)
				delete this->metadata;
			this->metadata = metadata;
			Set(image);
		}
	public:
		int width;
		int height;
		DXGI_FORMAT_Managed format;
		System::IntPtr pixels;
		int rowPitch;
		int slicePitch;
		property int dimension {
			int get() {
				if (!metadata)
					throw gcnew System::InvalidOperationException();
				return metadata->dimension - 1;
			}
		};
		bool IsPMAlpha() {
			if (!metadata)
				throw gcnew System::InvalidOperationException();
			return (metadata->miscFlags2 & DirectX::TEX_MISC2_ALPHA_MODE_MASK) == DirectX::TEX_ALPHA_MODE_PREMULTIPLIED;
		}
		bool SetPMAlpha(bool isPMAlpha) {
			if (!metadata)
				throw gcnew System::InvalidOperationException();
			return (metadata->miscFlags2 & ~DirectX::TEX_MISC2_ALPHA_MODE_MASK) | (isPMAlpha ? DirectX::TEX_ALPHA_MODE_PREMULTIPLIED : 0);
		}
		bool IsCubemap() {
			if (!metadata)
				throw gcnew System::InvalidOperationException();
			return (metadata->miscFlags & DirectX::TEX_MISC_TEXTURECUBE) != 0;
		}
		bool IsAlphaAllOpaque() {
			if (!scratchImage)
				throw gcnew System::InvalidOperationException();
			return scratchImage->IsAlphaAllOpaque();
		}

		void Release() {
			delete metadata;
			metadata = NULL;
			if (scratchImage) {
				scratchImage->Release();
				delete scratchImage;
				scratchImage = NULL;
			}
		}
	};

	_Success_(!return)
	static HRESULT GetSingleImage(_Inout_ DirectX::ScratchImage*& simage, _Inout_ DirectX::TexMetadata* metadata, _Out_ Image% image) noexcept {
		// Make single frame
		metadata->mipLevels = 1;
		metadata->arraySize = 1;
		metadata->depth = 1;
		metadata->miscFlags &= ~DirectX::TEX_MISC_TEXTURECUBE;

		if (DirectX::IsTypeless(metadata->format)) {
			metadata->format = metadata->format == DXGI_FORMAT_R32G32B32A32_TYPELESS ? DXGI_FORMAT_R32G32B32A32_FLOAT : DirectX::MakeTypelessUNORM(metadata->format);
			simage->OverrideFormat(metadata->format);
		}

		if (DirectX::IsPlanar(metadata->format)) {
			auto timage = new (std::nothrow) DirectX::ScratchImage();
			if (!timage)
				return E_OUTOFMEMORY;

			HRESULT hr = DirectX::ConvertToSinglePlane(simage->GetImages(), /*simage->GetImageCount()*/ 1, *metadata, *timage);
			if (FAILED(hr)) {
				timage->Release();
				delete timage;
				return hr;
			}

			*metadata = timage->GetMetadata();
			simage->Release();
			delete simage;
			simage = timage;
		}

		// Undo Premultiplied Alpha
		if (DirectX::HasAlpha(metadata->format) && metadata->format != DXGI_FORMAT_A8_UNORM
			&& metadata->GetAlphaMode() != DirectX::TEX_ALPHA_MODE_STRAIGHT && metadata->IsPMAlpha()) {
			auto timage = new (std::nothrow) DirectX::ScratchImage();
			if (!timage)
				return E_OUTOFMEMORY;

			HRESULT hr = DirectX::PremultiplyAlpha(simage->GetImages(), /*simage->GetImageCount()*/ 1, *metadata, DirectX::TEX_PMALPHA_REVERSE, *timage);
			if (FAILED(hr)) {
				timage->Release();
				delete timage;
				return hr;
			}

			*metadata = timage->GetMetadata();
			simage->Release();
			delete simage;
			simage = timage;
		}

		if (DirectX::IsCompressed(metadata->format)) {
			auto timage = new (std::nothrow) DirectX::ScratchImage;
			if (!timage)
				return E_OUTOFMEMORY;

			DXGI_FORMAT dformat = DecompressFormat(metadata->format);
			if (dformat == DXGI_FORMAT_UNKNOWN)
				return E_FAIL;
			HRESULT hr = DirectX::Decompress(simage->GetImages(), /*simage->GetImageCount()*/ 1, *metadata, dformat, *timage);
			if (FAILED(hr)) {
				timage->Release();
				delete timage;
				return hr;
			}

			*metadata = timage->GetMetadata();
			simage->Release();
			delete simage;
			simage = timage;
		}

		if (DirectX::HasAlpha(metadata->format) && metadata->format != DXGI_FORMAT_A8_UNORM) {
			if (simage->IsAlphaAllOpaque()) {
				metadata->SetAlphaMode(DirectX::TEX_ALPHA_MODE_OPAQUE);
			} else if (metadata->GetAlphaMode() == DirectX::TEX_ALPHA_MODE_UNKNOWN) {
				metadata->SetAlphaMode(DirectX::TEX_ALPHA_MODE_STRAIGHT);
			}
		} else {
			metadata->SetAlphaMode(DirectX::TEX_ALPHA_MODE_UNKNOWN);
		}

		image.Set(simage, metadata);
		return S_OK;
	}

	/// <summary>
	/// Managed wrapper of unmanaged memory
	/// </summary>
	public ref struct BLOB sealed {
	private:
		DirectX::Blob* blob;
		!BLOB() {
			this->~BLOB();
		}
	internal:
		BLOB(DirectX::Blob* blob) {
			this->blob = blob;
		}
	public:
		property void* Pointer {
			void* get() {
				return blob->GetBufferPointer();
			}
		}
		property int Length {
			int get() {
				return static_cast<int>(blob->GetBufferSize());
			}
		}
		~BLOB() {
			blob->Release();
			delete blob;
			blob = NULL;
		}
	};

	/// <summary>
	/// Main implement of DirectXTexWrapper
	/// </summary>
	public ref class DirectXTex abstract sealed {
	public:
		/// <summary>
		/// Get bits per pixel of the image with given format
		/// </summary>
		static int BitsPerPixel(_In_ DXGI_FORMAT_Managed format) {
			return static_cast<int>(DirectX::BitsPerPixel((DXGI_FORMAT)format));
		}

		_Success_(!return)
		/// <summary>
		/// Read header of dds only, but not pixels
		/// </summary>
		static HRESULT LoadDDSHeader(_In_reads_bytes_(size) const void* pSource, _In_ const int size, _Out_ Image% image) {
			auto metadata = new (std::nothrow) DirectX::TexMetadata;
			if (!metadata)
				return E_OUTOFMEMORY;
			HRESULT hr = DirectX::GetMetadataFromDDSMemory(pSource, size, DirectX::DDS_FLAGS_ALLOW_LARGE_FILES, *metadata);
			if (FAILED(hr))
				return hr;
			image.scratchImage = NULL;
			image.metadata = metadata;
			image.width = static_cast<int>(metadata->width);
			image.height = static_cast<int>(metadata->height);
			image.format = (DXGI_FORMAT_Managed)metadata->format;
			image.rowPitch = 0;
			image.slicePitch = 0;
			image.pixels = System::IntPtr::Zero;
			return S_OK;
		}

		_Success_(!return)
		/// <summary>
		/// Read a dds file from memory
		/// </summary>
		static HRESULT LoadDDSSingleFrame(_In_reads_bytes_(size) const void* pSource, _In_ const int size, _Out_ Image% image) {
			auto metadata = new (std::nothrow) DirectX::TexMetadata;
			if (!metadata)
				return E_OUTOFMEMORY;
			auto simage = new (std::nothrow) DirectX::ScratchImage;
			if (!simage)
				return E_OUTOFMEMORY;
			HRESULT hr = DirectX::LoadFromDDSMemory(pSource, size, DirectX::DDS_FLAGS_ALLOW_LARGE_FILES, metadata, *simage);
			if (FAILED(hr))
				return hr;
			return GetSingleImage(simage, metadata, image);
		}

		_Success_(!return)
		/// <summary>
		/// Read a dds file from disk
		/// </summary>
		static HRESULT LoadDDSSingleFrame(_In_ const wchar_t* szFile, _Out_ Image% image) {
			auto metadata = new (std::nothrow) DirectX::TexMetadata;
			if (!metadata)
				return E_OUTOFMEMORY;
			auto simage = new (std::nothrow) DirectX::ScratchImage;
			if (!simage)
				return E_OUTOFMEMORY;
			HRESULT hr = DirectX::LoadFromDDSFile(szFile, DirectX::DDS_FLAGS_ALLOW_LARGE_FILES, metadata, *simage);
			if (FAILED(hr))
				return hr;
			return GetSingleImage(simage, metadata, image);
		}

		_Success_(!return)
		/// <summary>
		/// Convert a image to the given format
		/// </summary>
		static HRESULT Convert(_Inout_ Image% image, _In_ DXGI_FORMAT_Managed format) {
			if (image.format == format)
				return S_OK;

			auto timage = new (std::nothrow) DirectX::ScratchImage;
			if (!timage)
				return E_OUTOFMEMORY;

			HRESULT hr = DirectX::Convert(image.scratchImage->GetImages(), 1, *image.metadata, (DXGI_FORMAT)format, DirectX::TEX_FILTER_DEFAULT, DirectX::TEX_THRESHOLD_DEFAULT, *timage);
			if (FAILED(hr)) {
				timage->Release();
				delete timage;
				return hr;
			}

			image.Set(timage, timage->GetMetadata());
			return S_OK;
		}

		/*  This will make strange loss images
		_Success_(!return)
		static HRESULT SavePng(_In_ Image image, _In_ const wchar_t* szFile) {
			return DirectX::SaveToWICFile(*image.scratchImage->GetImages(), DirectX::WIC_FLAGS_ALLOW_MONO, DirectX::GetWICCodec(DirectX::WIC_CODEC_PNG), szFile);
		}
		*/

		_Success_(!return)
		/// <summary>
		/// Save image to a dds file
		/// </summary>
		static HRESULT SaveDds(_Inout_ Image% image, _Out_[System::Runtime::InteropServices::Out] BLOB^% buffer) {
			const void* p = image.pixels.ToPointer();
			if (!image.rowPitch || !image.slicePitch || !p)
				return ERROR_BAD_ARGUMENTS;
			DirectX::Image dImage {
				static_cast<size_t>(image.width),
				static_cast<size_t>(image.height),
				(DXGI_FORMAT)image.format,
				static_cast<size_t>(image.rowPitch),
				static_cast<size_t>(image.slicePitch),
				(uint8_t*)p
			};
			auto timage = new (std::nothrow) DirectX::ScratchImage();
			if (!timage)
				return E_OUTOFMEMORY;
			HRESULT hr = DirectX::GenerateMipMaps(dImage, DirectX::TEX_FILTER_DEFAULT, 0, *timage, true);
			if (FAILED(hr)) {
				timage->Release();
				delete timage;
				return hr;
			}

			auto metadata = new (std::nothrow) DirectX::TexMetadata;
			if (!metadata) {
				timage->Release();
				delete timage;
				return E_OUTOFMEMORY;
			}
			*metadata = timage->GetMetadata();
			auto blob = new (std::nothrow) DirectX::Blob;
			if (!blob) {
				timage->Release();
				delete timage;
				return E_OUTOFMEMORY;
			}
			hr = blob->Initialize(timage->GetPixelsSize());
			if (FAILED(hr)) {
				timage->Release();
				delete timage;
				blob->Release();
				delete blob;
				return hr;
			}
			buffer = gcnew BLOB(blob);
			hr = DirectX::SaveToDDSMemory(timage->GetImages(), timage->GetImageCount(), *metadata, DirectX::DDS_FLAGS_ALLOW_LARGE_FILES, *blob);
			if (FAILED(hr)) {
				timage->Release();
				delete timage;
				blob->Release();
				delete blob;
				return hr;
			}

			image.Set(timage, metadata);
			return hr;
		}
	};
}