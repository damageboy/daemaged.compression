using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Daemaged.Compression.LZMA
{
  /// <summary>Provides methods and properties used to compress and decompress streams.</summary>
  [SuppressUnmanagedCodeSecurity] 
  public class LZMAStream : Stream
  {
    private const int BufferSize = 16384;

    private Stream _compressedStream;
    private CompressionMode _mode;

    private unsafe LZMAStreamNative *_zstream;    

    private byte[] _tmpBuffer;
    private GCHandle _tmpBufferHandle;
    private unsafe void *_tmpBufferPtr;
    private byte[] _zstreamBuff;
    private GCHandle _zstreamHandle;
    private bool _wasWrittenTo;
    private bool _isDisposed;

    private bool _isClosed;


    public unsafe LZMAStream(Stream stream, CompressionMode mode) : this(stream, mode,  null) {}

    /// <summary>Initializes a new instance of the GZipStream class using the specified stream and BZip2CompressionMode value.</summary>
    /// <param name="stream">The stream to compress or decompress.</param>
    /// <param name="mode">One of the BZip2CompressionMode values that indicates the action to take.</param>
    /// <param name="opts">The encodgin/decoding options</param>
    public unsafe LZMAStream(Stream stream, CompressionMode mode, ref LZMAOptionLZMA opts)
    {
      fixed (LZMAOptionLZMA* o = &opts) {
        Init(stream, mode, o);
      }
    }

    public unsafe LZMAStream(Stream stream, CompressionMode mode, LZMAOptionLZMA *opts)
    { Init(stream, mode, opts); }

    public unsafe LZMAStream(Stream stream, CompressionMode mode, uint preset)
    { Init(stream, mode, preset); }



    public unsafe void Init(Stream stream, CompressionMode mode, LZMAOptionLZMA *opts)
    {
      _compressedStream = stream;
      _mode = mode;

      _isClosed = false;

      _zstreamBuff = new byte[sizeof(LZMAStreamNative)];
      _zstreamHandle = GCHandle.Alloc(_zstreamBuff, GCHandleType.Pinned);
      _zstream = (LZMAStreamNative*)_zstreamHandle.AddrOfPinnedObject().ToPointer();

      _tmpBuffer = new byte[BufferSize];
      _tmpBufferHandle = GCHandle.Alloc(_tmpBuffer, GCHandleType.Pinned);
      _tmpBufferPtr = _tmpBufferHandle.AddrOfPinnedObject().ToPointer();

      LZMAStatus ret;
      switch (mode)
      {
        case CompressionMode.Compress:          
          // We will always use one filter, + 1 to mark the end of the filter array
          var filters = stackalloc LZMAFilter[2];
          filters[0].id = LZMANative.LZMA_FILTER_LZMA2;
          filters[0].options = opts;
          filters[1].id = LZMANative.LZMA_VLI_UNKNOWN;
          
          ret = LZMANative.lzma_stream_encoder(_zstream, filters, LZMACheck.LZMA_CHECK_CRC64);

          if (ret != LZMAStatus.LZMA_OK)
            throw new ArgumentException(string.Format("Unable to init LZMA decoder. Return code: {0}", ret));

          _zstream->next_out = _tmpBufferPtr;
          _zstream->avail_out = (IntPtr)_tmpBuffer.Length;

          break;
        case CompressionMode.Decompress:
          ret = LZMANative.lzma_auto_decoder(_zstream, 1024 * 1024 * 1024, 0);

          if (ret != LZMAStatus.LZMA_OK)
            throw new ArgumentException(string.Format("Unable to init LZMA decoder. Return code: {0}", ret));
          break;
      }

    }

    public unsafe void Init(Stream stream, CompressionMode mode, uint preset)
    {
      _compressedStream = stream;
      _mode = mode;

      _isClosed = false;

      _zstreamBuff = new byte[sizeof(LZMAStreamNative)];
      _zstreamHandle = GCHandle.Alloc(_zstreamBuff, GCHandleType.Pinned);
      _zstream = (LZMAStreamNative*)_zstreamHandle.AddrOfPinnedObject().ToPointer();

      _tmpBuffer = new byte[BufferSize];
      _tmpBufferHandle = GCHandle.Alloc(_tmpBuffer, GCHandleType.Pinned);
      _tmpBufferPtr = _tmpBufferHandle.AddrOfPinnedObject().ToPointer();

      LZMAStatus ret;
      switch (mode)
      {
        case CompressionMode.Compress:          
          ret = LZMANative.lzma_easy_encoder(_zstream, preset, LZMACheck.LZMA_CHECK_CRC64);

          if (ret != LZMAStatus.LZMA_OK)
            throw new ArgumentException(string.Format("Unable to init LZMA decoder. Return code: {0}", ret));

          _zstream->next_out = _tmpBufferPtr;
          _zstream->avail_out = (IntPtr)_tmpBuffer.Length;

          break;
        case CompressionMode.Decompress:
          ret = LZMANative.lzma_auto_decoder(_zstream, 1024 * 1024 * 1024, 0);

          if (ret != LZMAStatus.LZMA_OK)
            throw new ArgumentException(string.Format("Unable to init LZMA decoder. Return code: {0}", ret));
          break;
      }

    }



    /// <summary>GZipStream destructor. Cleans all allocated resources.</summary>
    ~LZMAStream()
    {
      Dispose();
    }

    public bool CloseUnderlyingStream { get; set; }

    #region Destructor & IDispose stuff

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

    #endregion

    public unsafe int Read(byte* buffer, int length, int offset, int count)
    {
      if (_mode == CompressionMode.Compress)
        throw new NotSupportedException("Can't read on a compress stream!");

      var exitLoop = false;

      _zstream->next_out = buffer + offset;
      _zstream->avail_out = (IntPtr)count;

      while (_zstream->avail_out.ToInt32() > 0 && exitLoop == false)
      {
        if (_zstream->avail_in.ToInt32() == 0)
        {
          var readLength = _compressedStream.Read(_tmpBuffer, 0, _tmpBuffer.Length);
          _zstream->avail_in = (IntPtr)readLength;
          _zstream->next_in = _tmpBufferPtr;
        }
        var result = LZMANative.lzma_code(_zstream, LZMAAction.LZMA_RUN);

        if (result == LZMAStatus.LZMA_OK)
          continue;

        switch (result) {
          case LZMAStatus.LZMA_STREAM_END:
            exitLoop = true;
            break;
          case LZMAStatus.LZMA_MEM_ERROR:
          case LZMAStatus.LZMA_MEMLIMIT_ERROR:
            throw new OutOfMemoryException("liblzma return code: " + result);
          default:
            throw new Exception("liblzma return code: " + result);
        }
      }

      return (count - (int) _zstream->avail_out);          
    }

    private unsafe void Write(byte* buffer, int count, LZMAAction action)
    {
      if (_mode == CompressionMode.Decompress)
        throw new NotSupportedException("Can't write on a decompress stream!");

      _wasWrittenTo = true;

      _zstream->avail_in = (IntPtr)count;
      _zstream->next_in = buffer;

      do {
        var result = LZMANative.lzma_code(_zstream, action);

        var avail_out = _zstream->avail_out.ToInt32();
        if (avail_out < BufferSize) {
          var outSize = BufferSize - avail_out;
          _compressedStream.Write(_tmpBuffer, 0, outSize);
          _zstream->next_out = _tmpBufferPtr;
          _zstream->avail_out = (IntPtr) BufferSize;
        }

        
        // Translate erros into specific exceptions
        switch (result) {
          case LZMAStatus.LZMA_OK:
            continue;
          case LZMAStatus.LZMA_STREAM_END:
            return;
          case LZMAStatus.LZMA_MEM_ERROR:
          case LZMAStatus.LZMA_MEMLIMIT_ERROR:
            throw new OutOfMemoryException("liblzma return code: " + result);
          default:
            throw new Exception("liblzma return code: " + result);
        }
      } while ((_zstream->avail_in.ToInt32() > 0) || action == LZMAAction.LZMA_FINISH);

    }

    public unsafe void Write(byte* buffer, int length, int offset, int count)
    { Write(buffer + offset, count, LZMAAction.LZMA_RUN); }

    /// <summary>
    /// Attempts to read a number of bytes from the stream.
    /// </summary>
    /// <param name="buffer">The destination data buffer</param>
    /// <param name="offset">The index of the first destination byte in <c>buffer</c></param>
    /// <param name="count">The number of bytes requested</param>
    /// <returns>The number of bytes read</returns>
    /// <exception cref="ArgumentNullException">If <c>buffer</c> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <c>count</c> or <c>offset</c> are negative</exception>
    /// <exception cref="ArgumentException">If <c>offset</c>  + <c>count</c> is &gt; buffer.Length</exception>
    /// <exception cref="NotSupportedException">If this stream is not readable.</exception>
    /// <exception cref="ObjectDisposedException">If this stream has been disposed.</exception>
    public override unsafe int Read(byte[] buffer, int offset, int count)
    {
      if (!CanRead) throw new NotSupportedException();
      if (buffer == null) throw new ArgumentNullException();
      if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
      if ((offset + count) > buffer.Length) throw new ArgumentException();
      if (_isDisposed) throw new ObjectDisposedException("GZipStream");

      fixed (byte* b = &buffer[0]) {
        return Read(b, buffer.Length, offset, count);
      }
    }

    /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
    /// <param name="buffer">The array used to store compressed bytes.</param>
    /// <param name="offset">The location in the array to begin reading.</param>
    /// <param name="count">The number of bytes compressed.</param>
    public override unsafe void Write(byte[] buffer, int offset, int count)
    {
      if (!CanWrite) throw new NotSupportedException();
      if (buffer == null) throw new ArgumentNullException();
      if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
      if ((offset + count) > buffer.Length) throw new ArgumentException();
      if (_isDisposed) throw new ObjectDisposedException("LZMAStream");

      fixed (byte* b = &buffer[0]) {
        Write(b, buffer.Length, offset, count);
      }
    }

    /// <summary>Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.</summary>
    public override unsafe void Close()
    {
      if (_isClosed)
        return;

      // If we were compressing... There might be stuff left on the
      // temporary buffer
      if (_mode == CompressionMode.Compress && _wasWrittenTo) {
        Write(null, 0, LZMAAction.LZMA_FINISH);
      }
      LZMANative.lzma_end(_zstream);
      _compressedStream.Flush();
      if (CloseUnderlyingStream)
        _compressedStream.Close();
      _tmpBufferHandle.Free();
      _zstreamHandle.Free();
      _isClosed = true;
      _wasWrittenTo = false;
    }

    /// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
    public override bool CanRead { get { return (_mode == CompressionMode.Decompress ? true : false); } }

    public int Granularity
    {
      get { return 1; }
    }

    /// <summary>
    /// Total bytes read by LZMA
    /// </summary>
    public unsafe ulong TotalBytesIn { get { return _zstream->total_in; } }
    /// <summary>
    /// Total bytes written by LZMA
    /// </summary>
    public unsafe ulong TotalBytesOut { get { return _zstream->total_out; } }

    /// <summary>Gets a value indicating whether the stream supports writing.</summary>
    public override bool CanWrite { get { return (_mode == CompressionMode.Compress ? true : false); } }

    /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
    public override bool CanSeek { get { return (false); } }

    /// <summary>Gets a reference to the underlying stream.</summary>
    public Stream BaseStream { get { return (_compressedStream); } }

    #region Not yet supported
    /// <summary>Flushes the contents of the internal buffer of the current GZipStream object to the underlying stream.</summary>
    public override void Flush()
    { }

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