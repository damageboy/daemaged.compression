using System;

namespace Daemaged.Compression.GZip
{
  /// <summary>
  /// The exception that is thrown when an error occurs on the zlib dll
  /// </summary>
  public class ZLibException : ApplicationException
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ZLibException"/> class with a specified
    /// error message and error code
    /// </summary>
    /// <param name="errorCode">The zlib error code that caused the exception</param>
    /// <param name="msg">A message that (hopefully) describes the error</param>
    public ZLibException(int errorCode, string msg)
      : base(String.Format("ZLib error {0} {1}", errorCode, msg))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZLibException"/> class with a specified
    /// error code
    /// </summary>
    /// <param name="errorCode">The zlib error code that caused the exception</param>
    public ZLibException(int errorCode)
      : base(String.Format("ZLib error {0}", errorCode))
    {
    }
  }

  /// <summary>
  /// Encapsulates general information about the ZLib library
  /// </summary>
  public class ZLibInfo
  {

    #region Private stuff

    uint _flags;

    // helper function that unpacks a bitsize mask
    static int bitSize(uint bits)
    {
      switch (bits)
      {
        case 0: return 16;
        case 1: return 32;
        case 2: return 64;
      }
      return -1;
    }
    #endregion

    /// <summary>
    /// Constructs an instance of the <c>ZLibInfo</c> class.
    /// </summary>
    public ZLibInfo()
    {
      _flags = ZLibNative.zlibCompileFlags();
    }

    /// <summary>
    /// True if the library is compiled with debug info
    /// </summary>
    public bool HasDebugInfo => 0 != (_flags & 0x100);

    /// <summary>
    /// True if the library is compiled with assembly optimizations
    /// </summary>
    public bool UsesAssemblyCode => 0 != (_flags & 0x200);

    /// <summary>
    /// Gets the size of the unsigned int that was compiled into Zlib
    /// </summary>
    public int SizeOfUInt => bitSize(_flags & 3);

    /// <summary>
    /// Gets the size of the unsigned long that was compiled into Zlib
    /// </summary>
    public int SizeOfULong => bitSize((_flags >> 2) & 3);

    /// <summary>
    /// Gets the size of the pointers that were compiled into Zlib
    /// </summary>
    public int SizeOfPointer => bitSize((_flags >> 4) & 3);

    /// <summary>
    /// Gets the size of the z_off_t type that was compiled into Zlib
    /// </summary>
    public int SizeOfOffset => bitSize((_flags >> 6) & 3);

    /// <summary>
    /// Gets the version of ZLib as a string, e.g. "1.2.1"
    /// </summary>
    public static string Version => ZLibNative.zlibVersion();
  }
}