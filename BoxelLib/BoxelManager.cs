using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelCommon;
using BoxelRenderer;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

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
        private readonly Buffer PerFrameData;
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
            this.Renderer = new PointRenderer(RenderDevice.D3DDevice);
            this.DrawDistance = 32;
            this.RenderDevice = RenderDevice;
            this.PerFrameData = new Buffer(RenderDevice.D3DDevice, Matrix.SizeInBytes, ResourceUsage.Dynamic,
                                           BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            this.RenderDevice.D3DDevice.ImmediateContext1.VertexShader.SetConstantBuffer(0, this.PerFrameData);
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
            this.UpdatePerFrameData(RenderCamera);
            this.Renderer.Render(this.RenderDevice.D3DDevice.ImmediateContext1);
            this.RenderDevice.Render();
        }

        private void UpdatePerFrameData(ICamera RenderCamera)
        {
            DataStream PerFramePointer;
            this.RenderDevice.D3DDevice.ImmediateContext1.MapSubresource(this.PerFrameData, 0, MapMode.WriteDiscard,
                                                                            MapFlags.None, out PerFramePointer);
            var World = Matrix.Translation(new Vector3(0,0,0));
            var WorldViewProj = World * RenderCamera.View * RenderCamera.Projection;
            PerFramePointer.Write(Matrix.Transpose(WorldViewProj));
            this.RenderDevice.D3DDevice.ImmediateContext1.UnmapSubresource(this.PerFrameData, 0);
        }
    }
}
