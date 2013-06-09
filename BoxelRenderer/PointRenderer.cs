using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device1 = SharpDX.Direct3D11.Device1;

namespace BoxelRenderer
{
    public sealed class PointRenderer : IBoxelRenderer
    {
        private Buffer VertexBuffer;
        private VertexBufferBinding VertexBufferView;
        private int VertexSizeInBytes;
        private InputLayout Layout;
        private VertexShader VertexShader;
        private PixelShader PixelShader;
        private int BoxelCount;
        public int ViewHash { get; private set; }

        public PointRenderer(Device1 Device)
        {
            this.CompileShaders(Device);
        }

        public void SetView(IEnumerable<IBoxel> Boxels, int SphereHash, Device1 Device)
        {
            Debug.Assert(SphereHash != this.ViewHash);
            this.GenerateVertexBuffer(Boxels, Device);
            this.BoxelCount = Boxels.Count();
            this.ViewHash = SphereHash;
        }

        public void Render(DeviceContext1 Context)
        {
            Context.InputAssembler.InputLayout = this.Layout;
            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            Context.InputAssembler.SetVertexBuffers(0, this.VertexBufferView);
            Context.VertexShader.Set(this.VertexShader);
            Context.PixelShader.Set(this.PixelShader);
            Context.Draw(this.BoxelCount, 0);
        }

        private void GenerateVertexBuffer(IEnumerable<IBoxel> Boxels, Device1 Device)
        {
            using (var VertexStream = new DataStream(Boxels.Count() * this.VertexSizeInBytes, false, true))
            {
                foreach (var Boxel in Boxels)
                {
                    VertexStream.Write((Vector3)Boxel.Position);
                }
                this.VertexBuffer = new Buffer(Device, VertexStream, (int)VertexStream.Length, ResourceUsage.Immutable,
                                               BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                this.VertexBufferView = new VertexBufferBinding(this.VertexBuffer, 12, 0);
            }
        }

        private void InitializeLayout(Device1 Device, ShaderSignature VertexShaderSignature)
        {
            var Elements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                };
            this.VertexSizeInBytes = sizeof (float)*3;
            this.Layout = new InputLayout(Device, VertexShaderSignature, Elements);
        }

        private void CompileShaders(Device1 Device)
        {
            Trace.WriteLine("Compiling shaders...");
            Trace.WriteLine("*TODO:* Don't compile at runtime!");
            ShaderSignature VertexShaderSignature;
 
            using (
                var Result = ShaderBytecode.CompileFromFile("PRShaders.hlsl", "VShader", "vs_4_0",
                                                              ShaderFlags.WarningsAreErrors))
            {
                if (Result.HasErrors)
                {
                    throw new InvalidOperationException(
                        String.Format("Vertex shader compilation failed (result: {1})! Output:\n{0}\n",
                                      Result.Message, Result.ResultCode));
                }
                VertexShaderSignature = new ShaderSignature(Result.Bytecode);
                this.VertexShader = new VertexShader(Device, Result.Bytecode);
            }

            using (
                var Result = ShaderBytecode.CompileFromFile("PRShaders.hlsl", "PShader", "ps_4_0",
                                                              ShaderFlags.WarningsAreErrors))
            {
                if (Result.HasErrors)
                {
                    throw new InvalidOperationException(
                        String.Format("Pixel shader compilation failed (result: {1})! Output:\n{0}\n",
                                      Result.Message, Result.ResultCode));
                }
                this.PixelShader = new PixelShader(Device, Result.Bytecode);
            }
            Trace.WriteLine("Done. Setting up InputLayout...");
            this.InitializeLayout(Device, VertexShaderSignature);
            Trace.WriteLine("Done.");
        }
    }
}
