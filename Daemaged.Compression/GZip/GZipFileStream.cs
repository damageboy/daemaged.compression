using System;
using System.IO;
using System.Security;

namespace Daemaged.Compression.GZip
{
  /// <summary>
  /// Implements a compressed <see cref="Stream"/>, in GZip (.gz) format.
  /// </summary>
  [SuppressUnmanagedCodeSecurity] 
  public class GZipFileStream : Stream
  {
    #region Private data
    public const CompressLevel DEFAULT_COMPRESSION_LEVEL = CompressLevel.Best;
    private readonly IntPtr _gzFile;
    private bool _isDisposed = false;
    private readonly bool _isWriting;
    #endregion

    #region Constructors
    public GZipFileStream(string fileName, FileAccess access)
      : this(fileName, access, DEFAULT_COMPRESSION_LEVEL)
    {}

    /// <summary>
    /// Opens an existing file as a readable GZipFileStream
    /// </summary>
    /// <param name="fileName">The name of the file to open</param>
    /// <param name="access">The file access pattern</param>
    /// <param name="level">The compression level to use</param>
    /// <exception cref="ZLibException">If an error occurred in the internal zlib function</exception>
    public GZipFileStream(string fileName, FileAccess access, CompressLevel level)
    {
      switch (access) {
        case FileAccess.Read:
          _isWriting = false;
          _gzFile = ZLibNative.gzopen(fileName, "rb");
          break;
        case FileAccess.Write:
          _isWriting = true;
          _gzFile = ZLibNative.gzopen(fileName, String.Format("wb{0}", (int)level));
          break;
        case FileAccess.ReadWrite:
          throw new ArgumentException(String.Format("{0} cannot be used with {1}", access, GetType().FullName), "access");
      }
      if (_gzFile == IntPtr.Zero)
        throw new ZLibException(-1, "Could not open " + fileName);

    }
    #endregion

    #region Access properties
    /// <summary>
    /// Returns true of this stream can be read from, false otherwise
    /// </summary>
    public override bool CanRead => !_isWriting;


    /// <summary>
    /// Returns false.
    /// </summary>
    public override bool CanSeek => false;

    public int Granularity => 1;

    /// <summary>
    /// Returns true if this tsream is writeable, false otherwise
    /// </summary>
    public override bool CanWrite => _isWriting;
    #endregion

    #region Destructor & IDispose stuff

    /// <summary>
    /// Destroys this instance
    /// </summary>
    ~GZipFileStream()
    {
      CleanUp(false);
    }

    /// <summary>
    /// Closes the external file handle
    /// </summary>
    public new void Dispose()
    {
      CleanUp(true);
    }

    // Does the actual closing of the file handle.
    private void CleanUp(bool isDisposing)
    {
      if (_isDisposed) return;
      ZLibNative.gzclose(_gzFile);
      _isDisposed = true;
    }
    #endregion

    #region Basic reading and writing
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

    /// <summary>
    /// Attempts to read a number of bytes from the stream.
    /// </summary>
    /// <param name="buffer">The destination data buffer</param>
    /// <param name="length"></param>
    /// <param name="offset">The index of the first destination byte in <c>buffer</c></param>
    /// <param name="count">The number of bytes requested</param>
    /// <returns>The number of bytes read</returns>
    /// <exception cref="ArgumentNullException">If <c>buffer</c> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <c>count</c> or <c>offset</c> are negative</exception>
    /// <exception cref="ArgumentException">If <c>offset</c>  + <c>count</c> is &gt; buffer.Length</exception>
    /// <exception cref="NotSupportedException">If this stream is not readable.</exception>
    /// <exception cref="ObjectDisposedException">If this stream has been disposed.</exception>
    public unsafe int Read(byte *buffer, int length, int offset, int count)
    {
      if (!CanRead) throw new NotSupportedException();
      if (buffer == null) throw new ArgumentNullException();
      if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
      if ((offset + count) > length) throw new ArgumentException();
      if (_isDisposed) throw new ObjectDisposedException("GZipStream");

      var result = ZLibNative.gzread(_gzFile, buffer + offset, count);

      if (result < 0)
        throw new IOException();

      if (result == 0)
        throw new EndOfStreamException();
      return result;
    }

    /// <summary>
    /// Attempts to read a single byte from the stream.
    /// </summary>
    /// <returns>The byte that was read, or -1 in case of error or End-Of-File</returns>
    public override int ReadByte()
    {
      if (!CanRead) throw new NotSupportedException();
      if (_isDisposed) throw new ObjectDisposedException("GZipStream");
      return ZLibNative.gzgetc(_gzFile);
    }

    /// <summary>
    /// Writes a number of bytes to the stream
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <exception cref="ArgumentNullException">If <c>buffer</c> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <c>count</c> or <c>offset</c> are negative</exception>
    /// <exception cref="ArgumentException">If <c>offset</c>  + <c>count</c> is &gt; buffer.Length</exception>
    /// <exception cref="NotSupportedException">If this stream is not writeable.</exception>
    /// <exception cref="ObjectDisposedException">If this stream has been disposed.</exception>
    public unsafe override void Write(byte[] buffer, int offset, int count)
    {
      if (!CanWrite) throw new NotSupportedException();
      if (buffer == null) throw new ArgumentNullException();
      if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
      if ((offset + count) > buffer.Length) throw new ArgumentException();
      if (_isDisposed) throw new ObjectDisposedException("GZipStream");

      fixed (byte* b = &buffer[0])
      {
        Write(b, buffer.Length, offset, count);
      }
    }

    /// <summary>
    /// Writes a number of bytes to the stream
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="length"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <exception cref="ArgumentNullException">If <c>buffer</c> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <c>count</c> or <c>offset</c> are negative</exception>
    /// <exception cref="ArgumentException">If <c>offset</c>  + <c>count</c> is &gt; buffer.Length</exception>
    /// <exception cref="NotSupportedException">If this stream is not writeable.</exception>
    /// <exception cref="ObjectDisposedException">If this stream has been disposed.</exception>
    public unsafe void Write(byte* buffer, int length, int offset, int count)
    {
      if (!CanWrite) throw new NotSupportedException();
      if (buffer == null) throw new ArgumentNullException();
      if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
      if ((offset + count) > length) throw new ArgumentException();
      if (_isDisposed) throw new ObjectDisposedException("GZipStream");

      var result = ZLibNative.gzwrite(_gzFile, buffer + offset, count);
      if (result < 0)
        throw new IOException();
    }

    /// <summary>
    /// Writes a single byte to the stream
    /// </summary>
    /// <param name="value">The byte to add to the stream.</param>
    /// <exception cref="NotSupportedException">If this stream is not writeable.</exception>
    /// <exception cref="ObjectDisposedException">If this stream has been disposed.</exception>
    public override void WriteByte(byte value)
    {
      if (!CanWrite) throw new NotSupportedException();
      if (_isDisposed) throw new ObjectDisposedException("GZipStream");

      var result = ZLibNative.gzputc(_gzFile, value);
      if (result < 0)
        throw new IOException();
    }
    #endregion

    #region Position & length stuff
    /// <summary>
    /// Not supported.
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="NotSupportedException">Always thrown</exception>
    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    ///  Not suppported.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="origin"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">Always thrown</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Flushes the <c>GZipFileStream</c>.
    /// </summary>
    /// <remarks>In this implementation, this method does nothing. This is because excessive
    /// flushing may degrade the achievable compression rates.</remarks>
    public override void Flush()
    {
      // left empty on purpose
    }

    /// <summary>
    /// Gets/sets the current position in the <c>GZipFileStream</c>. Not suppported.
    /// </summary>
    /// <remarks>In this implementation this property is not supported</remarks>
    /// <exception cref="NotSupportedException">Always thrown</exception>
    public override long Position
    {
      get
      {
        throw new NotSupportedException();
      }
      set
      {
        throw new NotSupportedException();
      }
    }

    /// <summary>
    /// Gets the size of the stream. Not suppported.
    /// </summary>
    /// <remarks>In this implementation this property is not supported</remarks>
    /// <exception cref="NotSupportedException">Always thrown</exception>
    public override long Length
    {
      get
      {
        throw new NotSupportedException();
      }
    }
    #endregion
  }
}