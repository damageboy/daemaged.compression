using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Daemaged.Compression.GZip
{
  /// <summary>
  /// Defines constants for the various flush types used with zlib
  /// </summary>
  internal enum FlushTypes
  {
    None, Partial, Sync, Full, Finish, Block
  }

  /// <summary>
  /// Defines constants for the available compression levels in zlib
  /// </summary>
  public enum CompressLevel
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

  public enum ZLibFlush
  {
    NoFlush = 0,
    PartialFlush = 1,
    SyncFlush = 2,
    FullFlush = 3,
    Finish = 4
  }

  internal enum ZLibCompressionLevel
  {
    NoCompression = 0,
    BestSpeed = 1,
    BestCompression = 2,
    DefaultCompression = 3
  }

  internal enum ZLibCompressionStrategy
  {
    Filtered = 1,
    HuffmanOnly = 2,
    DefaultStrategy = 0
  }

  internal enum ZLibCompressionMethod
  {
    Delated = 8
  }

  internal enum ZLibDataType
  {
    Binary = 0,
    Ascii = 1,
    Unknown = 2,
  }

  public enum ZLibOpenType
  {
    ZLib = 15,
    GZip = 15 + 16,
    Both = 15 + 32,
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0)]
  public struct ZStream
  {
    public unsafe byte *next_in;
    public uint avail_in;
    public IntPtr total_in;

    public unsafe byte *next_out;
    public uint avail_out;
    public IntPtr total_out;
    
    public unsafe sbyte *msg;
    public unsafe void *state;

    public unsafe void* zalloc;
    public unsafe void* zfree;
    public unsafe void* opaque;

    public int data_type;
    public uint adler;
    public uint reserved;
  }

  public enum ZLibReturnCode
  {
    Z_OK            = 0,
    Z_STREAM_END    = 1,
    Z_NEED_DICT     = 2,
    Z_ERRNO         = (-1),
    Z_STREAM_ERROR  = (-2),
    Z_DATA_ERROR    = (-3),
    Z_MEM_ERROR     = (-4),
    Z_BUF_ERROR     = (-5),
    Z_VERSION_ERROR = (-6),    
  }

  public static class ZLibNative
  {
#if ZLIB_MIXED_MODE
    internal const string ZLIB = "Daemaged.Compression.GZip";
#else
    internal const string ZLIB = "libz";
#endif
    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int deflateInit_(ref ZStream sz, int level, string vs, int size);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern ZLibReturnCode deflate(ref ZStream sz, int flush);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int deflateReset(ref ZStream sz);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int deflateEnd(ref ZStream sz);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int inflateInit_(ref ZStream sz, string vs, int size);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern unsafe int deflateInit2_ (ref ZStream sz, int  level, int  method, int windowBits, int memLevel, int strategy, sbyte *version, int stream_size);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern unsafe ZLibReturnCode inflateInit2_(ref ZStream sz, ZLibOpenType windowBits, string version, int stream_size);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern ZLibReturnCode inflate(ref ZStream sz, ZLibFlush flush);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int inflateReset(ref ZStream sz);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int inflateEnd(ref ZStream sz);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int compress(byte *dest, uint *destLen, byte* source, uint sourceLen);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int compress2(byte* dest, uint* destLen, byte* source, uint sourceLen, int level);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint compressBound (uint sourceLen);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int uncompress(byte* dest, uint* destLen, byte* source, uint sourceLen);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr gzopen(string name, string mode);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gzclose(IntPtr gzFile);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern int gzwrite(IntPtr gzFile, byte *data, int length);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern int gzread(IntPtr gzFile, byte *data, int length);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gzgetc(IntPtr gzFile);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gzputc(IntPtr gzFile, int c);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint zlibCompileFlags();

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern string zlibVersion();

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern uint adler32(uint adler, byte* data, uint length);

    [DllImport(ZLibNative.ZLIB, CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern uint crc32(uint crc, byte* data, uint length);
  }
}