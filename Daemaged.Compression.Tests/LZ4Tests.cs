using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Daemaged.Compression.LZ4;
using NUnit.Framework;

namespace Daemaged.Compression.Tests
{
  [TestFixture]
  public class LZ4Tests
  {
    private static string _lz4Exe;
    private List<string> _tmpFilesCreateDuringThisTest = new List<string>();
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

    private bool FileCompare(string file1, string file2)
    {
      int file1byte;
      int file2byte;
      FileStream fs1;
      FileStream fs2;

      // Determine if the same file was referenced two times.
      if (file1 == file2)
      {
        // Return true to indicate that the files are the same.
        return true;
      }

      // Open the two files.
      fs1 = new FileStream(file1, FileMode.Open);
      fs2 = new FileStream(file2, FileMode.Open);

      // Check the file sizes. If they are not the same, the files
      // are not the same.
      if (fs1.Length != fs2.Length)
      {
        // Close the file
        fs1.Close();
        fs2.Close();

        // Return false to indicate files are different
        return false;
      }

      // Read and compare a byte from each file until either a
      // non-matching set of bytes is found or until the end of
      // file1 is reached.
      do
      {
        // Read one byte from each file.
        file1byte = fs1.ReadByte();
        file2byte = fs2.ReadByte();
      }
      while ((file1byte == file2byte) && (file1byte != -1));

      // Close the files.
      fs1.Close();
      fs2.Close();

      // Return the success of the comparison. "file1byte" is
      // equal to "file2byte" at this point only if the files are
      // the same.
      return ((file1byte - file2byte) == 0);
    }

    private void CompressWithLZ4EXE(string uncompressed, string compressed, Lz4CompressionLevel level)
    {
      var psi = new ProcessStartInfo {
        FileName = Lz4ExePath,
        CreateNoWindow = true,
        Arguments = string.Format("-f -{0} {1} {2}", (int) level, uncompressed, compressed),
        UseShellExecute = false,
      };
      var p = Process.Start(psi);
      p.WaitForExit();
      if (p.ExitCode != 0)
        throw new Exception(string.Format("lz4 executable failed with error {0}", p.ExitCode));
    }

    private void UncompressWithExternalLZ4EXE(string compressed, string uncompressed)
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
      UncompressWithExternalLZ4EXE(compressedFile, testFile);
      Assert.That(FileCompare(uncompressedFile, testFile), Is.True);
    }

    [Test]
    [Combinatorial]
    public void Compression(
      [Values(4*KB, 400*KB, 4*MB, 40*MB)] int size,
      [Values(4*KB, 400 * KB, 4 * MB, 40 * MB)] int bufferSize,
      [Values(Lz4CompressionLevel.Fastest, Lz4CompressionLevel.Best)]  Lz4CompressionLevel level
      )
    {
      var uncompressedFile = PrepareFileWithRandomGarbage(size);
      var compressedFile = AddExt(uncompressedFile, ".lz4");
      var testManagedFile = AddExt(uncompressedFile, ".testmanaged");
      var testNativeFile = AddExt(uncompressedFile, ".testnative");

      CompressWithLZ4Stream(uncompressedFile, compressedFile, bufferSize, level);
      UncompressWithLZ4Stream(compressedFile, testManagedFile, bufferSize);
      UncompressWithExternalLZ4EXE(compressedFile, testNativeFile);
      Assert.That(FileCompare(uncompressedFile, testManagedFile), Is.True);
      Assert.That(FileCompare(uncompressedFile, testNativeFile), Is.True);
    }


    [Test]
    [Combinatorial]
    public void Decompression(
      [Values(4 * KB, 400 * KB, 4 * MB, 40 * MB)] int size,
      [Values(4 * KB, 400 * KB, 4 * MB, 40 * MB)] int bufferSize,
      [Values(Lz4CompressionLevel.Fastest, Lz4CompressionLevel.Best)]  Lz4CompressionLevel level
      )
    {
      var uncompressedFile = PrepareFileWithRandomGarbage(size);
      var compressedFile = AddExt(uncompressedFile, ".lz4");
      var testManagedFile = AddExt(uncompressedFile, ".testmanaged");

      CompressWithLZ4EXE(uncompressedFile, compressedFile, level);
      UncompressWithLZ4Stream(compressedFile, testManagedFile, bufferSize);
      Assert.That(FileCompare(uncompressedFile, testManagedFile), Is.True);
    }


    private void UncompressWithLZ4Stream(string compressedFile, string uncompressedFile, int bufferSize)
    {
      using (var cs = File.OpenRead(compressedFile))
      using (var us = File.OpenWrite(uncompressedFile))
      using (var lz4s = new LZ4Stream(cs, CompressionMode.Decompress))
      {
        lz4s.CopyTo(us, bufferSize);
      }

    }

    private static void CompressWithLZ4Stream(string uncompressedFile, string compressedFile, int bufferSize, Lz4CompressionLevel level)
    {
      using (var us = File.OpenRead(uncompressedFile))
      using (var cs = File.OpenWrite(compressedFile))
      using (var lz4s = new LZ4Stream(cs, CompressionMode.Compress, level))
      {
        us.CopyTo(lz4s, bufferSize);
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
      Environment.SetEnvironmentVariable("LZ4NATIVE_OVERRIDE", TestContext.CurrentContext.TestDirectory);
    }

    [TearDown]
    public void TearDown()
    {
      foreach (var f in _tmpFilesCreateDuringThisTest)
        try { File.Delete(f); } catch { }
    }
  }
}
