using System;

namespace Daemaged.Compression.LZ4
{
  /// <summary>
  /// The exception that is thrown when an error occurs on the zlib dll
  /// </summary>
  public class LZ4Exception : Exception
  {
    /// <summary>
    /// Gets the error code.
    /// </summary>
    /// <value>The error code.</value>
    public IntPtr ErrorCode { get; private set; }
    /// <summary>
    /// Initializes a new instance of the <see cref="LZ4Exception" /> class with a specified
    /// error message and error code
    /// </summary>
    /// <param name="errorCode">The LZ4 error code that caused the exception</param>
    /// <param name="msg">A message that (hopefully) describes the error</param>
    public LZ4Exception(IntPtr errorCode, string msg) : base(msg)
    {
      ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LZ4Exception"/> class with a specified
    /// error code
    /// </summary>
    /// <param name="errorCode">The lz4 error code that caused the exception</param>
    public LZ4Exception(IntPtr errorCode) : this(errorCode, LZ4Native.GetErrorName(errorCode)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LZ4Exception" /> class with a specified
    /// error code
    /// </summary>
    /// <param name="msg">The message (hopefully) describing this error</param>
    public LZ4Exception(string msg) : this(IntPtr.Zero, msg) { }
  }
}