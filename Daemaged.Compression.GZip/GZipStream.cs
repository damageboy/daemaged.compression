using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Daemaged.Compression.GZip
{
  /// <summary>Type of compression to use for the GZipStream. Currently only Decompress is supported.</summary>
  public enum CompressionMode
  {
    /// <summary>Compresses the underlying stream.</summary>
    Compress,
    /// <summary>Decompresses the underlying stream.</summary>
    Decompress,
  }

  /// <summary>Provides methods and properties used to compress and decompress streams.</summary>
  public class GZipStream : Stream
  {
    private const int BufferSize = 16384;

    private readonly Stream _compressedStream;
    private CompressionMode _mode;

    private ZStream _zstream;

    private const string ZLibVersion = "1.2.3";

    private readonly byte[] _inputBuffer;
    private GCHandle _inputBufferHandle;

    /// <summary>Initializes a new instance of the GZipStream class using the specified stream and BZip2CompressionMode value.</summary>
    /// <param name="stream">The stream to compress or decompress.</param>
    /// <param name="mode">One of the BZip2CompressionMode values that indicates the action to take.</param>
    public unsafe GZipStream(Stream stream, CompressionMode mode)
    {

      _compressedStream = stream;
      _mode = mode;

      _zstream.zalloc = null;
      _zstream.zfree = null;
      _zstream.opaque = null;

      switch (mode)
      {
        case CompressionMode.Compress:
          break;
        case CompressionMode.Decompress:
          var ret = ZLibNative.inflateInit2_(ref _zstream, ZLibOpenType.Both, ZLibVersion, Marshal.SizeOf(typeof(ZStream)));

          if (ret != ZLibReturnCode.Z_OK)
            throw new ArgumentException("Unable to init ZLib. Return code: " + ret);

          _inputBuffer = new byte[BufferSize];
          _inputBufferHandle = GCHandle.Alloc(_inputBuffer, GCHandleType.Pinned);

          break;

      }

    }

    /// <summary>GZipStream destructor. Cleans all allocated resources.</summary>
    ~GZipStream()
    {
      _inputBufferHandle.Free();
      ZLibNative.inflateEnd(ref _zstream);
    }

    /// <summary>Reads a number of decompressed bytes into the specified byte array.</summary>
    /// <param name="array">The array used to store decompressed bytes.</param>
    /// <param name="offset">The location in the array to begin reading.</param>
    /// <param name="count">The number of bytes decompressed.</param>
    /// <returns>The number of bytes that were decompressed into the byte array. If the end of the stream has been reached, zero or the number of bytes read is returned.</returns>
    public override unsafe int Read(byte[] array, int offset, int count)
    {
      if (_mode == CompressionMode.Compress)
        throw new NotSupportedException("Can't read on a compress stream!");

      var exitLoop = false;

      var tmpOutputBuffer = new byte[count];
      var tmpOutpuBufferHandle = GCHandle.Alloc(tmpOutputBuffer, GCHandleType.Pinned);

      _zstream.next_out = (byte*) tmpOutpuBufferHandle.AddrOfPinnedObject().ToPointer();
      _zstream.avail_out = (uint)tmpOutputBuffer.Length;

      try {
        while (_zstream.avail_out > 0 && exitLoop == false) {
          if (_zstream.avail_in == 0) {
            var readLength = _compressedStream.Read(_inputBuffer, 0, _inputBuffer.Length);
            _zstream.avail_in = (uint)readLength;
            _zstream.next_in = (byte*) _inputBufferHandle.AddrOfPinnedObject().ToPointer();
          }
          var result = ZLibNative.inflate(ref _zstream, ZLibFlush.NoFlush);
          switch (result) {
            case ZLibReturnCode.Z_STREAM_END:
              exitLoop = true;
              Array.Copy(tmpOutputBuffer, 0, array, offset, count - (int)_zstream.avail_out);
              break;
            case ZLibReturnCode.Z_OK:
              Array.Copy(tmpOutputBuffer, 0, array, offset, count - (int)_zstream.avail_out);
              break;
            case ZLibReturnCode.Z_MEM_ERROR:
              throw new OutOfMemoryException("ZLib return code: " + result);
            default:
              throw new Exception("ZLib return code: " + result);
          }
        }

        return (count - (int)_zstream.avail_out);
      }
      finally {
        tmpOutpuBufferHandle.Free();
      }
    }

    /// <summary>Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.</summary>
    public override void Close()
    {
      _compressedStream.Close();
      base.Close();
    }

    /// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
    public override bool CanRead { get { return (_mode == CompressionMode.Decompress ? true : false); } }

    /// <summary>Gets a value indicating whether the stream supports writing.</summary>
    public override bool CanWrite { get { return (_mode == CompressionMode.Compress ? true : false); } }

    /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
    public override bool CanSeek { get { return (false); } }

    /// <summary>Gets a reference to the underlying stream.</summary>
    public Stream BaseStream { get { return (_compressedStream); } }

    #region Not yet supported
    /// <summary>Flushes the contents of the internal buffer of the current GZipStream object to the underlying stream.</summary>
    public override void Flush()
    {
      throw new NotSupportedException("The method or operation is not implemented.");
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

    /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
    /// <param name="array">The array used to store compressed bytes.</param>
    /// <param name="offset">The location in the array to begin reading.</param>
    /// <param name="count">The number of bytes compressed.</param>
    public override void Write(byte[] array, int offset, int count)
    { throw new NotSupportedException("Not yet supported!"); }

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