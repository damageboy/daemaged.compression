using System;
using System.IO;
using Daemaged.Compression.GZip;
using Daemaged.Compression;
using NUnit.Framework;

namespace Daemaged.Comoression.GZip.Tests
{
  partial class GZipStreamTests
  {
    [Test]
    public GZipStream Constructor(Stream stream, CompressionMode mode, GZipOptions options)
    { return new GZipStream(stream, mode, options); }
  }
}
