using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            [Obsolete]
            public int Length;
            [Obsolete]
            public int Width;
            [Obsolete]
            public int Height;
            [Obsolete]
            public bool UseChunks;
            public int ChunkLength;
            public int ChunkWidth;
            public int ChunkHeight;
        }

        private readonly BoxelManagerSettings Settings;
        private readonly IBoxelContainer Boxels;
        private readonly IBoxelRenderer Renderer;
        private readonly RenderDevice RenderDevice;
        private BoxelTypes<ICubeBoxelType> BoxelTypes;
        [Obsolete("There won't be JUST boxels so this should be somewhere else. Need a more general rendering manager.")]
        private readonly Buffer PerFrameData;
        private const bool Benchmark = false;
        private const int AllBoxelHash = unchecked((int)0xDEADBEEF);
        /// <summary>
        /// Minimum number of boxels to draw from camera in all directions.
        /// </summary>
        private readonly int DrawDistance;
        private bool IsDirty;
        public int BoxelCount {
            get { return this.Boxels.Count; }
        }
        public IEnumerable<IBoxel> AllBoxels { get { return this.Boxels.AllBoxels; } } 

        public BoxelManager(BoxelManagerSettings Settings, RenderDevice RenderDevice, BoxelTypes<ICubeBoxelType> Types)
        {
            if(Settings.UseChunks)
                throw new NotImplementedException("Chunking not supported.");
            this.Settings = Settings;
            var LargestSide = Math.Max(Math.Max(Settings.Width, Settings.Height), Settings.Length);
            this.BoxelTypes = Types;
            this.Boxels = new ConstantRandomContainer();
            foreach (var Arg in Environment.GetCommandLineArgs())
            {
                if (Arg == "point")
                {
                    Trace.WriteLine("Using PointRenderer.");
                    this.Renderer = new PointRenderer(RenderDevice, this.BoxelTypes);
                    break;
                }
                else if (Arg == "cube")
                {
                    Trace.WriteLine("Using CubeRenderer.");
                    this.Renderer = new CubeRenderer(RenderDevice, this.BoxelTypes);
                    break;
                }
                else if (Arg == "cubenogs")
                {
                    Trace.WriteLine("Using CubeNoGSRenderer.");
                    this.Renderer = new CubeNoGSRenderer(RenderDevice, this.BoxelTypes);
                    break;
                }
                else if (Arg == "cubeii")
                {
                    Trace.WriteLine("Using CubeIIR");
                    this.Renderer = new CubeIndexedInstancedRenderer(RenderDevice, this.BoxelTypes);
                }
            }
            if (this.Renderer == null)
            {
                Trace.WriteLine("Using CubeNoGSRenderer by default.");
                this.Renderer = new CubeNoGSRenderer(RenderDevice, this.BoxelTypes);
            }
            this.DrawDistance = 32;
            this.RenderDevice = RenderDevice;
            this.PerFrameData = new Buffer(RenderDevice.D3DDevice, Matrix.SizeInBytes, ResourceUsage.Dynamic,
                                           BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            this.RenderDevice.D3DDevice.ImmediateContext1.VertexShader.SetConstantBuffer(0, this.PerFrameData);
            this.RenderDevice.D3DDevice.ImmediateContext1.GeometryShader.SetConstantBuffer(0, this.PerFrameData);
            this.RenderDevice.D3DDevice.ImmediateContext1.PixelShader.SetConstantBuffer(0, this.PerFrameData);
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
            if (this.Settings.UseChunks)
            {
                var CenterChunk = ChunkPosition.From(RenderCamera.Position);
                var Hash = ChunkPosition.HashSphere(CenterChunk, this.DrawDistance);
                if (this.Renderer.ViewHash != Hash)
                {
                    this.Renderer.SetView(this.Boxels.BoxelsInRadius(CenterChunk, this.DrawDistance),
                        Hash, this.RenderDevice.D3DDevice);
                    Trace.WriteLine("!Forcing Garbage Collection!!REMOVE ME!");
                    GC.Collect(3, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(3, GCCollectionMode.Forced, true);
                    Trace.WriteLine("!Forcing Garbage Collection Complete!!REMOVE ME!");
                }
            }
            else if (!this.Settings.UseChunks && this.Renderer.ViewHash != AllBoxelHash)
            {
                this.Renderer.SetView(this.Boxels.AllBoxels, AllBoxelHash, this.RenderDevice.D3DDevice);
                Trace.WriteLine("!Forcing Garbage Collection!!REMOVE ME!!This should only happen once!");
                GC.Collect(3, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(3, GCCollectionMode.Forced, true);
                Trace.WriteLine("!Forcing Garbage Collection Complete!!REMOVE ME!");
            }
            this.RenderDevice.Profiler.RecordTimeStamp(GPUProfiler.TimeStamp.FrameIdle);
            this.UpdatePerFrameData(RenderCamera);
            this.RenderDevice.Profiler.RecordTimeStamp(GPUProfiler.TimeStamp.PerFrameBufferUpdate);
            this.Renderer.Render(this.RenderDevice.D3DDevice.ImmediateContext1);
            this.RenderDevice.Render();
        }

        private void SetupRenderer(BaseRenderer NewRenderer)
        {
            if (this.Renderer != null)
            {
                throw new NotImplementedException();
            }
            //this.Renderer = NewRenderer;
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
