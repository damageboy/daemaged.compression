using System;
using System.Runtime.InteropServices;
using System.Security;
using Daemaged.Compression.Util;

namespace Daemaged.Compression.LZ4
{
  using size_t = IntPtr;


  public enum Lz4BlockSize
  {
    Default = 0,
    Max64Kb = 4,
    Max256Kb = 5,
    Max1Mb = 6,
    Max4Mb = 7
  }

  public enum Lz4BlockMode
  {
    Linked = 0,
    Independent
  }

  public enum Lz4ContentChecksum
  {
    Disabled = 0,
    Enabled
  }

  public enum Lz4AutoFlush
  {
    Disabled = 0,
    Enabled
  }

  /// <summary>
  /// Defines constants for the available compression levels in zlib
  /// </summary>
  public enum Lz4CompressionLevel
  {
    /// <summary>
    /// The default compression level with a reasonable compromise between compression and speed
    /// </summary>
    Default = -1,
    /// <summary>
    /// No compression at all. The data are passed straight through.
    /// </summary>
    None = 0,
    /// <summary>
    /// The maximum compression rate available.
    /// </summary>
    Best = 9,
    /// <summary>
    /// The fastest available compression level.
    /// </summary>
    Fastest = 1
  }

#if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  public struct Lz4FrameInfo
  {
    public Lz4BlockSize blockSize;    //0
    public Lz4BlockMode blockMode;    //8
    public Lz4ContentChecksum contentChecksum; //16
    public unsafe fixed uint reserved[5];      //24
  }

#if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  public struct Lz4Preferences
  {
    public Lz4FrameInfo frameInfo;    //0
    public Lz4CompressionLevel CompressionLevel;  //8
    public Lz4AutoFlush autoFlush; //16
    public unsafe fixed uint reserved[4];      //24
  }

#if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  public struct Lz4CompressOptions
  {
    public uint stableSrc;    //0
    public unsafe fixed uint reserved[3];      //8
  }

#if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  public struct Lz4DecompressOptions
  {
    public uint stableSrc;    //0
    public unsafe fixed uint reserved[3];      //8
  }

  public enum Lz4LibReturnCode
  {
    Ok                        = 0,
    InvalidMaxBlockSize       = -1,
    InvalidBlockMode          = -2,
    InvalidContentChecksumFlag= -3,
    InvalidCompressionLevel   = -4,
    AllocationFailed          = -5,
    SrcSizeTooLarge           = -6,
    DstSizeTooLarge           = -7,
    DecompressionFailed       = -8,
    InvalidChecksum           = -9,
    ErrorMaxCode              = -10,
  }

  [SuppressUnmanagedCodeSecurity]
  public static class LZ4Native
  {
    private static readonly object _staticSyncRoot = new object();
    private static IntPtr _nativeModulePtr;
    public const uint Lz4Version = 100;
    public const int HeaderMaxSize = 20;
    public const int MagicNumber = 0x184D2204;
    public const int KB = (1 << 10);
    public const int MB = (1 << 20);
    public const uint GB = (1U << 30);


    public static int GetBlockSize(Lz4BlockSize blockSizeType)
    {
      switch (blockSizeType)
      {
        case Lz4BlockSize.Max64Kb:
          return 64*KB;
        case Lz4BlockSize.Max256Kb:
          return 256*KB;
        case Lz4BlockSize.Max1Mb:
          return 1*MB;
        case Lz4BlockSize.Max4Mb:
          return 4*MB;
      }

      return -1;
    }

    static LZ4Native()
    {
      lock (_staticSyncRoot) {
        _nativeModulePtr = Preload.Load(LIBLZ4);
      }
    }

    internal const string LIBLZ4 = "liblz4";

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public unsafe static extern size_t LZ4F_compressFrameBound(Lz4FrameInfo* frameInfo, size_t srcSize, Lz4Preferences* preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_compressFrame(byte* dstBuffer, size_t dstMaxSize, byte* srcBuffer, size_t srcSize, Lz4Preferences *preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe size_t LZ4F_createCompressionContext(void** ctx, uint version);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe size_t LZ4F_freeCompressionContext(void* ctx);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe size_t LZ4F_compressBegin(void* compressionContext, void* dstBuffer, size_t dstMaxSize, Lz4Preferences* preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_compressBound(size_t srcSize, Lz4Preferences *preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_compressUpdate(void* compressionContext, void* dstBuffer, size_t dstMaxSize, void* srcBuffer, size_t srcSize, ref Lz4Preferences preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_flush(void* compressionContext, void* dstBuffer, size_t dstMaxSize, ref Lz4CompressOptions compressOptions);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_compressEnd(void* compressionContext, void* dstBuffer, size_t dstMaxSize, Lz4CompressOptions* compressOptions);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern bool LZ4F_isError(size_t code);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern sbyte* LZ4F_getErrorName(size_t code);

    public static unsafe string GetErrorName(size_t code) { return new String(LZ4F_getErrorName(code)); }

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_createDecompressionContext(void** decompressionContext, uint version);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_freeDecompressionContext(void* decompressionContext);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_getFrameInfo(void* decompressionContext, Lz4FrameInfo* frameInfo, void* srcBuffer, int* srcSize);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_decompress(void* decompressionContext, void* dstBuffer, size_t* dstMaxSize, void* srcBuffer, size_t* srcSize, Lz4DecompressOptions* decompressOptions);
  }
}