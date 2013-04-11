using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Daemaged.Compression.GZip
{

  /// <summary>Provides methods and properties used to compress and decompress streams.</summary>
  [SuppressUnmanagedCodeSecurity] 
  public class GZipStream : Stream
  {
    private const int BufferSize = 16384;

    private readonly Stream _stream;
    private readonly CompressionMode _mode;

    private ZStream _zstream;

    private const string ZLibVersion = "1.2.7";

    private bool _isDisposed;
    private readonly byte[] _tmpBuffer;
    private readonly GCHandle _tmpBufferHandle;
    private readonly unsafe void* _tmpBufferPtr;
    private bool _isClosed;
    private bool _writeAfterReset;

    public GZipStream(Stream stream, CompressionMode mode) : 
      this(stream, mode, new GZipOptions())
    { }

    /// <summary>Initializes a new instance of the GZipStream class using the specified stream and BZip2CompressionMode value.</summary>
    /// <param name="stream">The stream to compress or decompress.</param>
    /// <param name="mode">One of the BZip2CompressionMode values that indicates the action to take.</param>
    /// <param name="options">The Gzip Options</param>ll
    public unsafe GZipStream(Stream stream, CompressionMode mode, GZipOptions options) 
    {
      if (stream == null)
        throw new ArgumentNullException("stream");

      _stream = stream;
      _mode = mode;

      _zstream.zalloc = null;
      _zstream.zfree = null;
      _zstream.opaque = null;

      _tmpBuffer = new byte[BufferSize];
      _tmpBufferHandle = GCHandle.Alloc(_tmpBuffer, GCHandleType.Pinned);
      _tmpBufferPtr = _tmpBufferHandle.AddrOfPinnedObject().ToPointer();
      ZLibReturnCode ret;
      switch (mode)
      {
        case CompressionMode.Compress:
          ret = ZLibNative.deflateInit2_(ref _zstream, 
                                         options.Level, options.Method, options.WindowBits, 
                                         options.MemoryLevel, (int) options.Strategy, 
                                         ZLibVersion, Marshal.SizeOf(typeof(ZStream)));

          if (ret != ZLibReturnCode.Ok)
            throw new ArgumentException("Unable to init ZLib. Return code: " + ret);

          _zstream.next_out = (byte*) _tmpBufferPtr;
          _zstream.avail_out = (uint) _tmpBuffer.Length;
          break;
        case CompressionMode.Decompress:
          ret = ZLibNative.inflateInit2_(ref _zstream, options.WindowBits, ZLibVersion, Marshal.SizeOf(typeof(ZStream)));

          if (ret != ZLibReturnCode.Ok)
            throw new ArgumentException("Unable to init ZLib. Return code: " + ret);
          break;

      }

    }

    /// <summary>GZipStream destructor. Cleans all allocated resources.</summary>
    ~GZipStream()
    {
      Dispose();
    }

    public unsafe int Read(byte* array, int offset, int count)
    {
      if (_mode == CompressionMode.Compress)
        throw new NotSupportedException("Can't read on a compress stream!");

      var exitLoop = false;

      _zstream.next_out = array;
      _zstream.avail_out = (uint) count;

      while (_zstream.avail_out > 0 && exitLoop == false) {
        if (_zstream.avail_in == 0) {
          var readLength = _stream.Read(_tmpBuffer, 0, _tmpBuffer.Length);
          _zstream.avail_in = (uint) readLength;
          _zstream.next_in = (byte*) _tmpBufferPtr;
        }
        var result = ZLibNative.inflate(ref _zstream, ZLibFlush.NoFlush);
        switch (result) {
          case ZLibReturnCode.StreamEnd:
            exitLoop = true;
            break;
          case ZLibReturnCode.Ok:
            break;
          case ZLibReturnCode.MemError:
            throw new OutOfMemoryException("ZLib return code: " + result);
          default:
            throw new Exception("ZLib return code: " + result);
        }
      }

      return (count - (int) _zstream.avail_out);
    }


    /// <summary>Reads a number of decompressed bytes into the specified byte array.</summary>
    /// <param name="buffer">the buffer used to store decompressed bytes</param>
    /// <param name="offset">The location in the array to begin reading.</param>
    /// <param name="count">The number of bytes decompressed.</param>
    /// <returns>The number of bytes that were decompressed into the byte array. If the end of the stream has been reached, zero or the number of bytes read is returned.</returns>
    public override unsafe int Read(byte[] buffer, int offset, int count)
    {
      if (!CanRead) throw new NotSupportedException();
      if (buffer == null) throw new ArgumentNullException();
      if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
      if ((offset + count) > buffer.Length) throw new ArgumentException();
      if (_isDisposed) throw new ObjectDisposedException("GZipStream");

      fixed (byte* b = &buffer[0])
      {
        return Read(b, offset, count);
      }
    }

    /// <summary>Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.</summary>
    public unsafe override void Close()
    {
      if (_isClosed)
        return;
      _isClosed = true; 
      if (_mode == CompressionMode.Compress)
      {
        if (_writeAfterReset)
          Write(null, 0, ZLibFlush.Finish);
        ZLibNative.deflateEnd(ref _zstream);
      } else 
        ZLibNative.inflateEnd(ref _zstream);
      
      if (CloseUnderlyingStream)
        _stream.Close();

      _tmpBufferHandle.Free();
      base.Close();      
    }

    /// <summary>Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.</summary>
    public unsafe void Reset() {
      if (_isClosed)
        return;
      if (_mode == CompressionMode.Compress) {
        if (_writeAfterReset)
          Write(null, 0, ZLibFlush.Finish);
        _writeAfterReset = false;
        ZLibNative.deflateReset(ref _zstream);
      } else
        ZLibNative.inflateReset(ref _zstream);
    }


    public bool CloseUnderlyingStream { get; set; }

    /// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
    public override bool CanRead { get { return (_mode == CompressionMode.Decompress ? true : false); } }

    /// <summary>Gets a value indicating whether the stream supports writing.</summary>
    public override bool CanWrite { get { return (_mode == CompressionMode.Compress ? true : false); } }

    /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
    public override bool CanSeek { get { return (false); } }

    /// <summary>Gets a reference to the underlying stream.</summary>
    public Stream BaseStream { get { return (_stream); } }

    /// <summary>Flushes the contents of the internal buffer of the current GZipStream object to the underlying stream.</summary>
    public override unsafe void Flush()
    { Write(null, 0, ZLibFlush.FullFlush); }

    /// <summary>
    /// Closes the external file handle
    /// </summary>
    public new void Dispose()
    { Dispose(true); }


    // Does the actual closing of the file handle.
    protected override void Dispose(bool isDisposing)
    {
      if (_isDisposed)
        return;

      Close();
      _isDisposed = true;
    }


    /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
    /// <param name="offset">The location in the stream.</param>
    /// <param name="origin">One of the SeekOrigin values.</param>
    /// <returns>A long value.</returns>
    public override long Seek(long offset, SeekOrigin origin)
    { throw new NotSupportedException(); }

    /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
    /// <param name="value">The length of the stream.</param>
    public override void SetLength(long value)
    { throw new NotSupportedException(); }

    private unsafe void Write(byte *buffer, int count, ZLibFlush flush)
    {
      if (_mode == CompressionMode.Decompress)
        throw new NotSupportedException("Can't write on a decompress stream!");

      // This indicates that we need to go through a "Finish" write when closing/reseting
      _writeAfterReset = true;

      _zstream.avail_in = (uint)count;
      _zstream.next_in = buffer;
      uint availOut;
      do
      {
        var result = ZLibNative.deflate(ref _zstream, flush);
        availOut = _zstream.avail_out;
        if (availOut < BufferSize)
        {
          var outSize = BufferSize - availOut;
          _stream.Write(_tmpBuffer, 0, (int)outSize);
          _zstream.next_out = (byte*)_tmpBufferPtr;
          _zstream.avail_out = BufferSize;
        }
        // Translate erros into specific exceptions
        switch (result)
        {
          case ZLibReturnCode.Ok:
            continue;
          case ZLibReturnCode.StreamEnd:
            return;
          case ZLibReturnCode.MemError:
            throw new OutOfMemoryException("zlib return code: " + result);
          default:
            throw new Exception("zlib return code: " + result);
        }
      // We go one for two reasons:
      // 1. There's still more input available
      // 2. We're in some FLUSH/FINISH scenario 
      //    and the output buffer has no sufficient space left
      } while ((_zstream.avail_in > 0) || 
               (flush != ZLibFlush.NoFlush && availOut == 0));
    }

    public unsafe void Write(byte* buffer, int count)
    {
      if (!CanWrite) throw new NotSupportedException();
      if (_isDisposed) throw new ObjectDisposedException("GZipStream");
      
      Write(buffer, count, ZLibFlush.NoFlush);
    }

    //public unsafe void Write(byte* buffer, int length, int offset, int count)
    //{ Write(buffer + offset, count, LZMAAction.LZMA_RUN); }
    /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
    /// <param name="buffer">The array used to store compressed bytes.</param>
    /// <param name="offset">The location in the array to begin reading.</param>
    /// <param name="count">The number of bytes compressed.</param>
    public override unsafe void Write(byte[] buffer, int offset, int count)
    {
      if (buffer == null) throw new ArgumentNullException();
      if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
      if ((offset + count) > buffer.Length) throw new ArgumentException();

      fixed (byte* b = &buffer[0]) {
        Write(b + offset, count);
      }
    }

    #region Not yet supported
    /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
    public override long Length
    { get { throw new NotSupportedException(); } }

    /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
    public override long Position
    {
      get { throw new NotSupportedException(); }
      set { throw new NotSupportedException(); }
    }
    #endregion
  }
}