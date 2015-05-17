using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using Daemaged.Compression.Util;

namespace Daemaged.Compression.LZ4
{
  using size_t = IntPtr;

  public class LZ4Stream : Stream, IDisposable
  {
    #region Private Members
    static int LZ4S_GetBlockSize_FromBlockId(int id)
    {
      return (1 << (8 + (2*id)));
    }

    const int LZ4S_BLOCKSIZEID_DEFAULT = 7;

    bool _finishedReading;
    bool _isClosed;
    bool _isDisposed;
    int _globalBlockSizeId = LZ4S_BLOCKSIZEID_DEFAULT;
    ulong _nextSizeToRead;
    readonly CompressionMode _mode;

    NativeBuffer _srcBuffer;
    NativeBuffer _dstBuffer;

    const int KB = 1024;
    const int DecompressionBufferSize = 64*KB;

    readonly Stream _baseStream;
    int _blockSizeBytes;
    readonly unsafe void* _ctx;
    readonly unsafe Lz4Preferences* _prefs;
    int _srcPos;
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
    public unsafe LZ4Stream(Stream baseStream, CompressionMode mode,
      Lz4CompressionLevel level = Lz4CompressionLevel.Fastest, Lz4BlockSize blockSize = Lz4BlockSize.Max4Mb)
    {
      if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));

      // Init
      _baseStream = baseStream;
      _mode = mode;

      void* ctx;
      size_t rc;
      switch (mode) {
        case CompressionMode.Compress:
          rc = LZ4Native.LZ4F_createCompressionContext(&ctx, LZ4Native.Lz4Version);
          _ctx = ctx;

          if (LZ4Native.LZ4F_isError(rc))
            throw new LZ4Exception(rc);

          _prefs = (Lz4Preferences*) Marshal.AllocHGlobal(sizeof (Lz4Preferences)).ToPointer();
          _prefs->frameInfo.blockMode = Lz4BlockMode.Independent;
          _prefs->frameInfo.blockSize = blockSize;
          _prefs->frameInfo.contentChecksum = Lz4ContentChecksum.Enabled;
          _prefs->autoFlush = Lz4AutoFlush.Enabled;
          _prefs->CompressionLevel = level;

          _blockSizeBytes = LZ4Native.GetBlockSize(blockSize);

          _srcBuffer = new NativeBuffer(_blockSizeBytes);
          _dstBuffer = new NativeBuffer(_blockSizeBytes);
          //_dstBuffer = new NativeBuffer(LZ4Native.LZ4F_compressBound(_blockSizeBytes, _prefs));

          var headerSize = LZ4Native.LZ4F_compressBegin(_ctx, _dstBuffer.Ptr, (IntPtr) _dstBuffer.Size, _prefs);
          if (LZ4Native.LZ4F_isError(headerSize))
            throw new LZ4Exception(headerSize,
              $"File header generation failed: {LZ4Native.GetErrorName(headerSize)}");

          _baseStream.Write(_dstBuffer.Buffer, 0, headerSize.ToInt32());

          break;
        case CompressionMode.Decompress:
          rc = LZ4Native.LZ4F_createDecompressionContext(&ctx, LZ4Native.Lz4Version);
          _ctx = ctx;

          if (LZ4Native.LZ4F_isError(rc))
            throw new LZ4Exception(rc);

          _srcBuffer = new NativeBuffer(DecompressionBufferSize);
          _dstBuffer = new NativeBuffer(DecompressionBufferSize);

          baseStream.Read(_srcBuffer.Buffer, 0, sizeof (int));

          var outSize = 0UL;
          var inSize = (ulong) sizeof (int);
          rc = LZ4Native.LZ4F_decompress(_ctx, _dstBuffer.Ptr, (IntPtr*) &outSize, _srcBuffer.Ptr, (IntPtr*) &inSize,
            null);

          if (LZ4Native.LZ4F_isError(rc))
            throw new LZ4Exception(rc);

          _nextSizeToRead = (ulong) rc;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(mode), $"Unsupported mode: {mode}");
      }
    }
    #endregion Ctor

    #region Stream API
    public override bool CanRead {
      get { return _mode == CompressionMode.Decompress; }
    }

    public override bool CanSeek {
      get { return false; }
    }

    public override bool CanWrite {
      get { return _mode == CompressionMode.Compress; }
    }

    public override void Flush()
    {
      _baseStream.Flush();
    }

    /// <summary>
    /// inBuff is used to read the stream's uncompressed content
    /// outBuff is used to get the contents after the compression
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public override unsafe int Read(byte[] buffer, int offset, int count)
    {
      if (buffer == null) throw new ArgumentNullException();
      if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "length cannot be negative");
      if ((offset + count) > buffer.Length)
        throw new ArgumentException("offset + count exceeds the length of the supplied buffer");
      if (_isDisposed) throw new ObjectDisposedException(nameof(LZ4Stream));
      Contract.EndContractBlock();
      fixed (byte* b = &buffer[0]) {
        return Read(b + offset, count);
      }
    }

    public unsafe int Read(byte* p, int length)
    {
      if (!CanRead) throw new NotSupportedException();
      if (p == null) throw new ArgumentNullException();
      if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative");
      if (_isDisposed) throw new ObjectDisposedException(nameof(LZ4Stream));
      Contract.EndContractBlock();

      if (_finishedReading)
        return 0;

      var decompressedSize = 0;
      var decodedBytes = (ulong) length;

      while (_nextSizeToRead > 0 && decompressedSize < length) {
        int readSize;
        _srcPos = 0;

        if (_nextSizeToRead > (ulong) _srcBuffer.Size)
          _nextSizeToRead = (ulong) _srcBuffer.Size;
        readSize = _baseStream.Read(_srcBuffer.Buffer, 0, (int) _nextSizeToRead);
        if (readSize == 0)
          break;

        while (_srcPos < readSize) {
          var remaining = (ulong) (readSize - _srcPos);
          decodedBytes = (ulong) (length - decompressedSize);
          var rc = LZ4Native.LZ4F_decompress(_ctx, p, (IntPtr*) &decodedBytes, _srcBuffer.Ptr + _srcPos,
            (IntPtr*) &remaining, null);
          if (LZ4Native.LZ4F_isError(rc))
            throw new LZ4Exception(rc, $"Decompression error: {LZ4Native.GetErrorName(rc)}");
          _nextSizeToRead = (ulong) rc;
          _srcPos += (int) remaining;
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
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public override unsafe void Write(byte[] buffer, int offset, int count)
    {
      if (_mode == CompressionMode.Decompress) throw new NotSupportedException("Can't write on a decompress stream!");

      if (count == 0)
        return;
      if (count < 0)
        throw new ArgumentOutOfRangeException(nameof(count), "Can't compress negative amount of bytes");
      Contract.EndContractBlock();
    }

    public override unsafe void Close()
    {
      if (_isClosed) return;
      _isClosed = true;

      if (_mode == CompressionMode.Compress) {
        // Mark end of stream
        var options = new Lz4CompressOptions();
        var headerSize = LZ4Native.LZ4F_compressEnd(_ctx, _dstBuffer.Ptr, (IntPtr) _dstBuffer.Size, &options);
        if (LZ4Native.LZ4F_isError(headerSize)) {
          Console.WriteLine("36, End of file generation failed : " + ((Lz4LibReturnCode) headerSize));
          Debugger.Break();
        }

        _baseStream.Write(_dstBuffer.Buffer, 0, headerSize.ToInt32());

        var rc = LZ4Native.LZ4F_freeCompressionContext(_ctx);
        if (LZ4Native.LZ4F_isError(rc))
          throw new LZ4Exception(rc,
            $"Error : can't free LZ4F context resource : {LZ4Native.GetErrorName(rc)}");
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