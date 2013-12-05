using BoxelCommon;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace BoxelRenderer
{
    public sealed class TiledPlaneRenderer : BaseRenderer
    {
        private Buffer BoxelTextureLookup;

        public TiledPlaneRenderer(RenderDevice Device, BoxelTypes<ICubeBoxelType> Types)
            : base("TiledPlaneShaders.hlsl", "VShaderTextured", null, "PShaderTextured", 
                PrimitiveTopology.TriangleList, Device, Types)
        {
            throw new NotImplementedException();
        }

        protected override void GenerateBuffers(IEnumerable<IBoxel> Boxels, SharpDX.Direct3D11.Device1 Device, out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            throw new NotImplementedException();
        }

        private void CreatePlanarOutline(IEnumerable<IBoxel> Boxels, ref IntPtr BufferPointer)
        {
            var BoxelPlaneMap = new Dictionary<int, Plane>();
            foreach(var Boxel in BoxelHelpers.SideOcclusionCull(Boxels))
            {
                
            }
        }

        protected override void PreRender(DeviceContext1 Context)
        {
            throw new NotImplementedException();
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            throw new NotImplementedException();
            Elements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                    new InputElement("TEXCOORD", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0),
                };
            VertexSizeInBytes = Vector3.SizeInBytes * 2;
        }

        
    }
}
