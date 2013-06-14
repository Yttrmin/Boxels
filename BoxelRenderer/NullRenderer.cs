using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace BoxelRenderer
{
    public class NullRenderer : BaseRenderer
    {
        public NullRenderer(Device1 Device)
            : base("PRShaders.hlsl", "VShader", null, "PShader", SharpDX.Direct3D.PrimitiveTopology.PointList, Device)
        {

        }

        protected override void PreRender(SharpDX.Direct3D11.DeviceContext1 Context)
        {
            
        }

        protected override void GenerateBuffers(IEnumerable<BoxelLib.IBoxel> Boxels, SharpDX.Direct3D11.Device1 Device, 
            out SharpDX.Direct3D11.Buffer VertexBuffer, out SharpDX.Direct3D11.VertexBufferBinding Binding, out int VertexCount, 
            out SharpDX.Direct3D11.Buffer IndexBuffer, out SharpDX.Direct3D11.Buffer InstanceBuffer, 
            out SharpDX.Direct3D11.VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            IndexBuffer = null;
            InstanceCount = 0;
            InstanceBuffer = null;
            InstanceBinding = new SharpDX.Direct3D11.VertexBufferBinding();
            VertexCount = 0;
            VertexSizeInBytes = 0;
            VertexBuffer = null;
            Binding = new VertexBufferBinding();
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            RendererHelpers.InputLayout.Position(out Elements, out VertexSizeInBytes);
        }
    }
}
