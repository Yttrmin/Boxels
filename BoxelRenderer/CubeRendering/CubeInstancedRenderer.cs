using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device1 = SharpDX.Direct3D11.Device1;
using BoxelCommon;

namespace BoxelRenderer
{
    public class CubeInstancedRenderer : BaseRenderer
    {
        private const int BoxelSize = 2;
        private const int VerticesPerBoxel = 24;
        private const int EmittedVertices = 36;

        public CubeInstancedRenderer(RenderDevice Device, BoxelTypes<ICubeBoxelType> Types)
            : base("CRShaders.hlsl", "VShader", null, "PShader", PrimitiveTopology.TriangleList, Device, Types)
        {

        }

        protected override void PreRender(DeviceContext1 Context)
        {

        }

        protected override void GenerateBuffers(IEnumerable<IBoxel> Boxels, Device1 Device, out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            IndexBuffer = null;
            var Enumerable = Boxels as IBoxel[] ?? Boxels.ToArray();
            RendererHelpers.VertexBuffer.NonIndexedCube(out VertexBuffer, out Binding, out VertexCount, Device, BoxelSize);
            this.GenerateInstanceBuffer(out InstanceBuffer, Enumerable, Device);
            InstanceCount = Enumerable.Length;
            InstanceBinding = new VertexBufferBinding(InstanceBuffer, 12, 0);
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            RendererHelpers.InputLayout.PositionInstanced(out Elements, out VertexSizeInBytes);
        }

        private void GenerateInstanceBuffer(out Buffer InstanceBuffer, IEnumerable<IBoxel> Boxels, Device1 Device)
        {
            using (var Stream = new DataStream(Boxels.Count() * Vector3.SizeInBytes, false, true))
            {
                foreach (var Boxel in Boxels)
                {
                    // Write the offset to apply to each vertex.
                    // This is very different from the 8 offsets applied for
                    // the old version.
                    Stream.Write(new Vector3(Boxel.Position.X * BoxelSize,
                        Boxel.Position.Y * BoxelSize, Boxel.Position.Z * BoxelSize));
                }
                InstanceBuffer = new Buffer(Device, Stream, (int)Stream.Length, ResourceUsage.Immutable,
                                            BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                InstanceBuffer.DebugName = "BoxelInstanceBuffer";
            }
        }
    }
}
