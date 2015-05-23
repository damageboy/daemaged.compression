using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using static Daemaged.Compression.Util.NativePreloadHelper;

namespace Daemaged.Compression.LZO2
{
  public enum LZOMethod { M1x15, M1x999 };

  [SuppressUnmanagedCodeSecurity]
  public unsafe class LZO2Native
  {
    internal const string LZO2 = "liblzo2";

    static LZO2Native()
    {
      _nativeModulePtr = Preload(LZO2);
      LZOInit();
    }

    private const uint LZO_VERSION = 0x2060;

    private static int LZOInit()
    {
      return __lzo_init_v2(LZO_VERSION, sizeof(short), sizeof(int), IntPtr.Size, sizeof(uint), sizeof(uint), IntPtr.Size,
                    IntPtr.Size, IntPtr.Size, IntPtr.Size);
    }

    [DllImport(LZO2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int __lzo_init_v2(uint version, int szShort, int szInt, int szLong, int szLzoUint, int szUint, int szDict, int szCharP, int szVoidP, int szCallBack);


    public static readonly int LZO1X_1_15_MEM_COMPRESS = (int)(32768 * IntPtr.Size);
    static IntPtr _nativeModulePtr;

    [DllImport(LZO2)]
    public static extern int lzo1x_1_15_compress(byte* src, IntPtr src_len,
                                byte* dst, IntPtr* dst_len,
                                void* wrkmem);

    [DllImport(LZO2)]
    public static extern int lzo1x_decompress(byte* src, IntPtr src_len,
                                byte* dst, IntPtr* dst_len,
                                void* wrkmem /* NOT USED */ );

    [DllImport(LZO2)]
    public static extern int lzo1x_999_compress(byte* src, IntPtr src_len,
                                byte* dst, IntPtr* dst_len,
                                void* wrkmem);

    public static void Compress(LZOMethod method, byte* src, IntPtr srcLen, byte* dst, IntPtr* dstLen, void* wrkMem)
    {
      int ret;
      switch (method)
      {
        case LZOMethod.M1x15:
          ret = lzo1x_1_15_compress(src, srcLen, dst, dstLen, wrkMem);
          break;
        case LZOMethod.M1x999:
          ret = lzo1x_999_compress(src, srcLen, dst, dstLen, wrkMem);
          break;
        default:
          throw new ArgumentException("Unknow method: " + method);
      }
      if (ret != 0)
        throw new LZOException(ret);
    }

    public static void Decompress(LZOMethod method, byte* src, IntPtr srcLen, byte* dst, IntPtr* dstLen)
    {
      int ret;
      switch (method)
      {
        case LZOMethod.M1x15:
        case LZOMethod.M1x999:
          ret = lzo1x_decompress(src, srcLen, dst, dstLen, null);
          break;
        default:
          throw new ArgumentException("Unknow method: " + method);
      }
      if (ret != 0)
        throw new LZOException(ret);
    }

    public static unsafe int LZO1x115Compress(byte[] src, int start, int count, out byte[] dst)
    {
      dst = new byte[count + count / 64 + 16 + 3];
      var num = new IntPtr(0);
      var workMem = new byte[LZO1X_1_15_MEM_COMPRESS];
      var srcHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
      var dstHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
      var workHandle = GCHandle.Alloc(workMem, GCHandleType.Pinned);
      lzo1x_1_15_compress(((byte*)srcHandle.AddrOfPinnedObject().ToPointer()) + start, (IntPtr) count,
                          (byte*)dstHandle.AddrOfPinnedObject().ToPointer(), &num,
                          (void*)workHandle.AddrOfPinnedObject());
      srcHandle.Free();
      dstHandle.Free();
      workHandle.Free();
      return num.ToInt32();
    }

    public static int LZO1x115Compress(byte[] src, out byte[] dst) => LZO1x115Compress(src, 0, src.Length, out dst);

    public class LZOException : Exception
    {
      private int _code;
      internal LZOException(int code) { _code = code; }
      public override string Message { get { return MESSAGES[_code]; } }

      private static Dictionary<int, string> MESSAGES = new Dictionary<int, string>()
      {
        {-1,  "Error"},
        {-2,  "Out of memory"},
        {-3,  "Not compressible"},
        {-4,  "Input overrun"},
        {-5,  "Output overrun"},
        {-6,  "Lookbehind overrun"},
        {-7,  "EOF not found"},
        {-8,  "Input not consumed"},
        {-9,  "Not yet implemented"},
        {-10, "Invalid argument"}
      };
    }
  }
}