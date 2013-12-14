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
using VisibleBoxel = BoxelCommon.BoxelHelpers.VisibleBoxel;
using Side = BoxelCommon.BoxelHelpers.Side;
using Vertex = BoxelRenderer.Vertex;
using System.Diagnostics;

namespace BoxelRenderer
{
    public sealed class TiledPlaneRenderer : BaseRenderer
    {
        private Buffer BoxelTextureLookup;
        private const int BoxelSize = 2;
        private IBoxelMesher Mesher;

        public TiledPlaneRenderer(RenderDevice Device, BoxelTypes<ICubeBoxelType> Types)
            : base("TiledPlaneShaders.hlsl", "VShaderTextured", null, "PShaderTextured", 
                PrimitiveTopology.TriangleList, Device, Types)
        {
            this.Mesher = new PlaneMesherLowPoly(BoxelSize);
        }

        protected override void GenerateBuffers(IEnumerable<IBoxel> Boxels, SharpDX.Direct3D11.Device1 Device, 
            out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            IndexBuffer = InstanceBuffer = null;
            InstanceBinding = new VertexBufferBinding();
            InstanceCount = 0;

            VertexCount = 0;

            var BoxelArray = Boxels.ToArray();
            using(var Buffer = new DataBuffer(BoxelArray.Length * VertexSizeInBytes * 24))
            {
                IntPtr CurrentPosition = Buffer.DataPointer;
                int FinalSize = 0;
                foreach(var Rect in this.Mesher.CreateRectangleOutline(BoxelArray))
                {
                    CurrentPosition = Utilities.Write<Vertex>(CurrentPosition, Rect.ToVertices(), 0, 6);
                    FinalSize += Vertex.SizeInBytes * 6;
                    VertexCount += 6;
                }

                VertexBuffer = new Buffer(Device, Buffer.DataPointer, new BufferDescription()
                    {
                        BindFlags = BindFlags.VertexBuffer,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None,
                        SizeInBytes = FinalSize,
                        StructureByteStride = 0,
                        Usage = ResourceUsage.Immutable
                    });
                VertexBuffer.DebugName = "RectangularBoxelVertices";
                Binding = new VertexBufferBinding(VertexBuffer, VertexSizeInBytes, 0);
            }
            System.Diagnostics.Trace.WriteLine(String.Format("TiledPlaneRenderer buffers created. {0} total vertices.",
                VertexCount));
        }

        protected override void PreRender(DeviceContext1 Context)
        {
            
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            //@TODO - Need a normal for each vertex. The Texture3D can hold a vector but I can't think of how that
            // could possibly tell us what texture for what side to sample. Instead just have it hold an int for
            // rock/grass/etc texture and calculate the side to pick from that array based on the surface normal.
            // Or possibly just include a simple int to save on math in the shader?
            Elements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                    new InputElement("TEXCOORD", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0),
                };
            VertexSizeInBytes = Vector3.SizeInBytes * 2;
        }
    }
}
