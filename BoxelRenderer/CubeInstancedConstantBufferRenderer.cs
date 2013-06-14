using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace BoxelRenderer
{
    public class CubeInstancedConstantBufferRenderer : BaseRenderer
    {
        private Buffer InstancePositionsBuffer;
        private const int MaxBoxels = 4096;
        private const int BoxelSize = 2;

        public CubeInstancedConstantBufferRenderer(Device1 Device)
            : base("CRShaders.hlsl", "VShaderCBuffer", null, "PShader", PrimitiveTopology.TriangleList, Device)
        {
            
        }

        protected override void PreRender(DeviceContext1 Context)
        {
            Context.VertexShader.SetConstantBuffer(1, this.InstancePositionsBuffer);
        }

        protected override void GenerateBuffers(IEnumerable<BoxelLib.IBoxel> Boxels, Device1 Device, out SharpDX.Direct3D11.Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out SharpDX.Direct3D11.Buffer IndexBuffer, 
            out SharpDX.Direct3D11.Buffer InstanceBuffer, out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            IndexBuffer = null;
            InstanceBuffer = null;
            InstanceBinding = new VertexBufferBinding();
            var Enumerable = Boxels as IBoxel[] ?? Boxels.ToArray();
            RendererHelpers.VertexBuffer.NonIndexedCube(out VertexBuffer, out Binding, out VertexCount, Device, BoxelSize);
            this.GenerateInstanceBuffer(Enumerable, Device);
            InstanceCount = MaxBoxels;
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            RendererHelpers.InputLayout.Position(out Elements, out VertexSizeInBytes);
        }

        private void GenerateInstanceBuffer(IBoxel[] Boxels, Device1 Device)
        {
            //if(Boxels.Count() > MaxBoxels)
            //    throw new InvalidOperationException(String.Format("Too many boxels. {0} > {1}", Boxels.Count(), MaxBoxels));
            using (var Stream = new DataStream(MaxBoxels*Vector4.SizeInBytes, false, true))
            {
                for (var i = 0; i < Math.Min(Boxels.Count(), MaxBoxels); i++ )
                {
                    Stream.Write(new Vector4(Boxels[i].Position.X * BoxelSize,
                        Boxels[i].Position.Y * BoxelSize, Boxels[i].Position.Z * BoxelSize, 0));
                }
                this.InstancePositionsBuffer = new Buffer(Device, Stream, (int)Stream.Length, ResourceUsage.Immutable,
                                                          BindFlags.ConstantBuffer, CpuAccessFlags.None,
                                                          ResourceOptionFlags.None, 0);
            }
        }
    }
}
