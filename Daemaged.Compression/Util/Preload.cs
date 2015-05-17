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

  internal class Preload
  {
    private static ProcessorArchitecture _processorArchitecture = ProcessorArchitecture.Unknown;

    private static Dictionary<ProcessorArchitecture, string> ArchDirMapper = new Dictionary
      <ProcessorArchitecture, string>
    {
      {ProcessorArchitecture.AMD64, "x64"},
      {ProcessorArchitecture.Intel, "x86"},
      {ProcessorArchitecture.IA32_on_Win64, "x86"},
      {ProcessorArchitecture.ARM, "arm"},
    };




    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string fileName);

    [DllImport("kernel32", CallingConvention = CallingConvention.Winapi)]
    private static extern void GetSystemInfo(out SYSTEM_INFO systemInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_INFO
    {
      public ProcessorArchitecture wProcessorArchitecture;
      public ushort wReserved; /* NOT USED */
      public uint dwPageSize; /* NOT USED */
      public IntPtr lpMinimumApplicationAddress; /* NOT USED */
      public IntPtr lpMaximumApplicationAddress; /* NOT USED */
      public uint dwActiveProcessorMask; /* NOT USED */
      public uint dwNumberOfProcessors; /* NOT USED */
      public uint dwProcessorType; /* NOT USED */
      public uint dwAllocationGranularity; /* NOT USED */
      public ushort wProcessorLevel; /* NOT USED */
      public ushort wProcessorRevision; /* NOT USED */
    }

    public static ProcessorArchitecture ProcessorArchitecture
    {
      get
      {
        if (_processorArchitecture != ProcessorArchitecture.Unknown)
          return _processorArchitecture;

        SYSTEM_INFO sysInfo;
        GetSystemInfo(out sysInfo);

        _processorArchitecture = sysInfo.wProcessorArchitecture;
        return _processorArchitecture;
      }
    }

    public static IntPtr Load(string name)
    {
      var overrideVariable = "OVERRIDE_NATIVE_" + name.ToUpper();
      var disableVariable = "DISABLE_PRELOAD_" + name.ToUpper();

      if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(disableVariable)))
        return IntPtr.Zero;

      var nativeSearchPath = Environment.GetEnvironmentVariable(overrideVariable);

      if (nativeSearchPath == null || !Directory.Exists(nativeSearchPath))
      {
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (String.IsNullOrEmpty(asmDir))
          throw new TypeLoadException(
            string.Format("Failed to load {0}.{1} because the .NET assembly loation could not be determined", name,
              NativeExtension));
        nativeSearchPath = asmDir;
      }

      var dllName = Path.Combine(nativeSearchPath, ProcessorArchitectureDirectory, name + "." + NativeExtension);
      if (!File.Exists(dllName))
        throw new TypeLoadException(string.Format("Failed to load {0}.{1} the native module doesn't not exist", name, NativeExtension));

      return LoadLibrary(dllName);
    }

    public static string ProcessorArchitectureDirectory { get { return ArchDirMapper[ProcessorArchitecture]; } }

    public static string NativeExtension { get { return "dll"; } }
  }
}
