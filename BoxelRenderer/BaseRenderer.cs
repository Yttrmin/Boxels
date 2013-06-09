using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace BoxelRenderer
{
    internal abstract class BaseRenderer : IBoxelRenderer
    {
        public int ViewHash { get; private set; }
        private InputLayout Layout;
        private VertexShader VertexShader;
        private GeometryShader GeometryShader;
        private PixelShader PixelShader;
        private Buffer VertexBuffer;
        private VertexBufferBinding VertexBufferBinding;
        private PrimitiveTopology Topology;
        private int BoxelCount;

        protected BaseRenderer(string ShaderFileName, string VertexEntryName, string GeometryEntryName,
                                    string PixelEntryName, PrimitiveTopology Topology, Device1 Device)
        {
            this.Topology = Topology;
            this.CompileShaders(Device, ShaderFileName, VertexEntryName, GeometryEntryName, PixelEntryName);
        }

        public void SetView(IEnumerable<IBoxel> Boxels, int SphereHash, Device1 Device)
        {
            Debug.Assert(SphereHash != this.ViewHash);
            this.GenerateVertexBuffer(Boxels, Device, out VertexBuffer, out VertexBufferBinding);
            this.BoxelCount = Boxels.Count();
            this.ViewHash = SphereHash;
        }

        public void Render(DeviceContext1 Context)
        {
            Context.InputAssembler.InputLayout = this.Layout;
            Context.InputAssembler.PrimitiveTopology = this.Topology;
            Context.InputAssembler.SetVertexBuffers(0, this.VertexBufferBinding);
            Context.VertexShader.Set(this.VertexShader);
            Context.GeometryShader.Set(this.GeometryShader);
            Context.PixelShader.Set(this.PixelShader);
            Context.Draw(this.BoxelCount, 0);
        }

        protected abstract void GenerateVertexBuffer(IEnumerable<IBoxel> Boxels, Device1 Device,
            out Buffer VertexBuffer, out VertexBufferBinding Binding);

        protected abstract void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes);

        private void CompileShaders(Device1 Device, string ShaderFileName, string VertexEntryName, string GeometryEntryName,
                                    string PixelEntryName)
        {
            Trace.WriteLine("Compiling shaders...");
            Trace.WriteLine("*TODO:* Don't compile at runtime!");
            ShaderSignature VertexShaderSignature;

            using (
                var Result = ShaderBytecode.CompileFromFile(ShaderFileName, VertexEntryName, "vs_4_0",
                                                              ShaderFlags.WarningsAreErrors))
            {
                VertexShaderSignature = new ShaderSignature(Result.Bytecode);
                this.VertexShader = new VertexShader(Device, Result.Bytecode);
            }

            if (!string.IsNullOrEmpty(GeometryEntryName))
            {
                using (
                    var Result = ShaderBytecode.CompileFromFile(ShaderFileName, GeometryEntryName, "gs_4_0",
                                                                ShaderFlags.WarningsAreErrors))
                {
                    this.GeometryShader = new GeometryShader(Device, Result.Bytecode);
                }
            }

            using (
                var Result = ShaderBytecode.CompileFromFile(ShaderFileName, PixelEntryName, "ps_4_0",
                                                              ShaderFlags.WarningsAreErrors))
            {
                this.PixelShader = new PixelShader(Device, Result.Bytecode);
            }
            Trace.WriteLine("Done. Setting up InputLayout...");
            this.InitializeLayout(Device, VertexShaderSignature);
            Trace.WriteLine("Done.");
        }

        private void InitializeLayout(Device1 Device, ShaderSignature VertexShaderSignature)
        {
            InputElement[] Elements;
            int VertexSizeInBytes;
            this.SetupInputElements(out Elements, out VertexSizeInBytes);
            this.Layout = new InputLayout(Device, VertexShaderSignature, Elements);
        }
    }
}
