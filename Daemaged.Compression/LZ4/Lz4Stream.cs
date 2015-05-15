using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using Daemaged.Compression.GZip;

namespace Daemaged.Compression.LZ4
{
  public class LZ4Stream : Stream, IDisposable
  {
    #region Private Members

    private static int LZ4S_GetBlockSize_FromBlockId(int id)
    {
      return (1 << (8 + (2*id)));
    }

    private const int LZ4S_BLOCKSIZEID_DEFAULT = 7;

    private bool _finishedReading;
    private bool _isClosed;
    private bool _isDisposed;
    private int _globalBlockSizeId = LZ4S_BLOCKSIZEID_DEFAULT;
    private int _filesize;
    private int _nextToRead;
    private CompressionMode _mode;
    private int _compressedfilesize;

    private BufferWrapper _inBuff;
    private BufferWrapper _outBuff;

    private const int KB = 1024;
    private const int DecompressionBufferSize = 64 * KB;

    private Stream _baseStream;
    private int _blockSizeBytes;
    private unsafe void* _ctx;
    private Lz4LibReturnCode _returnCode;
    private Lz4Preferences _prefs;

    #endregion Private Members

    #region Ctor

    public unsafe LZ4Stream(Stream baseStream, CompressionMode mode,
      Lz4CompressionLevel level = Lz4CompressionLevel.Fastest, Lz4BlockSize blockSize = Lz4BlockSize.Max4Mb)
    {
      if (baseStream == null) throw new ArgumentNullException("baseStream");

      // Init
      _baseStream = baseStream;
      _mode = mode;

      void* ctx;
      Lz4LibReturnCode returnCode;
      switch (mode)
      {
        case CompressionMode.Compress:
          returnCode = LZ4LibNative.LZ4F_createCompressionContext(&ctx, LZ4LibNative.Lz4Version);
          _ctx = ctx;

          if (returnCode != Lz4LibReturnCode.Ok)
            throw new LZ4LibException((int) returnCode, returnCode.ToString());

          _prefs.frameInfo.blockMode = Lz4BlockMode.Independent;
          _prefs.frameInfo.blockSize = blockSize;
          _prefs.frameInfo.contentChecksum = Lz4ContentChecksum.Enabled;
          _prefs.autoFlush = Lz4AutoFlush.Enabled;
          _prefs.CompressionLevel = level;
          _blockSizeBytes = LZ4LibNative.GetBlockSize(blockSize);

          _inBuff = new BufferWrapper(_blockSizeBytes);
          _outBuff = new BufferWrapper(LZ4LibNative.LZ4F_compressBound(_blockSizeBytes, ref _prefs));

          var headerSize = LZ4LibNative.LZ4F_compressBegin(_ctx, _outBuff.Ptr, _outBuff.Size, ref _prefs);
          if (LZ4LibNative.LZ4F_isError(headerSize) != 0)
            throw new LZ4LibException(_nextToRead, String.Format("File header generation failed: {0}", (Lz4LibReturnCode) _nextToRead));

          _baseStream.Write(_outBuff.Buffer, 0, headerSize);

          _compressedfilesize += headerSize;
          break;
        case CompressionMode.Decompress:
          returnCode = LZ4LibNative.LZ4F_createDecompressionContext(&ctx, LZ4LibNative.Lz4Version);
          _ctx = ctx;

          if (returnCode != Lz4LibReturnCode.Ok)
          {
            throw new LZ4LibException((int) returnCode, returnCode.ToString());
          }
          var sizeCheck = sizeof(int);
          //var sizeCheck = 7;

          using (var header = new BufferWrapper(LZ4LibNative.HeaderMaxSize))
          {
            Console.WriteLine("{0:X8}", (ulong) header.Ptr);
            _baseStream.Read(header.Buffer, 0, sizeCheck);
            Console.WriteLine("{0:X8}", (ulong) header.Ptr);
            var outBuffSize = 0;
            _nextToRead = LZ4LibNative.LZ4F_decompress(_ctx, null, &outBuffSize, header.Ptr, &sizeCheck, null);

            if (_nextToRead < 0)
              throw new LZ4LibException(_nextToRead,
                string.Format("Decompression error: {0}", (Lz4LibReturnCode) _nextToRead));

            if (_nextToRead > LZ4LibNative.HeaderMaxSize)
              throw new LZ4LibException(_nextToRead,
                string.Format("Header too large ({0} > {1})", _nextToRead, LZ4LibNative.HeaderMaxSize));
            Console.WriteLine("{0:X8}", (ulong) header.Ptr);
            sizeCheck = _baseStream.Read(header.Buffer, 0, _nextToRead);
            if (sizeCheck != _nextToRead)
              throw new IOException("Read Error");

            _nextToRead = LZ4LibNative.LZ4F_decompress(_ctx, null, &outBuffSize, header.Ptr, &sizeCheck, null);
          }

          Lz4FrameInfo frameInfo;
          var inBuffSize = _inBuff.Size;
          returnCode = LZ4LibNative.LZ4F_getFrameInfo(_ctx, &frameInfo, null, &inBuffSize);
          if (returnCode != Lz4LibReturnCode.Ok) {
            throw new LZ4LibException((int) returnCode,
              string.Format("can't decode frame header: {0}", returnCode));
          }

          _outBuff = new BufferWrapper(LZ4LibNative.GetBlockSize(frameInfo.blockSize));
          _inBuff = new BufferWrapper(_outBuff.Size + 4);
          break;
        default:
          throw new ArgumentOutOfRangeException("mode", string.Format("Unsupported mode: {0}", mode));
      }
    }

    #endregion Ctor

    #region Stream API

    public override bool CanRead
    {
      get { return _mode == CompressionMode.Decompress; }
    }

    public override bool CanSeek
    {
      get { return false; }
    }

    public override bool CanWrite
    {
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
      if (_mode == CompressionMode.Compress) throw new NotSupportedException("Can't read on a compress stream!");
      if (_finishedReading) return 0;

      var totalRead = 0;
      var sizeLeftToRead = count;

      while (sizeLeftToRead > 0)
      {
        if (_outBuff.AvailableToRead > 0)
        {
          var sizeRead = Math.Min(Math.Min(count, _outBuff.AvailableToRead), sizeLeftToRead);
          Copy(_outBuff.Buffer, _outBuff.RemainingOffset, buffer, count - sizeLeftToRead, sizeRead);
          sizeLeftToRead -= sizeRead;
          totalRead += sizeRead;
          _outBuff.AvailableToRead -= sizeRead;
          _outBuff.RemainingOffset += sizeRead;
        }
        else
        {
          var actuallyRead = _baseStream.Read(_inBuff.Buffer, 0, _nextToRead);
          if (actuallyRead < 0) throw new LZ4LibException("Read Error");
          //if (actuallyRead > _outBuff.Size) throw new Exception("Out buffer size is not large enough");

          var outBuffSize = _outBuff.Size;
          _nextToRead = LZ4LibNative.LZ4F_decompress(_ctx, _outBuff.Ptr, &outBuffSize, _inBuff.Ptr, &actuallyRead, null);
          if (LZ4LibNative.LZ4F_isError(_nextToRead) != 0)
            throw new LZ4LibException(_nextToRead, String.Format("Decompression error: {0}", (Lz4LibReturnCode)_nextToRead));

          if (outBuffSize == 0)
          {
            // this is how we know we're done
            _finishedReading = true;
            break;
          }

          _outBuff.AvailableToRead = outBuffSize;
          _outBuff.RemainingOffset = 0;
        }
      }

      return totalRead;
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
        throw new ArgumentOutOfRangeException("count", "Can't compress negative amount of bytes");
      Contract.EndContractBlock();

      var compressSize = _outBuff.Size - 50;
      //var maxBuffSize = _outBuff.Size;

      var bufferHandler = GCHandle.Alloc(buffer, GCHandleType.Pinned);
      var bufferPtr = (byte*)bufferHandler.AddrOfPinnedObject().ToPointer();
      bufferPtr += offset;

      do {
        // Compress the contents from buffer and put the compressed contents
        // in _outBuff (maximum block/buffer size chunks)
        var sizeWritten =
          LZ4LibNative.LZ4F_compressUpdate(_ctx, _outBuff.Ptr, _outBuff.Size, bufferPtr, Math.Min(compressSize, count), ref _prefs);
        if (LZ4LibNative.LZ4F_isError(sizeWritten) != 0)
          throw new LZ4LibException(sizeWritten, string.Format("Compression failed: {0}", (Lz4LibReturnCode) sizeWritten));

        // Write compressed contents from _outBuff to _baseStream
        _baseStream.Write(_outBuff.Buffer, 0, sizeWritten);
        bufferPtr += compressSize;
        count -= compressSize;
      } while (count > 0);
    }

    public override unsafe void Close()
    {
      if (_isClosed) return;
      _isClosed = true;

      if (_mode == CompressionMode.Compress)
      {
        // Mark end of stream
        var options = new Lz4CompressOptions();
        var headerSize = LZ4LibNative.LZ4F_compressEnd(_ctx, _outBuff.Ptr, _outBuff.Size, ref options);
        if (LZ4LibNative.LZ4F_isError(headerSize) != 0)
        {
          Console.WriteLine("36, End of file generation failed : " + ((Lz4LibReturnCode)headerSize));
          Debugger.Break();
        }

        _baseStream.Write(_outBuff.Buffer, 0, headerSize);
        _compressedfilesize += headerSize;

        var returnCode = LZ4LibNative.LZ4F_freeCompressionContext(_ctx);
        if (returnCode != Lz4LibReturnCode.Ok) throw new LZ4LibException(69, "Error : can't free LZ4F context resource : " + returnCode);
      }
      else
      {
        // Decompress

        var returnCode = LZ4LibNative.LZ4F_freeDecompressionContext(_ctx);
        if (returnCode != Lz4LibReturnCode.Ok) throw new LZ4LibException(69, "Error : can't free LZ4F context resource : " + returnCode);
      }

      _baseStream.Close();

      if (_inBuff != null) {
        _inBuff.Dispose();
        _inBuff = null;
      }
      if (_outBuff != null) {
        _outBuff.Dispose();
        _outBuff = null;
      }
    }

    void IDisposable.Dispose() { Dispose(); }

    private new void Dispose()
    {
      if (_isDisposed) return;

      Close();
      _isDisposed = true;
    }

    #endregion Stream API

    #region Helper Functions

    private static unsafe void Copy(byte[] src, int srcIndex, byte[] dst, int dstIndex, int count)
    {
      if (src == null || srcIndex < 0 || dst == null || dstIndex < 0 || count < 0)
      {
        throw new ArgumentException();
      }

      int srcLen = src.Length;
      int dstLen = dst.Length;
      if (srcLen - srcIndex < count || dstLen - dstIndex < count)
      {
        throw new ArgumentException();
      }

      // The following fixed statement pins the location of the src and dst objects
      // in memory so that they will not be moved by garbage collection.
      fixed (byte* pSrc = src, pDst = dst)
      {
        byte* ps = pSrc;
        byte* pd = pDst;

        ps += srcIndex;
        pd += dstIndex;

        // Loop over the count in blocks of 4 bytes, copying an integer (4 bytes) at a time:
        for (int i = 0; i < count/4; i++)
        {
          *((int*) pd) = *((int*) ps);
          pd += 4;
          ps += 4;
        }

        // Complete the copy by moving any bytes that weren't moved in blocks of 4:
        for (int i = 0; i < count%4; i++)
        {
          *pd = *ps;
          pd++;
          ps++;
        }
      }
    }

    #endregion Helper Functions

    #region Unsupported

    public override long Length
    {
      get { throw new NotImplementedException(); }
    }

    public override long Position
    {
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

  public unsafe class BufferWrapper : IDisposable
  {
    public BufferWrapper(int size)
    {
      Buffer = new byte[size];
      _gch = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
      Ptr = (byte*) _gch.AddrOfPinnedObject().ToPointer();
      Size = size;
    }
    public int Size { get; private set; }
    public byte[] Buffer { get; private set; }
    public byte* Ptr { get; private set; }
    private GCHandle _gch;
    public int AvailableToRead { get; set; }
    public int RemainingOffset { get; set; }
    public void Dispose()
    {
      _gch.Free();
    }
  }
}
