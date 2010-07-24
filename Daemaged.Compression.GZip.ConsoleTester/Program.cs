using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Daemaged.Compression.GZip;
using Daemaged.Compression.LZMA;

namespace Daemaged.Compression.GZip.ConsoleTester
{
  class Program
  {
    static void Main(string[] args)
    {
      var sgz = GzipTest();
      var sxz = LZMATest();
      Console.WriteLine(sgz);
      Console.WriteLine(sxz);
    }

    private static string GzipTest()
    {
      var z = new GZipStream(new FileStream("test.gz", FileMode.Create), Daemaged.Compression.GZip.CompressionMode.Compress)
                {CloseUnderlyingStream = true};
      var b = Encoding.ASCII.GetBytes("Just some test string to compress with gzip");
      z.Write(b, 0, b.Length);
      z.Reset();
      z.Write(b, 0, b.Length);
      z.Close();

      var t = new GZipStream(new FileStream("test.gz", FileMode.Open), Daemaged.Compression.GZip.CompressionMode.Decompress)
                {CloseUnderlyingStream = true};
      t.Read(b, 0, b.Length);
      var s = Encoding.ASCII.GetString(b);      
      t.Reset();
      t.Read(b, 0, b.Length);
      return s;
    }

    private static string LZMATest()
    {
      var z = new LZMAStream(new FileStream("test.xz", FileMode.Create), 
        Daemaged.Compression.LZMA.CompressionMode.Compress,
        new LZMAOptionLZMA(9))
      {CloseUnderlyingStream = true};
      var b = Encoding.ASCII.GetBytes("Just some test string to compress with gzip");
      z.Write(b, 0, b.Length);
      z.Close();

      var t = new LZMAStream(new FileStream("test.xz", FileMode.Open), Daemaged.Compression.LZMA.CompressionMode.Decompress) { CloseUnderlyingStream = true };
      t.Read(b, 0, b.Length);
      var s = Encoding.ASCII.GetString(b);
      return s;
    }

  }
}
