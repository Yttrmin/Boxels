using System.Collections.Generic;
using System.Linq;
using BoxelLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device1 = SharpDX.Direct3D11.Device1;

namespace BoxelRenderer
{
    public sealed class PointRenderer : BaseRenderer
    {
        public PointRenderer(Device1 Device) 
            : base("PRShaders.hlsl", "VShader", null, "PShader", PrimitiveTopology.PointList, Device)
        {

        }

        protected override void GenerateBuffers(IEnumerable<IBoxel> Boxels, Device1 Device, out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            InstanceBuffer = null;
            InstanceBinding = new VertexBufferBinding();
            IndexBuffer = null;
            InstanceCount = 0;
            VertexCount = Boxels.Count();
            using (var VertexStream = new DataStream(Boxels.Count() * VertexSizeInBytes, false, true))
            {
                foreach (var Boxel in Boxels)
                {
                    VertexStream.Write((Vector3)Boxel.Position);
                }
                VertexBuffer = new Buffer(Device, VertexStream, (int)VertexStream.Length, ResourceUsage.Immutable,
                                               BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                Binding = new VertexBufferBinding(VertexBuffer, 12, 0);
            }
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            Elements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                };
            VertexSizeInBytes = sizeof(float) * 3;
        }

        protected override void PreRender(DeviceContext1 Context)
        {
        }
    }
}
