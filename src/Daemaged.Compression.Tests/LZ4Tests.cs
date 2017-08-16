using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using Daemaged.Compression.LZ4;
using NUnit.Framework;

namespace Daemaged.Compression.Tests
{
  [TestFixture]
  public class LZ4Tests
  {
    private static string _lz4Exe;
    private readonly List<string> _tmpFilesCreateDuringThisTest = new List<string>();
    private const int KB = 1024;
    private const int MB = KB*KB;

    private const string RELATIVE_PATH_TO_LZ4_EXE = "../../liblz4/";

    private static string Lz4ExePath
    {
      get
      {
        if (_lz4Exe != null)
          return _lz4Exe;
        var arch = IntPtr.Size == 8 ? "x64" : "x86";
        _lz4Exe = Path.Combine(TestContext.CurrentContext.TestDirectory, RELATIVE_PATH_TO_LZ4_EXE, arch, "lz4");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
          _lz4Exe += ".exe";

        if (!File.Exists(_lz4Exe))
          throw new Exception("Can't file the lz4 executable");

        return _lz4Exe;
      }
    }

    private static void FileCompare(string fn1, string fn2)
    {
      if (fn1 == fn2)
        throw new ArgumentException($"{nameof(FileCompare)} was called with the same file as both parameters");

      int f1b;
      int f2b;

      // Open the two files.
      using (var fs1 = new FileStream(fn1, FileMode.Open))
      using (var fs2 = new FileStream(fn2, FileMode.Open)) {
        Assert.That(fs1.Length, Is.EqualTo(fs2.Length), "Files do not match in size");

        do {
          f1b = fs1.ReadByte();
          f2b = fs2.ReadByte();
          // This reduces the amount of boxing/unboxing that it's woth it even though it looks stupid
          // We still do the Assert.That() since we want it to bomb "properly" when it is gone bad
          if (f1b != f2b)
            Assert.That(f1b, Is.EqualTo(f2b));
        } while ((f1b != -1));
      }
    }

    private static void CompressWithLZ4EXE(string uncompressed, string compressed, Lz4CompressionLevel level)
    {
      var psi = new ProcessStartInfo {
        FileName = Lz4ExePath,
        CreateNoWindow = true,
        Arguments = $"-f -{(int) level} {uncompressed} {compressed}",
        UseShellExecute = false,
      };
      var p = Process.Start(psi);
      p.WaitForExit();
      if (p.ExitCode != 0)
        throw new Exception($"lz4 executable failed with error {p.ExitCode}");
    }

    private void UncompressWithLZ4EXE(string compressed, string uncompressed)
    {
      var p = Process.Start(Lz4ExePath, string.Format("-d -f {0} {1}", compressed, uncompressed));
      p.WaitForExit();
      if (p.ExitCode != 0)
        throw new Exception(string.Format("lz4 executable failed with error {0}", p.ExitCode));
    }

    private string PrepareFileWithRandomGarbage(int length)
    {
      var file = Path.GetTempFileName();

      var r = new Random((int) (DateTime.UtcNow.Ticks & 0xFFFFFFFF));

      var data = new byte[length];
      r.NextBytes(data);

      using (var s = File.OpenWrite(file))
        s.Write(data, 0, length);

      _tmpFilesCreateDuringThisTest.Add(file);
      return file;
    }

    [Test]
    public void LZ4ExeIsSane()
    {
      var uncompressedFile = PrepareFileWithRandomGarbage(4 * MB);
      var compressedFile = AddExt(uncompressedFile, ".lz4");
      var testFile = AddExt(uncompressedFile, ".test");
      CompressWithLZ4EXE(uncompressedFile, compressedFile, Lz4CompressionLevel.Best);
      UncompressWithLZ4EXE(compressedFile, testFile);
      FileCompare(uncompressedFile, testFile);
    }

    [Test]
    [Combinatorial]
    public void CompressionWithFixedBufferSize(
      [Values(4*KB, 400*KB, 4*MB, 40*MB)] int size,
      [Values(128, 512, KB, 4*KB, 400 * KB, 4 * MB, 40 * MB)] int bufferSize,
      [Values(Lz4CompressionLevel.Fastest, Lz4CompressionLevel.Best)]  Lz4CompressionLevel level
      )
    {
      var uncompressedFile = PrepareFileWithRandomGarbage(size);
      var compressedFile = AddExt(uncompressedFile, ".lz4");
      var testManagedFile = AddExt(uncompressedFile, ".testmanaged");
      var testNativeFile = AddExt(uncompressedFile, ".testnative");

      CompressWithLZ4Stream(uncompressedFile, compressedFile, () => bufferSize, bufferSize, level);
      UncompressWithLZ4EXE(compressedFile, testNativeFile);
      UncompressWithLZ4Stream(compressedFile, testManagedFile, () => bufferSize, bufferSize);
      FileCompare(uncompressedFile, testManagedFile);
      FileCompare(uncompressedFile, testNativeFile);
    }


    [Test]
    [Combinatorial]
    public void DecompressionWithFixedBufferSize(
      [Values(4 * KB, 400 * KB, 4 * MB, 16 * MB)] int size,
      [Values(4 * KB, 400 * KB, 4 * MB, 16 * MB)] int bufferSize,
      [Values(Lz4CompressionLevel.Fastest, Lz4CompressionLevel.Best)] Lz4CompressionLevel level
      )
    {
      var uncompressedFile = PrepareFileWithRandomGarbage(size);
      var compressedFile = AddExt(uncompressedFile, ".lz4");
      var testManagedFile = AddExt(uncompressedFile, ".testmanaged");

      CompressWithLZ4EXE(uncompressedFile, compressedFile, level);
      UncompressWithLZ4Stream(compressedFile, testManagedFile, () => bufferSize, bufferSize);
      FileCompare(uncompressedFile, testManagedFile);
    }

    [Test]
    [Combinatorial]
    public void DecompressionWithRandomBufferSize(
      [Values(4*KB, 400*KB, 4*MB, 8*MB)] int size,
      [Values(111, 222, 333, 444, 555, 666, 777, 888, 999)] int bufferSizeSeed,
      [Values(Lz4CompressionLevel.Fastest, Lz4CompressionLevel.Best)] Lz4CompressionLevel level
      )
    {
      var uncompressedFile = PrepareFileWithRandomGarbage(size);
      var compressedFile = AddExt(uncompressedFile, ".lz4");
      var testManagedFile = AddExt(uncompressedFile, ".testmanaged");
      var r = new Random(bufferSizeSeed);

      CompressWithLZ4EXE(uncompressedFile, compressedFile, level);
      UncompressWithLZ4Stream(compressedFile, testManagedFile, () => r.Next(1, size), size);
      FileCompare(uncompressedFile, testManagedFile);
    }

    private void UncompressWithLZ4Stream(string compressedFile, string uncompressedFile, Func<int> bufferSizeGenerator, int bufferSize)
    {
      using (var cs = File.OpenRead(compressedFile))
      using (var us = File.OpenWrite(uncompressedFile))
      using (var lz4s = new LZ4Stream(cs, CompressionMode.Decompress))
      {
        lz4s.CopyToWithBufferSizeGenerator(us, bufferSizeGenerator, bufferSize);
      }
    }

    private static void CompressWithLZ4Stream(string uncompressedFile, string compressedFile, Func<int> bufferSizeGenerator, int bufferSize, Lz4CompressionLevel level)
    {
      using (var us = File.OpenRead(uncompressedFile))
      using (var cs = File.OpenWrite(compressedFile))
      using (var lz4s = new LZ4Stream(cs, CompressionMode.Compress, level))
      {
        us.CopyToWithBufferSizeGenerator(lz4s, bufferSizeGenerator, bufferSize);
      }
    }


    private string AddExt(string testFile, string ext)
    {
      var lz4 = testFile + ext;
      _tmpFilesCreateDuringThisTest.Add(lz4);
      return lz4;
    }

    [SetUp]
    public void Setup()
    {
      Environment.SetEnvironmentVariable("OVERRIDE_NATIVE_LIBLZ4", TestContext.CurrentContext.TestDirectory);
    }

    [TearDown]
    public void TearDown()
    {
      foreach (var f in _tmpFilesCreateDuringThisTest)
        try { File.Delete(f); } catch { }
    }
  }

  public static class StreamExtensions
  {
    public static void CopyToWithBufferSizeGenerator(this Stream src, Stream dest, Func<int> bufferSizeGenerator, int maxBufferSize)
    {
      var buffer = new byte[maxBufferSize];
      int count;
      while ((count = src.Read(buffer, 0, bufferSizeGenerator())) != 0)
        dest.Write(buffer, 0, count);
    }

  }
}
