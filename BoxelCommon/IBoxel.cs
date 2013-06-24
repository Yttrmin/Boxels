using SharpDX;

namespace BoxelLib
{
    public interface IBoxel
    {
        float Scale { get; }
        Int3 Position { get; }
        int Type { get; }
    }
}
