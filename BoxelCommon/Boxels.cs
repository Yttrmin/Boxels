using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using ProtoBuf;

namespace BoxelCommon
{
    public abstract class BaseBoxel
    {

    }

    [ProtoContract]
    public class BasicBoxel : IBoxel
    {
        [ProtoMember(1)]
        public float Scale { get; private set; }
        public Int3 Position { get { return this._Position; } private set { this._Position = value; } }
        [ProtoMember(5)]
        public int Type { get; private set; }
        public IBoxelContainer Container { get; set; }
        [ProtoMember(2)]
        private int X { get { return this.Position.X; } set { this._Position.X = value; } }
        [ProtoMember(3)]
        private int Y { get { return this.Position.Y; } set { this._Position.Y = value; } }
        [ProtoMember(4)]
        private int Z { get { return this.Position.Z; } set { this._Position.Z = value; } }
        private Int3 _Position;

        private BasicBoxel()
        {

        }

        public BasicBoxel(Int3 Position, float Scale, int Type)
        {
            this.Position = Position;
            this.Scale = Scale;
            this.Type = Type;
        }

        public override string ToString()
        {
            return this.Position.ToString();
        }
    }
}
