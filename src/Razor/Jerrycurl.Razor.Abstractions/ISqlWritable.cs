namespace Jerrycurl.Razor;

public interface ISqlWritable
{
    public void WriteTo(ISqlBuffer buffer);
}
