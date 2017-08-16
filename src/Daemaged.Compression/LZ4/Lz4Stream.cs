using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using Daemaged.Compression.Util;
using static Daemaged.Compression.LZ4.LZ4Native;

namespace Daemaged.Compression.LZ4
{
  using size_t = IntPtr;

  public class LZ4Stream : Stream, IDisposable
  {
    #region Private Members

    bool _isClosed;
    bool _isDisposed;

    ulong _nextSizeToRead;
    readonly CompressionMode _mode;

    NativeBuffer _srcBuffer;
    NativeBuffer _dstBuffer;

    const int DECOMPRESSION_BUFFER_SIZE = 64*KB;

    static int GetBlockSizeFromBlockId(int id) => (1 << (8 + (2 * id)));

    // Used for compression/decompression
    readonly Stream _baseStream;
    // Used for compression/decompression
    readonly unsafe void* _ctx;
    //readonly unsafe LZ4Preferences* _prefs;
    #endregion Private Members

    #region Ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="LZ4Stream"/> class.
    /// </summary>
    /// <param name="baseStream">The base stream.</param>
    /// <param name="mode">The compression mode</param>
    /// <param name="level">The compression level. Relevant for <see cref="CompressionMode.Compress"/></param>
    /// <param name="blockSize">Size of the block.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="LZ4Exception">
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">mode</exception>
    /// <exception cref="OutOfMemoryException">There is insufficient memory to satisfy the request.</exception>
    /// <exception cref="ArgumentException">Steam compression mode contradicts base-stream read/write availability</exception>
    public unsafe LZ4Stream(Stream baseStream, CompressionMode mode, Lz4CompressionLevel level = Lz4CompressionLevel.Fastest, LZ4BlockSize blockSize = LZ4BlockSize.Max4Mb)
    {
      if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));
      if (mode == CompressionMode.Compress && !baseStream.CanWrite)
        throw new ArgumentException("stream is set to compress, but base-stream cannot be written to", nameof(baseStream));
      if (mode == CompressionMode.Decompress && !baseStream.CanRead)
        throw new ArgumentException("stream is set to decompress, but base-stream cannot be read from", nameof(baseStream));
      Contract.EndContractBlock();

      _baseStream = baseStream;
      _mode = mode;

      void* ctx;
      size_t rc;
      switch (mode) {
        case CompressionMode.Compress:
          rc = LZ4F_createCompressionContext(&ctx, Lz4Version);
          _ctx = ctx;

          if (LZ4F_isError(rc))
            throw new LZ4Exception(rc);

          LZ4Preferences prefs;
          prefs.AutoFlush = Lz4AutoFlush.Enabled;
          prefs.CompressionLevel = level;
          prefs.FrameInfo.BlockMode = LZ4BlockMode.Independent;
          prefs.FrameInfo.BlockSize = blockSize;
          prefs.FrameInfo.ContentChecksum = LZ4ContentChecksum.Enabled;

          MaxInputBufferSize = GetBlockSize(blockSize);
          _dstBuffer = new NativeBuffer(LZ4F_compressFrameBound((IntPtr) MaxInputBufferSize).ToInt32());

          var headerSize = LZ4F_compressBegin(_ctx, _dstBuffer.Ptr, (IntPtr) _dstBuffer.Size, &prefs);
          if (LZ4F_isError(headerSize))
            throw new LZ4Exception(headerSize,
              $"File header generation failed: {GetErrorName(headerSize)}");

          _baseStream.Write(_dstBuffer.Buffer, 0, headerSize.ToInt32());
          break;
        case CompressionMode.Decompress:
          rc = LZ4F_createDecompressionContext(&ctx, Lz4Version);
          _ctx = ctx;

          if (LZ4F_isError(rc))
            throw new LZ4Exception(rc);

          _srcBuffer = new NativeBuffer(DECOMPRESSION_BUFFER_SIZE);

          baseStream.Read(_srcBuffer.Buffer, 0, sizeof (int));

          var outSize = 0UL;
          var inSize = (ulong) sizeof (int);
          var frameHeader = stackalloc byte[MAX_FRAME_HEADER_SIZE];
          rc = LZ4F_decompress(_ctx, frameHeader, (IntPtr*) &outSize, _srcBuffer.Ptr, (IntPtr*) &inSize);

          if (LZ4F_isError(rc))
            throw new LZ4Exception(rc);

          _nextSizeToRead = (ulong) rc;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(mode), $"Unsupported mode: {mode}");
      }
    }

    public int MaxInputBufferSize { get; }
    #endregion Ctor

    #region Stream API
    public override bool CanRead => _mode == CompressionMode.Decompress;

    public override bool CanSeek => false;

    public override bool CanWrite => _mode == CompressionMode.Compress;

    /// <exception cref="IOException">An I/O error occurs.</exception>
    public override void Flush()
    {
      _baseStream.Flush();
    }

    /// <summary>
    /// inBuff is used to read the stream's uncompressed content
    /// outBuff is used to get the contents after the compression
    /// </summary>
    /// <param name="buffer">The buffer to fill with decompressed data.</param>
    /// <param name="offset">The offset into the buffer to start filling from.</param>
    /// <param name="count">The amount of available space in bytes in the buffer</param>
    /// <returns>The amount of bytes written to the buffer with decompressed data</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">offset + count exceeds the length of the supplied buffer</exception>
    /// <exception cref="ArgumentOutOfRangeException">offset cannot be negative.</exception>
    /// <exception cref="ArgumentOutOfRangeException">count cannot be negative.</exception>
    public override unsafe int Read(byte[] buffer, int offset, int count)
    {
      if (buffer == null) throw new ArgumentNullException(nameof(buffer));
      if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "cannot be negative");
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "cannot be negative");
      if ((offset + count) > buffer.Length) throw new ArgumentException("offset + count exceeds the length of the supplied buffer");
      Contract.EndContractBlock();
      fixed (byte* b = &buffer[0]) {
        return Read(b + offset, count);
      }
    }

    /// <exception cref="NotSupportedException">The stream must be in decompress mode to read from.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="p"/> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">length cannot be negative.</exception>
    /// <exception cref="ObjectDisposedException">The method cannot be called after the object has been disposed.</exception>
    public unsafe int Read(byte* p, int length)
    {
      if (!CanRead) throw new NotSupportedException($"{nameof(LZ4Stream)} must be in decompress mode to read from");
      if (p == null) throw new ArgumentNullException(nameof(p));
      if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative");
      if (_isDisposed) throw new ObjectDisposedException(nameof(LZ4Stream));
      Contract.EndContractBlock();

      var decompressedSize = 0;
      var decodedBytes = (ulong) length;

      while (_nextSizeToRead > 0 && decompressedSize < length) {
        if (_srcBuffer.ConsumedOffset == _srcBuffer.AvailableToRead) {
          if (_nextSizeToRead > (ulong) _srcBuffer.Size)
            _nextSizeToRead = (ulong) _srcBuffer.Size;
          _srcBuffer.AvailableToRead = _baseStream.Read(_srcBuffer.Buffer, 0, (int) _nextSizeToRead);
          _srcBuffer.ConsumedOffset = 0;
          if (_srcBuffer.AvailableToRead == 0)
            break;
        }

        while (_srcBuffer.ConsumedOffset < _srcBuffer.AvailableToRead) {
          var remaining = (ulong) (_srcBuffer.AvailableToRead - _srcBuffer.ConsumedOffset);
          decodedBytes = (ulong) (length - decompressedSize);
          var rc = LZ4F_decompress(_ctx, p, (IntPtr*) &decodedBytes, _srcBuffer.Ptr + _srcBuffer.ConsumedOffset, (IntPtr*) &remaining, null);
          if (LZ4F_isError(rc))
            throw new LZ4Exception(rc, $"Decompression error: {GetErrorName(rc)}");
          _nextSizeToRead = (ulong) rc;
          _srcBuffer.ConsumedOffset += (int) remaining;
          decompressedSize += (int) decodedBytes;
          p += decodedBytes;
          if (decompressedSize == length)
            return decompressedSize;
        }
      }
      return decompressedSize;
    }

    /// <summary>
    /// inBuff is used to read the buffer's uncompressed content - reads block size chunks
    /// outBuff is used to get the contents after the compression and write to _basestream
    /// </summary>
    /// <param name="buffer">The buffer with data ready to be compressed</param>
    /// <param name="offset">The offset into the buffer to start compressing from</param>
    /// <param name="count">The amount of available space in bytes in the buffer</param>
    /// <exception cref="ArgumentNullException"><paramref name=""/> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">cannot be negative.</exception>
    /// <exception cref="ArgumentOutOfRangeException">length cannot be negative.</exception>
    /// <exception cref="ArgumentException">offset + count exceeds the length of the supplied buffer</exception>
    public override unsafe void Write(byte[] buffer, int offset, int count)
    {
      if (buffer == null) throw new ArgumentNullException(nameof(buffer));
      if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "cannot be negative");
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "length cannot be negative");
      if ((offset + count) > buffer.Length) throw new ArgumentException("offset + count exceeds the length of the supplied buffer");
      Contract.EndContractBlock();
      if (count == 0)
        return;

      fixed (byte* b = &buffer[0]) {
        Write(b + offset, count);
      }
    }

    /// <summary>
    /// Writes the specified buffer into the compression stream.
    /// </summary>
    /// <param name="p">The buffer pointer</param>
    /// <param name="length">The length of the passed <paramref name="p"/>.</param>
    /// <exception cref="NotSupportedException">The stream must be in compress mode to write to.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="p"/> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">length cannot be negative.</exception>
    /// <exception cref="ArgumentOutOfRangeException">length cannot be greater than the max block size that this <see cref="LZ4Stream"/> was constructed with.</exception>
    /// <exception cref="ObjectDisposedException">The method cannot be called after the object has been disposed.</exception>
    public unsafe void Write(byte* p, int length)
    {
      if (!CanWrite) throw new NotSupportedException("The stream must be in compress mode to write to");
      if (p == null) throw new ArgumentNullException(nameof(p));
      if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative");
      //if (length > MaxInputBufferSize) throw new ArgumentOutOfRangeException(nameof(length), $"cannot be > the max block size ({MaxInputBufferSize}) that the stream was constructed with");
      if (_isDisposed) throw new ObjectDisposedException(nameof(LZ4Stream));
      Contract.EndContractBlock();

      while (length > 0) {
        var srcBytes = (IntPtr) Math.Min(MaxInputBufferSize, length);
        var bytesCompressed = LZ4F_compressUpdate(_ctx, _dstBuffer.Ptr, (IntPtr) _dstBuffer.Size, p, srcBytes);
        if (LZ4F_isError(bytesCompressed))
          throw new LZ4Exception(bytesCompressed, $"Compression failed: {GetErrorName(bytesCompressed)}");
        p += (int) srcBytes;
        length -= (int) srcBytes;
        if (bytesCompressed != IntPtr.Zero)
          _baseStream.Write(_dstBuffer.Buffer, 0, (int) bytesCompressed);
      }
    }

    public override unsafe void Close()
    {
      if (_isClosed)
        return;

      _isClosed = true;

      if (_mode == CompressionMode.Compress) {
        // Mark end of stream
        var endSize = LZ4F_compressEnd(_ctx, _dstBuffer.Ptr, (IntPtr) _dstBuffer.Size);
        if (LZ4F_isError(endSize))
          throw new LZ4Exception(endSize, $"End of file generation failed {GetErrorName(endSize)}");
        _baseStream.Write(_dstBuffer.Buffer, 0, endSize.ToInt32());

        var rc = LZ4F_freeCompressionContext(_ctx);
        if (LZ4F_isError(rc))
          throw new LZ4Exception(rc, $"Error : can't free LZ4F context resource: {GetErrorName(rc)}");
      } else {
        // Decompress
        var rc = LZ4Native.LZ4F_freeDecompressionContext(_ctx);
        if (LZ4Native.LZ4F_isError(rc))
          throw new LZ4Exception(rc,
            $"Error : can't free LZ4F context resource : {LZ4Native.GetErrorName(rc)}");
      }

      _baseStream.Close();

      if (_srcBuffer != null) {
        _srcBuffer.Dispose();
        _srcBuffer = null;
      }
      if (_dstBuffer != null) {
        _dstBuffer.Dispose();
        _dstBuffer = null;
      }
    }

    void IDisposable.Dispose()
    {
      Dispose();
    }

    new void Dispose()
    {
      if (_isDisposed) return;

      Close();
      _isDisposed = true;
    }
    #endregion Stream API

    #region Unsupported
    public override long Length {
      get { throw new NotImplementedException(); }
    }

    public override long Position {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
      throw new NotImplementedException();
    }
    #endregion Unsupported
  }
}