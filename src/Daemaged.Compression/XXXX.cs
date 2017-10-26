 using System;
using System.IO;

namespace Daemaged.Compression
{
  public unsafe interface IUnsafeStream : IDisposable
  {
    //long Seek(long offset, SeekOrigin origin);
    //void Flush();
    //long Position { get; set; }
    //bool CanRead { get; }
    //bool CanSeek { get; }
    //int Granularity { get; }
    //bool CanWrite { get; }
    //int Read(byte[] buffer, int offset, int count);
    //void Write(byte[] buffer, int offset, int count);
    int Read(byte* buffer, int length, int count);
    void Write(byte* buffer, int length, int count);
    //long Length { get; }
    //void SetLength(long value);
    //void Close();
  }
}