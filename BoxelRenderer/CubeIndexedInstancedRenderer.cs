using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device1 = SharpDX.Direct3D11.Device1;
using Cube = BoxelRenderer.RendererHelpers.Cube;

namespace BoxelRenderer
{
    public sealed class CubeIndexedInstancedRenderer : BaseRenderer
    {
        private const int BoxelSize = 2;
        private const int VerticesPerBoxel = 24;
        private const int EmittedVertices = 36;

        public CubeIndexedInstancedRenderer(Device1 Device)
            : base("CRShaders.hlsl", "VShader", null, "PShader", PrimitiveTopology.TriangleList, Device)
        {

        }

        protected override void PreRender(DeviceContext1 Context)
        {

        }

        protected override void GenerateBuffers(IEnumerable<IBoxel> Boxels, Device1 Device, out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            VertexCount = Cube.UniqueVertexCount;
            using (var VertexStream = new DataStream(VertexCount * Vector3.SizeInBytes, false, true))
            {
                new Cube(Vector3.Zero, BoxelSize).WriteVertices(VertexStream);
                VertexBuffer = new Buffer(Device, VertexStream, (int)VertexStream.Length, ResourceUsage.Immutable,
                                               BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                VertexBuffer.DebugName = "BoxelsVertexBuffer";
                Binding = new VertexBufferBinding(VertexBuffer, 12, 0);
            }
            this.GenerateIndexBuffer(out IndexBuffer, Device);
            this.GenerateInstanceBuffer(out InstanceBuffer, Boxels, Device);
            InstanceCount = Boxels.Count();
            InstanceBinding = new VertexBufferBinding(InstanceBuffer, 12, 0);
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            Elements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                    new InputElement("POSITION", 1, Format.R32G32B32_Float, 0, 1, InputClassification.PerInstanceData, 1),
                };
            VertexSizeInBytes = Vector3.SizeInBytes * 2;
        }

        private void GenerateIndexBuffer(out Buffer IndexBuffer, Device1 Device)
        {
            const int IndexCount = EmittedVertices;
            const int Size = sizeof(Int32) * IndexCount;
            using (var IndexStream = new DataStream(Size, false, true))
            {
                IndexStream.Write(0);
                IndexStream.Write(1);
                IndexStream.Write(2);

                IndexStream.Write(2);
                IndexStream.Write(1);
                IndexStream.Write(3);

                IndexStream.Write(4);
                IndexStream.Write(5);
                IndexStream.Write(6);

                IndexStream.Write(6);
                IndexStream.Write(5);
                IndexStream.Write(7);

                IndexStream.Write(8);
                IndexStream.Write(9);
                IndexStream.Write(10);

                IndexStream.Write(10);
                IndexStream.Write(9);
                IndexStream.Write(11);

                IndexStream.Write(12);
                IndexStream.Write(13);
                IndexStream.Write(14);

                IndexStream.Write(14);
                IndexStream.Write(13);
                IndexStream.Write(15);

                IndexStream.Write(16);
                IndexStream.Write(17);
                IndexStream.Write(18);

                IndexStream.Write(18);
                IndexStream.Write(17);
                IndexStream.Write(19);

                IndexStream.Write(20);
                IndexStream.Write(21);
                IndexStream.Write(22);

                IndexStream.Write(22);
                IndexStream.Write(21);
                IndexStream.Write(23);

                IndexBuffer = new Buffer(Device, IndexStream, Size, ResourceUsage.Immutable, BindFlags.IndexBuffer,
                                     CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                IndexBuffer.DebugName = "BoxelIndexBuffer";
            }
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
