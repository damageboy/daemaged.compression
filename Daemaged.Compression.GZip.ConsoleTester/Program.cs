using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Daemaged.Compression.GZip;

namespace Daemaged.Compression.GZip.ConsoleTester
{
  class Program
  {
    static void Main(string[] args)
    {
      var z = new GZipStream(new FileStream("test", FileMode.Create), CompressionMode.Compress)
                {CloseUnderlyingStream = true};
      var b = Encoding.ASCII.GetBytes("Just some test string to compress");
      z.Write(b, 0, b.Length);
      z.Reset();
      z.Write(b, 0, b.Length);
      z.Close();

      var t = new GZipStream(new FileStream("test", FileMode.Open), CompressionMode.Decompress)
                {CloseUnderlyingStream = true};
      t.Read(b, 0, b.Length);
      var s = Encoding.ASCII.GetString(b);
      
      Console.WriteLine(s);
      t.Reset();
      t.Read(b, 0, b.Length);
      Console.WriteLine(s);
    }
  }
}
