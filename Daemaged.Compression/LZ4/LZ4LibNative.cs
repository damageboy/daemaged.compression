using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using Daemaged.Compression.GZip;

namespace Daemaged.Compression.LZ4
{
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
  public static class LZ4LibNative
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

    /* unoptimized version; solves endianess & alignment issues */
    public static unsafe void LZ4IO_writeLE32(byte* p, uint value32)
    {
      byte* dstPtr = p;
      dstPtr[0] = (byte)value32;
      dstPtr[1] = (byte)(value32 >> 8);
      dstPtr[2] = (byte)(value32 >> 16);
      dstPtr[3] = (byte)(value32 >> 24);
    }

    static LZ4LibNative()
    {
      Initialize();
    }

    private static void Initialize()
    {
      // If we're on Linux / MacOsX, just let the platform find the .so / .dylib,
      // non of our bussiness, for now
      if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        return;

      lock (_staticSyncRoot) {
        _nativeModulePtr = Preload.Load(LIBLZ4);
      }
    }

    internal const string LIBLZ4 = "liblz4";

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public unsafe static extern int LZ4F_compressFrameBound(Lz4FrameInfo *frameInfo, int srcSize, ref Lz4Preferences preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int LZ4F_compressFrame(ref Lz4FrameInfo frameInfo, byte *dstBuffer, int dstMaxSize, byte *srcBuffer, int srcSize, ref Lz4Preferences preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe Lz4LibReturnCode LZ4F_createCompressionContext(void **compressionContext, uint version);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe Lz4LibReturnCode LZ4F_freeCompressionContext(void* compressionContext);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int LZ4F_compressBegin(void* compressionContext, void* dstBuffer, int dstMaxSize, ref Lz4Preferences preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int LZ4F_compressBound(int srcSize, ref Lz4Preferences preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int LZ4F_compressUpdate(void* compressionContext, void* dstBuffer, int dstMaxSize, void* srcBuffer, int srcSize, ref Lz4Preferences preferences);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int LZ4F_flush(void* compressionContext, void* dstBuffer, int dstMaxSize, ref Lz4CompressOptions compressOptions);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int LZ4F_compressEnd(void* compressionContext, void* dstBuffer, int dstMaxSize, ref Lz4CompressOptions compressOptions);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int LZ4F_isError(int size);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern Lz4LibReturnCode LZ4F_createDecompressionContext(void** decompressionContext, uint version);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern Lz4LibReturnCode LZ4F_freeDecompressionContext(void* decompressionContext);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern Lz4LibReturnCode LZ4F_getFrameInfo(void* decompressionContext, Lz4FrameInfo *frameInfo, void* srcBuffer, int* srcSize);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int LZ4F_decompress(void* decompressionContext, void* dstBuffer, int* dstMaxSize, void* srcBuffer, int* srcSize, Lz4DecompressOptions *decompressOptions);
  }
}