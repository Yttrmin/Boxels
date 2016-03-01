using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device1 = SharpDX.Direct3D11.Device1;
using BoxelCommon;
using System;
using SharpDX;
using SharpDX.WIC;

namespace BoxelRenderer
{
    public abstract class BaseRenderer : IBoxelRenderer, IDisposable
    {
        public int ViewHash { get; private set; }
        private InputLayout Layout;
        private VertexShader VertexShader;
        private GeometryShader GeometryShader;
        private PixelShader PixelShader;
        private Buffer VertexBuffer;
        private Buffer IndexBuffer;
        private Buffer InstanceBuffer;
        private VertexBufferBinding VertexBufferBinding, InstanceBufferBinding;
        private PrimitiveTopology Topology;
        private int VertexSizeInBytes;
        private int VertexCount, InstanceCount;
        private ShaderResourceView Texture;
        private int TextureCount;
        private SamplerState TextureSampler;
        private readonly ImagingFactory2 ImagingFactory;
        private readonly IDictionary<Size2, TextureManager> TextureManagers;
        private readonly IDictionary<string, TextureManager> TextureNameToManager;
        private BoxelTypes<ICubeBoxelType> BoxelTypes;
        private GPUProfiler Profiler;

        protected BaseRenderer(string ShaderFileName, string VertexEntryName, string GeometryEntryName,
                                    string PixelEntryName, PrimitiveTopology Topology, RenderDevice Device, BoxelTypes<ICubeBoxelType> BoxelTypes)
        {
            this.BoxelTypes = BoxelTypes;
            this.Topology = Topology;
            this.CompileShaders(Device.D3DDevice, ShaderFileName, VertexEntryName, GeometryEntryName, PixelEntryName);
            this.TextureSampler = new SamplerState(Device.D3DDevice, new SamplerStateDescription()
            {
                Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = SharpDX.Color.HotPink,
                MinimumLod = 0,
                MaximumLod = float.MaxValue,
                ComparisonFunction = Comparison.Never,
                MaximumAnisotropy = 16,
                MipLodBias = 0
            });
            this.ImagingFactory = new ImagingFactory2();
            this.TextureManagers = new Dictionary<Size2, TextureManager>();
            this.TextureNameToManager = new Dictionary<string, TextureManager>();
            this.Profiler = Device.Profiler;
            this.ConstructTextures(Device.D3DDevice);
        }
        
        public void SetView(IEnumerable<IBoxel> Boxels, int SphereHash, Device1 Device)
        {
            Debug.Assert(SphereHash != this.ViewHash);
            this.GenerateBuffers(Boxels, Device, out this.VertexBuffer, out this.VertexBufferBinding, 
                out this.VertexCount, out this.IndexBuffer, out this.InstanceBuffer,
                out this.InstanceBufferBinding, out this.InstanceCount, this.VertexSizeInBytes);
            this.ViewHash = SphereHash;
        }

        public void Render(DeviceContext1 Context)
        {
            Context.InputAssembler.InputLayout = this.Layout;
            Context.InputAssembler.PrimitiveTopology = this.Topology;
            Context.InputAssembler.SetVertexBuffers(0, this.VertexBufferBinding);
            if(this.InstanceBuffer != null)
                Context.InputAssembler.SetVertexBuffers(1, this.InstanceBufferBinding);
            Context.InputAssembler.SetIndexBuffer(this.IndexBuffer, Format.R32_UInt, 0);
            Context.VertexShader.Set(this.VertexShader);
            Context.GeometryShader.Set(this.GeometryShader);
            Context.PixelShader.Set(this.PixelShader);
            Context.PixelShader.SetShaderResource(0, this.Texture);
            Context.PixelShader.SetSampler(0, this.TextureSampler);
            this.PreRender(Context);
            if(this.InstanceBuffer != null && this.IndexBuffer != null)
                Context.DrawIndexedInstanced(36, this.InstanceCount, 0, 0, 0);
            else if(this.InstanceBuffer != null || this.InstanceCount > 0)
                Context.DrawInstanced(this.VertexCount, this.InstanceCount, 0, 0);
            else if(this.VertexBuffer != null)
                Context.Draw(this.VertexCount, 0);
            this.Profiler.RecordTimeStamp(GPUProfiler.TimeStamp.DrawTerrain);
        }

        public float GetTextureIndexByType(Axis Side, int BoxelType)
        {
            var Type = this.BoxelTypes.GetTypeFromInt(BoxelType);
            var Manager = this.TextureNameToManager[Type.TextureOnSide(Side)];
            return Manager.GetTextureIndexInArray(Manager[Type.TextureOnSide(Side)]);
        }

        /// <summary>
        /// Allows child classes to do their own rendering work before the Draw call.
        /// The child does not have to worry about (and should not mess with):
        /// InputLayout, PrimitiveTopology, index 0 VertexBuffer (from Generate), VertexShader,
        /// GeometryShader, PixelShader, or making the Draw() call for boxels. Similarly the child
        /// should not touch the index 0 constant buffer for VertexShader.
        /// </summary>
        /// <param name="Context"></param>
        protected abstract void PreRender(DeviceContext1 Context);
        
        protected abstract void GenerateBuffers(IEnumerable<IBoxel> Boxels, Device1 Device, out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes);

        protected abstract void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes);

        private void ConstructTextures(Device1 Device)
        {
            foreach (var TextureName in this.BoxelTypes.GetTextureNames())
            {
                if (this.TextureNameToManager.ContainsKey(TextureName))
                    continue;
                using(var Source = TextureManager.LoadBitmap(this.ImagingFactory, TextureName))
                {
                    TextureManager Manager;
                    this.TextureManagers.TryGetValue(Source.Size, out Manager);
                    if (Manager == null)
                        this.TextureManagers[Source.Size] = Manager = new TextureManager(Device, this.ImagingFactory, Source.Size);
                    Manager.Add(TextureName, Source);
                    this.TextureNameToManager[TextureName] = Manager;
                }
            }
            if (this.TextureManagers.Count > 1)
                throw new NotImplementedException("More than 1 TextureManager not allowed.");
            this.Texture = this.TextureManagers.ElementAt(0).Value.GenerateTextureArrayView(out TextureCount);
        }

        private void CompileShaders(Device1 Device, string ShaderFileName, string VertexEntryName, string GeometryEntryName,
                                    string PixelEntryName)
        {
            Trace.WriteLine("Compiling shaders...");
            Trace.WriteLine("*TODO:* Don't compile at runtime!");
            ShaderSignature VertexShaderSignature;

            using (
                var Result = ShaderBytecode.CompileFromFile(ShaderFileName, VertexEntryName, "vs_4_0",
                                                              ShaderFlags.WarningsAreErrors | ShaderFlags.OptimizationLevel3))
            {
                VertexShaderSignature = new ShaderSignature(Result.Bytecode);
                this.VertexShader = new VertexShader(Device, Result.Bytecode);
                this.VertexShader.DebugName = ShaderFileName + "::" + VertexEntryName;
            }

            if (!string.IsNullOrEmpty(GeometryEntryName))
            {
                using (
                    var Result = ShaderBytecode.CompileFromFile(ShaderFileName, GeometryEntryName, "gs_4_0",
                                                                ShaderFlags.WarningsAreErrors | ShaderFlags.OptimizationLevel3))
                {
                    this.GeometryShader = new GeometryShader(Device, Result.Bytecode);
                    this.GeometryShader.DebugName = ShaderFileName + "::" + GeometryEntryName;
                }
            }

            using (
                var Result = ShaderBytecode.CompileFromFile(ShaderFileName, PixelEntryName, "ps_4_0",
                                                              ShaderFlags.WarningsAreErrors | ShaderFlags.OptimizationLevel3))
            {
                this.PixelShader = new PixelShader(Device, Result.Bytecode);
                this.PixelShader.DebugName = ShaderFileName + "::" + PixelEntryName;
            }
            Trace.WriteLine("Done. Setting up InputLayout...");
            this.InitializeLayout(Device, VertexShaderSignature);
            Trace.WriteLine("Done.");
        }

        private void InitializeLayout(Device1 Device, ShaderSignature VertexShaderSignature)
        {
            InputElement[] Elements;
            this.SetupInputElements(out Elements, out this.VertexSizeInBytes);
            this.Layout = new InputLayout(Device, VertexShaderSignature, Elements);
        }

        private void Dispose(bool Disposing)
        {
            this.Layout.Dispose();
            this.Texture.Dispose();
            this.VertexShader.Dispose();
            if(this.GeometryShader != null)
                this.GeometryShader.Dispose();
            this.PixelShader.Dispose();
            this.VertexBuffer.Dispose();
            if(this.IndexBuffer != null)
                this.IndexBuffer.Dispose();
            if(this.InstanceBuffer != null)
                this.InstanceBuffer.Dispose();
            if (Disposing)
            {
                foreach(var Manager in this.TextureManagers.Values)
                {
                    Manager.Dispose();
                }
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        ~BaseRenderer()
        {
            this.Dispose(false);
        }
    }
}
