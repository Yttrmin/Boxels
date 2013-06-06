using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NativeWrappers;
using SharpDX;

namespace BoxelLib
{
    public class BoxelManager
    {
        public struct BoxelManagerSettings
        {
            public int Length;
            public int Width;
            public int Height;
            public bool UseChunks;
            public int ChunkLength;
            public int ChunkWidth;
            public int ChunkHeight;
        }

        private readonly BoxelManagerSettings Settings;
        private readonly IBoxelContainer Boxels;
        public int BoxelCount {
            get { return this.Boxels.Count; }
        }
        public IEnumerable<IBoxel> AllBoxels { get { return this.Boxels.AllBoxels; } } 

        public BoxelManager(BoxelManagerSettings Settings)
        {
            if(Settings.UseChunks)
                throw new NotImplementedException("Chunking not supported.");
            this.Settings = Settings;
            var LargestSide = Math.Max(Math.Max(Settings.Width, Settings.Height), Settings.Length);
            this.Boxels = new ConstantRandomContainer();
            //this.Boxels = new Octree<IBoxel>(LargestSide);
        }

        public void Add(IBoxel Boxel, Int3 Position)
        {
            this.Boxels.Add(Boxel, Position);
        }
    }
}
