using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Daemaged.Compression.LZMA
{
  public static class LZMAHelper
  {
    public static LZMAStatus CompressBuffer(byte[] inBuff, byte[] outBuff, out int outPosition, int preset) => CompressBuffer(inBuff, inBuff.Length, 0, outBuff, outBuff.Length, 0, out outPosition, preset);

    public static LZMAStatus CompressBuffer(byte[] inBuff, byte[] outBuff, out int outPosition, int preset, int delta) => CompressBuffer(inBuff, inBuff.Length, 0, outBuff, outBuff.Length, 0, out outPosition, preset, delta);

    public static unsafe LZMAStatus CompressBuffer(byte[] inBuff, int inLength, int inOffset, byte[] outBuff, int outLength, int outOffset, out int outPosition, int preset)
    {
      var inh = GCHandle.Alloc(inBuff, GCHandleType.Pinned);
      var outh = GCHandle.Alloc(outBuff, GCHandleType.Pinned);
      var inp = (byte*)inh.AddrOfPinnedObject().ToPointer();
      var outp = (byte*)outh.AddrOfPinnedObject().ToPointer();
      IntPtr outPos;
      var ret = CompressBuffer(inp + inOffset, (IntPtr)inLength, outp + outOffset, (IntPtr)outLength, out outPos, preset);
      outPosition = outPos.ToInt32();
      return ret;
    }

    public static unsafe LZMAStatus CompressBuffer(byte[] inBuff, int inLength, int inOffset, byte[] outBuff, int outLength, int outOffset, out int outPosition, int preset, int delta)
    {
      var inh = GCHandle.Alloc(inBuff, GCHandleType.Pinned);
      var outh = GCHandle.Alloc(inBuff, GCHandleType.Pinned);
      var inp = (byte *) inh.AddrOfPinnedObject().ToPointer();
      var outp = (byte*) outh.AddrOfPinnedObject().ToPointer();
      IntPtr outPos;
      var ret = CompressBuffer(inp + inOffset, (IntPtr)inLength, outp + outOffset, (IntPtr) outLength, out outPos,  preset, delta);
      outPosition = outPos.ToInt32();
      return ret;
    }

    public static unsafe LZMAStatus CompressBuffer(void *inBuff, IntPtr inLength, void *outBuff, IntPtr outLength, out IntPtr outPosition, int preset, int delta)
    {
      var deltaOpts = new LZMAOptionsDelta((uint) delta);
      var options = new LZMAOptionLZMA((uint) preset);
      var filters = stackalloc LZMAFilter[3];
      filters[0].id = LZMANative.LZMA_FILTER_DELTA;
      filters[0].options = &deltaOpts;
      filters[1].id = LZMANative.LZMA_FILTER_LZMA2;
      filters[1].options = &options;
      filters[2].id = LZMANative.LZMA_VLI_UNKNOWN;
      return CompressBuffer(inBuff, inLength, outBuff, outLength, out outPosition, filters);
    }

    public static unsafe LZMAStatus CompressBuffer(void *inBuff, IntPtr inLength, void *outBuff, IntPtr outLength, out IntPtr outPosition, int preset)
    {
      var options = new LZMAOptionLZMA((uint) preset);
      var filters = stackalloc LZMAFilter[2];
      filters[0].id = LZMANative.LZMA_FILTER_LZMA2;
      filters[0].options = &options;
      filters[1].id = LZMANative.LZMA_VLI_UNKNOWN;
      return CompressBuffer(inBuff, inLength, outBuff, outLength, out outPosition, filters);
    }
    public static unsafe LZMAStatus CompressBuffer(void *inBuff, IntPtr inLength, void *outBuff, IntPtr outLength, out IntPtr outPosition, LZMAFilter *filters)
    {
      fixed (IntPtr* op = &outPosition)
        return LZMANative.lzma_stream_buffer_encode(filters, LZMACheck.LZMA_CHECK_CRC64, null,
                                                    (byte*) inBuff, inLength,
                                                    (byte*) outBuff, op, outLength);
    }

    public static LZMAStatus DecompressBuffer(byte[] inBuff,  out int inPosition, byte[] outBuff, out int outPosition)
    { return DecompressBuffer(inBuff, inBuff.Length, 0, out inPosition, outBuff, outBuff.Length, 0, out outPosition); }

    public static unsafe LZMAStatus DecompressBuffer(byte[] inBuff, int inLength, int inOffset, out int inPosition, byte[] outBuff, int outLength, int outOffset, out int outPosition)
    {
      var inh = GCHandle.Alloc(inBuff, GCHandleType.Pinned);
      var outh = GCHandle.Alloc(outBuff, GCHandleType.Pinned);
      var inp = (byte*)inh.AddrOfPinnedObject().ToPointer();
      var outp = (byte*)outh.AddrOfPinnedObject().ToPointer();
      IntPtr outPos = IntPtr.Zero;
      IntPtr inPos = IntPtr.Zero;
      var ret = DecompressBuffer(inp + inOffset, (IntPtr)inLength, out inPos, outp + outOffset, (IntPtr)outLength, out outPos);
      outPosition = outPos.ToInt32();
      inPosition = inPos.ToInt32();
      return ret;
    }
    public static unsafe LZMAStatus DecompressBuffer(void *inBuff, IntPtr inLength, out IntPtr inPosition, void *outBuff, IntPtr outLength, out IntPtr outPosition)
    {
      ulong memLimit = 80*1024*1024;
      fixed (IntPtr *inp = &inPosition)
      fixed (IntPtr *op = &outPosition)
        return LZMANative.lzma_stream_buffer_decode(&memLimit, 0, null, inBuff, inp, inLength, outBuff, op, outLength);
    }
  }
}
