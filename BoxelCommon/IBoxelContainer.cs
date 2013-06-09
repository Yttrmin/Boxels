using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;

namespace BoxelCommon
{
    public interface IBoxelContainer
    {
        IBoxel AtOrDefault(Int3 Position);
        void Add(IBoxel Boxel, Int3 Position);
        IEnumerable<IBoxel> AllBoxels { get; }
        IEnumerable<IBoxel> BoxelsInRadius(Byte3 ChunkPosition, int RadiusInBoxels);
        int Count { get; }
        void Compact();
    }
}
