using System;
using System.Diagnostics.Contracts;
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
        Ptr = null;
        if (_gch.IsAllocated)
          _gch.Free();
        Buffer = null;
        throw new Exception("Failed to initialize a new NativeBuffer", e);
      }
      Size = size;
    }

    public int Size { get; }
    public byte[] Buffer { get; }
    public byte* Ptr { get; }
    GCHandle _gch;
    /// <summary>
    /// Actual amount of data inside the buffer
    /// </summary>
    /// <value>The amount of bytes available to read.</value>
    public int AvailableToRead { get; set; }
    /// <summary>
    /// The last consumed offset within the buffer
    /// </summary>
    /// <value>The remaining offset.</value>
    public int ConsumedOffset { get; set; }


    [ContractInvariantMethod]
    private void ObjectInvariant()
    {
      Contract.Invariant(Ptr == null || _gch.IsAllocated);
      Contract.Invariant(AvailableToRead <= Size);
      Contract.Invariant(ConsumedOffset <= AvailableToRead);
    }

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