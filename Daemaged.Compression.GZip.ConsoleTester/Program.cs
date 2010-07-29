using System;
using System.IO;
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
      var z = new GZipStream(new FileStream("test.gz", FileMode.Create), CompressionMode.Compress)
                {CloseUnderlyingStream = true};
      var b = Encoding.ASCII.GetBytes("Just some test string to compress with gzip");
      z.Write(b, 0, b.Length);
      z.Reset();
      z.Write(b, 0, b.Length);
      z.Close();

      var t = new GZipStream(new FileStream("test.gz", FileMode.Open), CompressionMode.Decompress)
                {CloseUnderlyingStream = true};
      t.Read(b, 0, b.Length);
      var s = Encoding.ASCII.GetString(b);      
      t.Reset();
      t.Read(b, 0, b.Length);
      return s;
    }

    private static string LZMATest()
    {
      var opts = new LZMAOptionLZMA(9);
      var z = new LZMAStream(new FileStream("test.xz", FileMode.Create), 
        LZMA.CompressionMode.Compress,
        ref opts)
      {CloseUnderlyingStream = true};
      var b = Encoding.ASCII.GetBytes("Just some test string to compress with gzip");
      z.Write(b, 0, b.Length);
      z.Close();
      Console.WriteLine("{0} bytes read, {1} bytes written", z.TotalBytesIn, z.TotalBytesOut);

      var t = new LZMAStream(new FileStream("test.xz", FileMode.Open), LZMA.CompressionMode.Decompress) { CloseUnderlyingStream = true };
      t.Read(b, 0, b.Length);
      var s = Encoding.ASCII.GetString(b);
      Console.WriteLine("{0} bytes read, {1} bytes written", t.TotalBytesIn, t.TotalBytesOut);

      var bc = new byte[1000];
      int ip;
      int op;
      var compRet = LZMAHelper.CompressBuffer(b, bc, out op, 9);
      Console.WriteLine("{0} bytes read, {1} bytes written, {2}", b.Length, op, compRet);
      op = 0;
      var decRet = LZMAHelper.DecompressBuffer(bc, out ip, b, out op);
      Console.WriteLine("{0} bytes read, {1} bytes written, {2}", ip, op, decRet);
      return s;
    }

  }
}
