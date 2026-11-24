namespace Jerrycurl.Mvc;

public interface ISqlWritable
{
    public void WriteTo(ISqlBuffer buffer);
}
