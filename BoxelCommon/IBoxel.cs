using SharpDX;

namespace BoxelLib
{
    public interface IBoxel
    {
        float Scale { get; }
        Int3 Position { get; }
    }

    public class BasicBoxel : IBoxel
    {
        public float Scale { get; private set; }
        public Int3 Position { get; private set; }

        public BasicBoxel(Int3 Position, float Scale)
        {
            this.Position = Position;
            this.Scale = Scale;
        }
    }
}
