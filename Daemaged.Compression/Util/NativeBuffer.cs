using System;
using System.Runtime.InteropServices;

namespace Daemaged.Compression.Util
{
  internal unsafe class NativeBuffer : IDisposable
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="NativeBuffer"/> class.
    /// </summary>
    /// <param name="size">The size of the buffer</param>
    /// <exception cref="Exception">Failed to initialize a new NativeBuffer</exception>
    public NativeBuffer(int size)
    {
      Buffer = new byte[size];
      try {
        _gch = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
        Ptr = (byte*) _gch.AddrOfPinnedObject().ToPointer();
      }
      catch (Exception e) {
        throw new Exception("Failed to initialize a new NativeBuffer", e);
      }
      Size = size;
    }

    public int Size { get; private set; }
    public byte[] Buffer { get; private set; }
    public byte* Ptr { get; private set; }
    GCHandle _gch;
    public int AvailableToRead { get; set; }
    public int RemainingOffset { get; set; }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <exception cref="InvalidOperationException">The handle was freed or never initialized. </exception>
    public void Dispose()
    {
      _gch.Free();
    }
  }
}