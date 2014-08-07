using System;
using System.Collections.Generic;
using System.Text;

namespace Daemaged.Compression
{
  /// <summary>Type of compression to use for the GZipStream. Currently only Decompress is supported.</summary>
  public enum CompressionMode
  {
    /// <summary>Compresses the underlying stream.</summary>
    Compress,
    /// <summary>Decompresses the underlying stream.</summary>
    Decompress,
  }
}
