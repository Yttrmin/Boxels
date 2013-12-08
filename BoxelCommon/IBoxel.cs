using BoxelCommon;
using SharpDX;

namespace BoxelCommon
{
    [ProtoBuf.ProtoContract]
    [ProtoBuf.ProtoInclude(1, typeof(BasicBoxel))]
    public interface IBoxel : IPositionable
    {
        float Scale { get; }
        Int3 Position { get; }
        int Type { get; }
        IBoxelContainer Container { get; set; }
    }

    public interface IPositionable
    {
        Int3 Position { get; }
    }
}
