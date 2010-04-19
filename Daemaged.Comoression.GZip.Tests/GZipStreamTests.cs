using System;
using System.IO;
using Daemaged.Compression.GZip;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using NUnit.Framework;

namespace Daemaged.Comoression.GZip.Tests
{
  [TestFixture]
  [PexClass(typeof(GZipStream))]
  [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
  [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
  partial class GZipStreamTests
  {
    [PexMethod]
    public GZipStream Constructor(Stream stream, CompressionMode mode, GZipOptions options)
    { return new GZipStream(stream, mode, options); }
  }
}
