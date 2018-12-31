using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CppSharp;
using CppSharp.Generators;

namespace CodeGen
{
  class Program
  {
    public class ZLibCodeGenerator : ILibrary
    {
      public void Setup(Driver driver)
      {
        var options = driver.Options;

        options.GeneratorKind = GeneratorKind.CSharp;
        options.GenerateSequentialLayout = true;

        var zlib = options.AddModule("ZLibNative");
        zlib.OutputNamespace = "ZLibNative";
        zlib.Headers.Add(Path.GetFullPath(@"../../native/src/zlib-ng/zlib.h"));
        zlib.LibraryDirs.Add(Path.GetFullPath(@"../../../../../XSpeedNew/libs/"));

        switch (Environment.OSVersion.Platform)
        {
          case PlatformID.Win32S:
          case PlatformID.Win32Windows:
          case PlatformID.Win32NT:
          case PlatformID.WinCE:
            xspeed.Libraries.Add(@"DFITCSECMdApi.dll");
            xspeed.Libraries.Add(@"DFITCSECTraderApi.dll");
            break;
          case PlatformID.Unix:
            xspeed.Libraries.Add(@"libDFITCSECMdApi.so");
            xspeed.Libraries.Add(@"libDFITCSECTraderApi.so");
            break;
          case PlatformID.Xbox:
          case PlatformID.MacOSX:
          default:
            throw new ArgumentOutOfRangeException();
        }
      }


    static void Main(string[] args)
    {
      ConsoleDriver.Run(new ZLibCodeGenerator());
    }
  }
}
