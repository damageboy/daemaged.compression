namespace Daemaged.Compression.GZip
{
  public enum GZipLevel
  {
    NoCompression = 0,
    BestSpeed = 1,
    BestCompression = 9,
    Default = -1,
  }  

  public enum GZipStrategy
  {
    Filtered = 1,
    HuffmanOnly = 2,
    Rle = 3,
    Fixed = 4,
    Default = 0,
  }

  public class GZipOptions
  {
    public GZipOptions()
    {
      WindowBits = 15;
      MemoryLevel = 8;
      Strategy = GZipStrategy.Default;
      PredefinedLevel = GZipLevel.Default;
      // Z_DEFLATED is the only supported version as of now
      Method = 8;
    }
    public GZipLevel PredefinedLevel
    { 
      set {
        Level = (int) value;
      }
    }
    public int Level { get; set; }
    public GZipStrategy Strategy { get; set; }
    public int WindowBits { get; set; }
    public int MemoryLevel { get; set; }
    public int Method { get; set; }
  }
}