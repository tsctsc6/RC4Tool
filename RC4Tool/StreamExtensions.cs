namespace RC4Tool;

public static class StreamExtensions
{
    public static IEnumerable<byte> ReadBytes(this Stream fs)
    {
        while(fs.CanRead)
        {
            int b = fs.ReadByte();
            if (b == -1) yield break;
            yield return (byte)b;
        }
    }

    public static void WriteBytes(this Stream fs, IEnumerable<byte> bytes)
    {
        foreach(var b in bytes)
        {
            fs.WriteByte(b);
        }
    }
}
