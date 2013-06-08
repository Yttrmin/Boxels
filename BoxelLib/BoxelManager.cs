using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelRenderer;
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
        private readonly IBoxelRenderer Renderer;
        private readonly RenderDevice RenderDevice;
        /// <summary>
        /// Minimum number of boxels to draw from camera in all directions.
        /// </summary>
        private readonly int DrawDistance;
        private bool IsDirty;
        public int BoxelCount {
            get { return this.Boxels.Count; }
        }
        public IEnumerable<IBoxel> AllBoxels { get { return this.Boxels.AllBoxels; } } 

        public BoxelManager(BoxelManagerSettings Settings, RenderDevice RenderDevice)
        {
            if(Settings.UseChunks)
                throw new NotImplementedException("Chunking not supported.");
            this.Settings = Settings;
            var LargestSide = Math.Max(Math.Max(Settings.Width, Settings.Height), Settings.Length);
            this.Boxels = new ConstantRandomContainer();
            this.Renderer = new BoxelPointRenderer(RenderDevice.D3DDevice);
            this.DrawDistance = 32;
            this.RenderDevice = RenderDevice;
        }

        public void Add(IBoxel Boxel, Int3 Position)
        {
            this.Boxels.Add(Boxel, Position);
            this.IsDirty = true;
        }

        public void Lock()
        {
            throw new NotImplementedException();
        }

        public void Render(ICamera RenderCamera)
        {
            var CenterChunk = ChunkPosition.From(RenderCamera.Position);
            var Hash = ChunkPosition.HashSphere(CenterChunk, this.DrawDistance);
            if (this.Renderer.ViewHash != Hash)
            {
                this.Renderer.SetView(this.Boxels.BoxelsInRadius(CenterChunk, this.DrawDistance), 
                    Hash, this.RenderDevice.D3DDevice);
            }
            this.Renderer.Render(this.RenderDevice.D3DDevice.ImmediateContext1);
            this.RenderDevice.Render();
        }
    }
}
