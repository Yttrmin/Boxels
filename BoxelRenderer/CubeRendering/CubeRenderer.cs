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
    public sealed class CubeRenderer : BaseRenderer
    {
        private const int BoxelSize = 16;
        private readonly Buffer CubeConstantData;
        private Texture2D BoxelTexture;
        private ShaderResourceView BoxelTextureView;

        public CubeRenderer(RenderDevice Device, BoxelTypes<ICubeBoxelType> Types)
            : base("BRShaders.hlsl", "VShader", "GShader", "PShader", PrimitiveTopology.PointList, Device, Types)
        {
            this.CubeConstantData = this.InitializeCubeData(Device.D3DDevice);
        }

        protected override void GenerateBuffers(IEnumerable<IBoxel> Boxels, Device1 Device, out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            IndexBuffer = null;
            InstanceBuffer = null;
            InstanceCount = 0;
            InstanceBinding = new VertexBufferBinding();
            VertexCount = Boxels.Count();
            using (var VertexStream = new DataStream(Boxels.Count() * VertexSizeInBytes, false, true))
            {
                foreach (var Boxel in Boxels)
                {
                    VertexStream.Write(new Vector3(Boxel.Position.X * BoxelSize, 
                        Boxel.Position.Y * BoxelSize, Boxel.Position.Z * BoxelSize));
                }
                VertexBuffer = new Buffer(Device, VertexStream, (int)VertexStream.Length, ResourceUsage.Immutable,
                                               BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                VertexBuffer.DebugName = "BoxelsVertexBuffer";
                Binding = new VertexBufferBinding(VertexBuffer, 12, 0);
            }
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            Elements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                };
            VertexSizeInBytes = Vector3.SizeInBytes;
        }

        protected override void PreRender(DeviceContext1 Context)
        {
            Context.GeometryShader.SetConstantBuffer(1, this.CubeConstantData);
        }

        private Buffer InitializeCubeData(Device1 Device)
        {
            Buffer CubeBuffer;
            const int CubeOffset = BoxelSize/2;
            using (var CubeConstantsStream = new DataStream(Vector4.SizeInBytes * 14 + Vector4.SizeInBytes * 4, true, true))
            {
                // Offset from center vertex for cube vertices.
                CubeConstantsStream.Write(new Vector4(-CubeOffset, -CubeOffset, CubeOffset, CubeOffset));
                CubeConstantsStream.Write(new Vector4(CubeOffset, -CubeOffset, CubeOffset, CubeOffset));
                CubeConstantsStream.Write(new Vector4(CubeOffset, CubeOffset, CubeOffset, CubeOffset));
                CubeConstantsStream.Write(new Vector4(-CubeOffset, -CubeOffset, -CubeOffset, CubeOffset));
                CubeConstantsStream.Write(new Vector4(-CubeOffset, CubeOffset, -CubeOffset, CubeOffset));
                CubeConstantsStream.Write(new Vector4(CubeOffset, -CubeOffset, -CubeOffset, CubeOffset));
                CubeConstantsStream.Write(new Vector4(CubeOffset, CubeOffset, -CubeOffset, CubeOffset));
                CubeConstantsStream.Write(new Vector4(-CubeOffset, CubeOffset, CubeOffset, CubeOffset));

                // Normals for each cube vertex.
                CubeConstantsStream.Write(new Vector4(0, 0, 1, 1));
                CubeConstantsStream.Write(new Vector4(0, 0, -1, 1));
                CubeConstantsStream.Write(new Vector4(0, 1, 0, 1));
                CubeConstantsStream.Write(new Vector4(0, -1, 0, 1));
                CubeConstantsStream.Write(new Vector4(1, 0, 0, 1));
                CubeConstantsStream.Write(new Vector4(-1, 0, 0, 0));
                
                // UV coordinates for each cube vertex.
                CubeConstantsStream.Write(new Vector4(0, 0, 0, 0));
                CubeConstantsStream.Write(new Vector4(0, 1, 0, 0));
                CubeConstantsStream.Write(new Vector4(1, 0, 0, 0));
                CubeConstantsStream.Write(new Vector4(1, 1, 0, 0));

                CubeBuffer = new Buffer(Device, CubeConstantsStream, Vector4.SizeInBytes * (14) + Vector4.SizeInBytes * 4
                , ResourceUsage.Immutable, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                CubeBuffer.DebugName = "CubeDataConstantBuffer";
            }
            return CubeBuffer;
        }
    }
}
