using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Daemaged.Compression.Util
{
  internal enum ProcessorArchitecture : ushort {
    Intel = 0,
    MIPS = 1,
    Alpha = 2,
    PowerPC = 3,
    SHx = 4,
    ARM = 5,
    IA64 = 6,
    Alpha64 = 7,
    MSIL = 8,
    AMD64 = 9,
    IA32_on_Win64 = 10,
    Unknown = 0xFFFF
  }

  public static class NativePreloadHelper
  {
    static ProcessorArchitecture _processorArchitecture = ProcessorArchitecture.Unknown;

    static readonly Dictionary<ProcessorArchitecture, string> ArchDirMapper =
      new Dictionary<ProcessorArchitecture, string> {
        { ProcessorArchitecture.AMD64, "x64"},
        { ProcessorArchitecture.Intel, "x86"},
        { ProcessorArchitecture.IA32_on_Win64, "x86"},
        { ProcessorArchitecture.ARM, "arm"},
      };

    [DllImport("kernel32", SetLastError = true)]
    static extern IntPtr LoadLibrary(string fileName);

    [DllImport("kernel32", CallingConvention = CallingConvention.Winapi)]
    static extern void GetSystemInfo(out SYSTEM_INFO systemInfo);

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_INFO
    {
      public readonly ProcessorArchitecture ProcessorArchitecture;
      readonly ushort wReserved; /* NOT USED */
      readonly uint dwPageSize; /* NOT USED */
      readonly IntPtr lpMinimumApplicationAddress; /* NOT USED */
      readonly IntPtr lpMaximumApplicationAddress; /* NOT USED */
      readonly uint dwActiveProcessorMask; /* NOT USED */
      readonly uint dwNumberOfProcessors; /* NOT USED */
      readonly uint dwProcessorType; /* NOT USED */
      readonly uint dwAllocationGranularity; /* NOT USED */
      readonly ushort wProcessorLevel; /* NOT USED */
      readonly ushort wProcessorRevision; /* NOT USED */
    }

    static ProcessorArchitecture ProcessorArchitecture
    {
      get
      {
        if (_processorArchitecture != ProcessorArchitecture.Unknown)
          return _processorArchitecture;

        SYSTEM_INFO sysInfo;
        GetSystemInfo(out sysInfo);

        _processorArchitecture = sysInfo.ProcessorArchitecture;
        return _processorArchitecture;
      }
    }

    /// <exception cref="TypeLoadException">The native module could not be loaded.</exception>
    public static IntPtr Preload(string name)
    {
      var overrideVariable = "OVERRIDE_NATIVE_" + name.ToUpper();
      var disableVariable = "DISABLE_PRELOAD_" + name.ToUpper();

      if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(disableVariable)))
        return IntPtr.Zero;

      var nativeSearchPath = Environment.GetEnvironmentVariable(overrideVariable);

      if (nativeSearchPath == null || !Directory.Exists(nativeSearchPath))
      {
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(asmDir))
          throw new TypeLoadException($"Failed to load {name}.{NativeExtension} because the .NET assembly location could not be determined");
        nativeSearchPath = asmDir;
      }

      var dllName = Path.Combine(nativeSearchPath, ProcessorArchitectureDirectory, name + "." + NativeExtension);
      if (!File.Exists(dllName))
        throw new TypeLoadException($"Failed to load {name}.{NativeExtension} the native module doesn't not exist");

      return LoadLibrary(dllName);
    }

    public static string ProcessorArchitectureDirectory => ArchDirMapper[ProcessorArchitecture];

    public static string NativeExtension { get { return "dll"; } }
  }
}
