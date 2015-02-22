using System;
using System.IO;
using System.Text;
using Daemaged.Compression;
using Daemaged.Compression.GZip;
using Daemaged.Compression.LZMA;


namespace Daemaged.Compression.ConsoleTester
{
  class Program
  {
    static void Main(string[] args)
    {
      //var x = LZOTest();
      var sgz = GzipTest();
      //Console.WriteLine(sgz);
      var sxz = LZMATest();      
      Console.WriteLine(sxz);
    }

#if XXX

    private static unsafe string LZOTest()
    {
      var b = Encoding.ASCII.GetBytes("Just some test string to compress with gzip");

      byte[] dst;
      byte[] decomp;
      var size = LZO2Native.LZO1x115Compress(b, out dst);
      Console.WriteLine(size);
      LZO2Native.lzo1x_decompress(dst, out decomp, b.Length);
      return "";
    }
#endif
    private static string GzipTest()
    {
      var z = new GZipStream(new FileStream("test.gz", FileMode.Create), CompressionMode.Compress)
                {CloseUnderlyingStream = true};
      var b = Encoding.ASCII.GetBytes("Just some test string to compress with gzip");
      z.Write(b, 0, b.Length);
      z.Reset();
      z.Write(b, 0, b.Length);
      z.Close();
      return "";
      var t = new GZipStream(new FileStream("test.gz", FileMode.Open), CompressionMode.Decompress)
                {CloseUnderlyingStream = true};
      t.Read(b, 0, b.Length);
      var s = Encoding.ASCII.GetString(b);      
      t.Reset();
      t.Read(b, 0, b.Length);
      File.Delete("test.xz");
      return s;
    }

    private static unsafe string LZMATest()
    {
      //var opts = new LZMAOptionLZMA(9);
      Console.WriteLine("opt-size {0}", sizeof(LZMAOptionLZMA));
      Console.WriteLine("stream-size {0}", sizeof(LZMAStreamNative));
      Console.WriteLine("filter-size {0}", sizeof(LZMAFilter));
      var z = new LZMAStream(new FileStream("test.xz", FileMode.Create), CompressionMode.Compress, 3)
      {CloseUnderlyingStream = true};
      var b = Encoding.ASCII.GetBytes("Just some test string to compress with lzma");
      z.Write(b, 0, b.Length);
      z.Close();
      Console.WriteLine("{0} bytes read, {1} bytes written", z.TotalBytesIn, z.TotalBytesOut);

      var t = new LZMAStream(new FileStream("test.xz", FileMode.Open), CompressionMode.Decompress) { CloseUnderlyingStream = true };
      t.Read(b, 0, b.Length);
      var s = Encoding.ASCII.GetString(b);
      Console.WriteLine("{0} bytes read, {1} bytes written", t.TotalBytesIn, t.TotalBytesOut);

      Console.ReadLine();

      var bc = new byte[1000];
      int ip;
      int op;
      var compRet = LZMAHelper.CompressBuffer(b, bc, out op, 9);
      Console.WriteLine("{0} bytes read, {1} bytes written, {2}", b.Length, op, compRet);
      op = 0;
      var decRet = LZMAHelper.DecompressBuffer(bc, out ip, b, out op);
      Console.WriteLine("{0} bytes read, {1} bytes written, {2}", ip, op, decRet);
      File.Delete("test.xz");
      return s;
    }

  }
}
