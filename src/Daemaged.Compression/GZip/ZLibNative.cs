using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Daemaged.Compression.Util;

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
    Raw = -15,
    ZLib = 15,
    GZip = 15 + 16,
    Both = 15 + 32,
  }

#if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  public struct ZStream
  {
    public unsafe byte *next_in;  //8
    public uint avail_in;         //12
    public uint total_in;         //20

    public unsafe byte *next_out; //28
    public uint avail_out;        //32
    public uint total_out;        //40

    public unsafe sbyte *msg;     //48
    public unsafe void *state;    //56

    public unsafe void* zalloc;   //64
    public unsafe void* zfree;    //72
    public unsafe void* opaque;   //80

    public int data_type;         //84
    public uint adler;            //88
    public uint reserved;         //92
  }

  public enum ZLibReturnCode
  {
    Ok            = 0,
    StreamEnd    = 1,
    Z_NEED_DICT     = 2,
    Z_ERRNO         = (-1),
    Z_STREAM_ERROR  = (-2),
    Z_DATA_ERROR    = (-3),
    MemError     = (-4),
    Z_BUF_ERROR     = (-5),
    Z_VERSION_ERROR = (-6),
  }


  [SuppressUnmanagedCodeSecurity]
  public static class ZLibNative
  {
    private static readonly object _staticSyncRoot = new object();
    private static IntPtr _nativeModulePtr;


    static ZLibNative()
    {
      Initialize();
    }

    private static void Initialize()
    {
      lock (_staticSyncRoot) {
        _nativeModulePtr = NativePreloadHelper.Preload(LIBZ);
      }
    }

    internal const string LIBZ = "libz";

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int deflateInit_(ref ZStream sz, int level, string vs, int size);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern ZLibReturnCode deflate(ref ZStream sz, ZLibFlush flush);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern int deflateReset(ref ZStream sz);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern int deflateEnd(ref ZStream sz);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int inflateInit_(ref ZStream sz, string vs, int size);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern unsafe ZLibReturnCode deflateInit2_ (ref ZStream sz, int  level, int  method, int windowBits, int memLevel, int strategy, string version, int stream_size);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern unsafe ZLibReturnCode inflateInit2_(ref ZStream sz, int windowBits, string version, int stream_size);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern ZLibReturnCode inflate(ref ZStream sz, ZLibFlush flush);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern int inflateReset(ref ZStream sz);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern int inflateEnd(ref ZStream sz);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int compress(byte *dest, uint *destLen, byte* source, uint sourceLen);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int compress2(byte* dest, uint* destLen, byte* source, uint sourceLen, int level);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint compressBound (uint sourceLen);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern int uncompress(byte* dest, uint* destLen, byte* source, uint sourceLen);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr gzopen(string name, string mode);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gzclose(IntPtr gzFile);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern int gzwrite(IntPtr gzFile, byte *data, int length);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern int gzread(IntPtr gzFile, byte *data, int length);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gzgetc(IntPtr gzFile);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern int gzputc(IntPtr gzFile, int c);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint zlibCompileFlags();

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static extern string zlibVersion();

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern uint adler32(uint adler, byte* data, uint length);

    [DllImport(LIBZ, CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern uint crc32(uint crc, byte* data, uint length);
  }
}