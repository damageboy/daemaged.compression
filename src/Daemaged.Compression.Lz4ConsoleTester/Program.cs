using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daemaged.Compression.LZ4;

namespace Daemaged.Compression.Lz4ConsoleTester
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Compressing...");
      CompressFile(@"D:\LZ4\GLB_MA5_1993_12.csv", @"D:\LZ4\GLB_MA5_1993_12.csv.lz4", Lz4CompressionLevel.Best);

      //Console.WriteLine("Building text...");
      //var builder = new StringBuilder(100000);
      //for (int i = 0; i < 100000; i++)
      //{
      //  builder.Append("sfsdkfhskdjfhksduhfksduhfkusdhkfuh sdf sdf sfs" + i +
      //                 " dfs fushfkuhskufehwfgn   sdgf ds gkusdhgku sd g sd gkuhskugh  g s guukshgkus hg  gsd sd g skguhsdkuhg ksd gds g sughskug" +
      //                 Environment.NewLine);
      //}
      //Console.WriteLine("Compressing text...");
      //CompressText(builder.ToString(), @"D:\LZ4\sometext.txt.lz4", Lz4CompressionLevel.Best);

      //Console.WriteLine("Decompressing...");
      //DecompressFile(@"D:\LZ4\PREDS-MA5-DTFast2-250.csv.lz4", @"D:\LZ4\PREDS-MA5-DTFast2-250.csv.lz4.csv", 1000);

      Console.WriteLine("Decompressing...");
      DecompressFile(@"D:\LZ4\GLB_MA5_1993_12.csv.lz4", @"D:\LZ4\GLB_MA5_1993_12.csv.lz4.csv", 1000);

      //Console.WriteLine("Decompressing...");
      //DecompressText(@"D:\LZ4\sometext.txt.lz4", @"D:\LZ4\sometext.txt.lz4.txt", 1000);
    }

    static void CompressFile(string sourceFile, string destinationFile, Lz4CompressionLevel level)
    {
      using (var writer = new FileStream(destinationFile, FileMode.Create))
      using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
      using (var reader = new StreamReader(sourceStream))
      using (var lz4Stream = new LZ4Stream(writer, CompressionMode.Compress, Lz4CompressionLevel.Fastest))
      {
        var allContent = reader.ReadToEnd();
        var allContentBytes = Encoding.ASCII.GetBytes(allContent);
        lz4Stream.Write(allContentBytes, 0, allContentBytes.Length);
      }
    }

    static void CompressText(string text, string destinationFile, Lz4CompressionLevel level)
    {
      using (var writer = new FileStream(destinationFile, FileMode.Create))
      using (var lz4Stream = new LZ4Stream(writer, CompressionMode.Compress, Lz4CompressionLevel.Fastest))
      {
        var lineBytes = Encoding.ASCII.GetBytes(text);
        lz4Stream.Write(lineBytes, 0, lineBytes.Length);
      }
    }

    static void DecompressFile(string sourceFile, string destinationFile, int bufferSize = 100)
    {
      using (var writerStream = new FileStream(destinationFile, FileMode.Create))
      using (var writer = new StreamWriter(writerStream))
      using (var stream = new FileStream(sourceFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
      using (var lz4Stream = new LZ4Stream(stream, CompressionMode.Decompress))
      {
        foreach (var line in lz4Stream.ReadLines())
        {
          writer.WriteLine(line);
        }
      }
    }

    static void DecompressText(string sourceFile, string destinationFile, int bufferSize = 100)
    {
      using (var writerStream = new FileStream(destinationFile, FileMode.Create))
      using (var writer = new StreamWriter(writerStream))
      using (var stream = new FileStream(sourceFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
      using (var lz4Stream = new LZ4Stream(stream, CompressionMode.Decompress))
      using (var lz4Reader = new StreamReader(lz4Stream))
      {
        while (lz4Reader.Peek() > 0)
        {
          var line = lz4Reader.ReadLine();
          writer.WriteLine(line);
        }
      }
    }
  }

  static class StreamExtensions
  {
    public static IEnumerable<string> ReadLines(this Stream stream, Encoding encoding = null)
    {
      using (var reader = new StreamReader(stream, encoding ?? Encoding.Default)) {
        string line;
        while ((line = reader.ReadLine()) != null)
          yield return line;
      }
    }
  }
}
