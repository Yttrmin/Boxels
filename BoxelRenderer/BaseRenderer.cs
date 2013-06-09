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
    public abstract class BaseRenderer : IBoxelRenderer
    {
        public int ViewHash { get; private set; }
        private InputLayout Layout;
        private VertexShader VertexShader;
        private GeometryShader GeometryShader;
        private PixelShader PixelShader;
        private Buffer VertexBuffer;
        private VertexBufferBinding VertexBufferBinding;
        private PrimitiveTopology Topology;
        private int VertexSizeInBytes;
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
            this.GenerateVertexBuffer(Boxels, Device, out VertexBuffer, out VertexBufferBinding, this.VertexSizeInBytes);
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
            this.PreRender(Context);
            Context.Draw(this.BoxelCount, 0);
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

        protected abstract void GenerateVertexBuffer(IEnumerable<IBoxel> Boxels, Device1 Device,
            out Buffer VertexBuffer, out VertexBufferBinding Binding, int VertexSizeInBytes);

        protected abstract void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes);

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
            }

            if (!string.IsNullOrEmpty(GeometryEntryName))
            {
                using (
                    var Result = ShaderBytecode.CompileFromFile(ShaderFileName, GeometryEntryName, "gs_4_0",
                                                                ShaderFlags.WarningsAreErrors | ShaderFlags.OptimizationLevel3))
                {
                    this.GeometryShader = new GeometryShader(Device, Result.Bytecode);
                }
            }

            using (
                var Result = ShaderBytecode.CompileFromFile(ShaderFileName, PixelEntryName, "ps_4_0",
                                                              ShaderFlags.WarningsAreErrors | ShaderFlags.OptimizationLevel3))
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
            this.SetupInputElements(out Elements, out this.VertexSizeInBytes);
            this.Layout = new InputLayout(Device, VertexShaderSignature, Elements);
        }
    }
}
