using System;
using System.Runtime.InteropServices;
using System.Security;
using static Daemaged.Compression.Util.NativePreloadHelper;

namespace Daemaged.Compression.LZ4
{
  using size_t = IntPtr;


  public enum LZ4BlockSize
  {
    Default = 0,
    Max64Kb = 4,
    Max256Kb = 5,
    Max1Mb = 6,
    Max4Mb = 7
  }

  public enum LZ4BlockMode
  {
    Linked = 0,
    Independent
  }

  public enum LZ4ContentChecksum
  {
    Disabled = 0,
    Enabled
  }

  public enum Lz4AutoFlush
  {
    Disabled = 0,
    Enabled
  }

  public enum LZ4FrameType {
    Frame = 0,
    SkippableFrame,
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

  [StructLayout(LayoutKind.Sequential)]
  public struct Lz4FrameInfo
  {
    public LZ4BlockSize BlockSize;
    public LZ4BlockMode BlockMode;
    public LZ4ContentChecksum ContentChecksum;
    public LZ4FrameType FrameType;
    public long ContentSize;
    private unsafe fixed uint reserved[2];
  }

  [StructLayout(LayoutKind.Sequential)]
  public unsafe struct LZ4Preferences
  {
    public Lz4FrameInfo FrameInfo;
    public Lz4CompressionLevel CompressionLevel;
    public Lz4AutoFlush AutoFlush;
    private unsafe fixed uint reserved[4];
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
    Ok                         = 0,
    InvalidMaxBlockSize        = -1,
    InvalidBlockMode           = -2,
    InvalidContentChecksumFlag = -3,
    InvalidCompressionLevel    = -4,
    AllocationFailed           = -5,
    SrcSizeTooLarge            = -6,
    DstSizeTooLarge            = -7,
    DecompressionFailed        = -8,
    InvalidChecksum            = -9,
    ErrorMaxCode               = -10,
  }

  [SuppressUnmanagedCodeSecurity]
  public static class LZ4Native
  {
    private static readonly object _staticSyncRoot = new object();
    private static IntPtr _nativeModulePtr;
    internal const uint Lz4Version = 100;
    public const int HeaderMaxSize = 20;
    public const int MagicNumber = 0x184D2204;
    public const int KB = (1 << 10);
    public const int MB = (1 << 20);
    public const int GB = (1 << 30);


    public static int GetBlockSize(LZ4BlockSize blockSizeType)
    {
      switch (blockSizeType)
      {
        case LZ4BlockSize.Max64Kb:
          return 64*KB;
        case LZ4BlockSize.Max256Kb:
          return 256*KB;
        case LZ4BlockSize.Max1Mb:
          return 1*MB;
        case LZ4BlockSize.Max4Mb:
          return 4*MB;
      }

      return -1;
    }

    static LZ4Native()
    {
      lock (_staticSyncRoot) {
        _nativeModulePtr = Preload(LIBLZ4);
      }
    }

    internal const string LIBLZ4 = "liblz4";
    public const int LZ4_BLOCK_SIZE_ID_DEFAULT = 7;

    public const int MAX_FRAME_HEADER_SIZE = 15;
    #region Simple Compression Functions
    /// <summary>
    /// Get the maximal output buffer required to compress <paramref name="srcSize"/> input bytes"/>
    /// </summary>
    /// <param name="srcSize">Size of the source buffer to be compressed.</param>
    /// <param name="preferences">The compression preferences;optional. You can pass null, in which case all preferences will be set to default.</param>
    /// <returns>size_t.</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public unsafe static extern size_t LZ4F_compressFrameBound(size_t srcSize, LZ4Preferences* preferences = null);

    /// <summary>
    /// Compress an entire srcBuffer into a valid LZ4 frame, as defined by specification v1.5.1
    /// </summary>
    /// <param name="dstBuffer">The destination buffer; MUST be large enough, (<paramref name="dstMaxSize"/>) to ensure compression completion even in worst case</param>
    /// <param name="dstMaxSize">The destination buffer size. You can get the minimum required value of dstMaxSize by using LZ4F_compressFrameBound()</param>
    /// <param name="srcBuffer">The source buffer.</param>
    /// <param name="srcSize">Size of the source buffer.</param>
    /// <param name="preferences">The compression preferences;optional. You can pass null, in which case all preferences will be set to default.</param>
    /// <returns>The result of the function is the number of bytes written into dstBuffer.</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_compressFrame(byte* dstBuffer, size_t dstMaxSize, byte* srcBuffer, size_t srcSize, LZ4Preferences *preferences = null);
    #endregion

    #region Advanced Compression Functions
    /// <summary>
    /// Create a compression context.
    /// The first thing to do is to create a compression context object, which will be used in all compression operations.
    /// This is achieved by calling this function, which takes as argument a version and an LZ4F_preferences_t structure.
    /// The version provided MUST be LZ4F_VERSION. It is intended to track potential version differences between different binaries.
    /// The function will provide a pointer to a fully allocated compression context_t object.
    /// </summary>
    /// <param name="ctx">A pointer to the compression context pointer.</param>
    /// <param name="version">The frame version.</param>
    /// <returns>The result is an errorCode, which can be tested using <see cref="LZ4F_isError"/>.</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe size_t LZ4F_createCompressionContext(void** ctx, uint version);

    /// <summary>
    /// Release the resources allocated for the comrpession context
    /// </summary>
    /// <param name="ctx">A pointer to the context.</param>
    /// <returns>The result is an errorCode, which can be tested using <see cref="LZ4F_isError"/>.</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe size_t LZ4F_freeCompressionContext(void* ctx);

    /// <summary>
    /// Write the frame header into dstBuffer.
    /// dstBuffer must be large enough to accommodate a header (dstMaxSize). Maximum header size is 15 bytes.
    /// </summary>
    /// <param name="ctx">The CTX.</param>
    /// <param name="dstBuffer">The DST buffer.</param>
    /// <param name="dstMaxSize">Maximum size of the DST.</param>
    /// <param name="preferences">The compression preferences to use. The preferences structure is optional : you can provide null as argument, all preferences will then be set to default.</param>
    /// <returns>The result of the function is the number of bytes written into dstBuffer for the header</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe size_t LZ4F_compressBegin(void* ctx, void* dstBuffer, size_t dstMaxSize, LZ4Preferences* preferences = null);

    /// <summary>
    /// Provides the minimum size of Dst buffer given srcSize to handle worst case situations.
    /// Different preferences can produce different results.
    /// This function includes frame termination cost (4 bytes, or 8 if frame checksum is enabled).
    /// </summary>
    /// <param name="srcSize">Size of the source buffer size.</param>
    /// <param name="preferences">The compression preferences. prefsPtr is optional : you can provide NULL as argument, all preferences will then be set to cover worst case.</param>
    /// <returns>The size of the worst-case destination buffer to compress <paramref name="srcSize"/> bytes</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_compressBound(size_t srcSize, LZ4Preferences *preferences = null);

    /// <summary>
    /// LZ4F_compressUpdate() can be called repetitively to compress as much data as necessary.
    /// The most important rule is that dstBuffer MUST be large enough (dstMaxSize) to ensure compression completion even in worst case.
    /// You can get the minimum value of dstMaxSize by using <see cref="LZ4F_compressBound"/>.
    /// If this condition is not respected, LZ4F_compress() will fail (result is an errorCode).
    /// The function doesn't guarantee error recovery, so you have to reset compression context when an error occurs.
    /// </summary>
    /// <param name="ctx">A pointer compression context.</param>
    /// <param name="dstBuffer">A pointer to the destination buffer.</param>
    /// <param name="dstMaxSize">The maximal available space in the destination buffer.</param>
    /// <param name="srcBuffer">A pointer to the source buffer.</param>
    /// <param name="srcSize">The size of available data in the source buffer.</param>
    /// <param name="options">A pointer to the  options structure. The options are optional; you can provide null as argument</param>
    /// <returns>The result of the function is the number of bytes written into dstBuffer : it can be zero, meaning input data was just buffered.
    /// The function outputs an error code if it fails (can be tested using <see cref="LZ4F_isError"/></returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_compressUpdate(void* ctx, void* dstBuffer, size_t dstMaxSize, void* srcBuffer, size_t srcSize, Lz4CompressOptions* options = null);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_flush(void* ctx, void* dstBuffer, size_t dstMaxSize, Lz4CompressOptions *options = null);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_compressEnd(void* ctx, void* dstBuffer, size_t dstMaxSize, Lz4CompressOptions* compressOptions = null);
    #endregion

    #region Decompression functions

    /// <summary>
    /// Create a decompression context.
    /// The first decompression API to call is to create an decompression context object, which will be used in all decompression operations.
    /// This is achieved using this function. The version provided MUST be LZ4F_VERSION. It is intended to track potential breaking differences
    /// between different versions. The function will provide a pointer to a fully allocated and initialized decompression context object.
    /// </summary>
    /// <param name="ctx">A pointer to a context pointer, will be filled by the callee.</param>
    /// <param name="version">The frame version.</param>
    /// <returns>The result is an errorCode, which can be tested using <see cref="LZ4F_isError"/>.</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_createDecompressionContext(void** ctx, uint version);

    /// <summary>
    /// Free the decompression context
    /// The decompression context memory can be released using this function.
    /// </summary>
    /// <param name="ctx">A pointer to the context to free.</param>
    /// <returns>The result is an errorCode, which can be tested using <see cref="LZ4F_isError"/>.</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_freeDecompressionContext(void* ctx);

    /// <summary>
    /// This function decodes frame header information (such as max BlockSize, frame checksum, etc.).
    /// Its usage is optional : you can start by calling directly LZ4F_decompress() instead.
    /// The objective is to extract frame header information, typically for allocation purposes.
    /// LZ4F_getFrameInfo() can also be used anytime after starting decompression, on any valid decompression context
    /// The result is *copied* into an existing LZ4F_frameInfo_t structure which must be already allocated.
    /// The number of bytes read from srcBuffer will be provided within *srcSizePtr(necessarily &lt;= original value).
    /// You are expected to resume decompression from where it stopped(srcBuffer + *srcSizePtr)
    /// </summary>
    /// <param name="ctx">The CTX.</param>
    /// <param name="frameInfo">A pointer to frame information structure to be filled.</param>
    /// <param name="srcBuffer">The source buffer.</param>
    /// <param name="srcSize">Size of the source buffer.</param>
    /// <returns>an hint of how many srcSize bytes LZ4F_decompress() expects for next call, or an error code which can be tested using <see cref="LZ4F_isError"/>
    /// (typically, when there is not enough src bytes to fully decode the frame header)</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_getFrameInfo(void* ctx, Lz4FrameInfo* frameInfo, void* srcBuffer, int* srcSize);

    /// <summary>
    /// Call this function repetitively to regenerate data compressed within srcBuffer.
    /// The function will attempt to decode *srcSizePtr bytes from srcBuffer, into dstBuffer of maximum size *dstSizePtr.
    /// The number of bytes regenerated into dstBuffer will be provided within *dstSizePtr (necessarily &lt;= original value).
    /// The number of bytes read from srcBuffer will be provided within *srcSizePtr (necessarily &lt;= original value).
    /// If number of bytes read is &lt; number of bytes provided, then decompression operation is not completed.
    /// This typically happens when <paramref name="dstBuffer"/> is not large enough to contain all decoded data.
    /// The function must be called again, starting from where it stopped(<paramref name="srcBuffer"/> + *<paramref name="srcSizePtr"/>)
    /// The function will check this condition, and refuse to continue if it is not respected.
    /// <paramref name="dstBuffer"/> is supposed to be flushed between each call to the function, since its content will be overwritten.
    /// dst arguments can be changed at will with each consecutive call to the function.
    /// The function result is an hint of how many srcSize bytes this function expects for next call.
    /// Schematically, it's the size of the current (or remaining) compressed block + header of next block.
    /// Respecting the hint provides some boost to performance, since it does skip intermediate buffers.
    /// This is just a hint, you can always provide any <paramref name="srcSize"/> you want.
    /// </summary>
    /// <remarks>After a frame is fully decoded, the decompression context can be used again to decompress another frame.</remarks>
    /// <param name="ctx">The decompression context</param>
    /// <param name="dstBuffer">The destination buffer.</param>
    /// <param name="dstMaxSize">A pointer to maximal size of the destination buffer.
    /// Will be modified by function to reflect how much data was written into the destination buffer during this call</param>
    /// <param name="srcBuffer">The source buffer.</param>
    /// <param name="srcSize">A pointer to the amount of available data in the source buffer.
    /// Will be modified by the function to reflect how much data was consumed from the source buffer during this call</param>
    /// <param name="decompressOptions">The decompress options.</param>
    /// <returns>When a frame is fully decoded, the function result will be 0 (no more data expected).
    /// If decompression failed, function result is an error code, which can be tested using <see cref="LZ4F_isError"/>.</returns>
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern size_t LZ4F_decompress(void* ctx, void* dstBuffer, size_t* dstMaxSize, void* srcBuffer, size_t* srcSize, Lz4DecompressOptions* decompressOptions = null);
    #endregion

    #region Error handling functions
    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LZ4F_isError(size_t code);

    [DllImport(LIBLZ4, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern sbyte* LZ4F_getErrorName(size_t code);
#endregion

    public static unsafe string GetErrorName(size_t code) => new string(LZ4F_getErrorName(code));
  }
}