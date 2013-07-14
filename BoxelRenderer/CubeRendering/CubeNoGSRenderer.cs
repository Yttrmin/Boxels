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
using Device1 = SharpDX.Direct3D11.Device1;
using SharpDX.DXGI;
using BoxelCommon;

namespace BoxelRenderer
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CubeNoGSRenderer : BaseRenderer
    {
        private const int BoxelSize = 2;

        public CubeNoGSRenderer(RenderDevice Device, BoxelTypes<ICubeBoxelType> Types)
            : base("CubeShaders.hlsl", "VShaderTextured", null, "PShaderTextured", PrimitiveTopology.TriangleList, Device, Types)
        {
            
        }

        protected override void PreRender(DeviceContext1 Context)
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
            var Enumerable = this.GetBoxelArray(Boxels);
            var CullResult = BoxelHelpers.OcclusionCull(Enumerable);
            System.Diagnostics.Trace.WriteLine(String.Format("{0} {1}", Enumerable.Length, CullResult.Count()));
            var Random = new Random();
            using (var Buffer = new DataBuffer((Enumerable.Length * SmartCube.MaxDrawnVertexCount) * VertexSizeInBytes))
            {
                IntPtr CurrentPosition = Buffer.DataPointer;
                int FinalSize = 0;
                foreach (var Boxel in BoxelHelpers.SideOcclusionCull(Enumerable))
                {
                    FinalSize += new SmartCube(new Vector3(Boxel.Item1.Position.X * BoxelSize, Boxel.Item1.Position.Y * BoxelSize, 
                        Boxel.Item1.Position.Z * BoxelSize),
                        BoxelSize, Boxel.Item2, this, Boxel.Item1.Type).Write(ref CurrentPosition);
                }
                VertexCount = FinalSize / Vertex.SizeInBytes;
                System.Diagnostics.Trace.WriteLine(String.Format("Final vertex count: {0}", FinalSize / Vertex.SizeInBytes));
                VertexBuffer = new Buffer(Device, Buffer.DataPointer, new BufferDescription()
                {
                    BindFlags=BindFlags.VertexBuffer,
                    CpuAccessFlags=CpuAccessFlags.None,
                    OptionFlags=ResourceOptionFlags.None,
                    SizeInBytes=FinalSize,
                    StructureByteStride=0,
                    Usage=ResourceUsage.Immutable
                });
                VertexBuffer.DebugName = "BoxelsVertexBuffer";
                Binding = new VertexBufferBinding(VertexBuffer, VertexSizeInBytes, 0);
            }
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            Elements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                    new InputElement("TEXCOORD", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0),
                };
            VertexSizeInBytes = Vector3.SizeInBytes * 2;
        }

        //@TODO - Remove me. Just for timing.
        [Timer]
        private IBoxel[] GetBoxelArray(IEnumerable<IBoxel> Boxels)
        {
            return Boxels as IBoxel[] ?? Boxels.ToArray();
        }
    }
}
