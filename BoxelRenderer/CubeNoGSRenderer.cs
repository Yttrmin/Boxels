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

namespace BoxelRenderer
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CubeNoGSRenderer : BaseRenderer
    {
        private const int BoxelSize = 2;
        private const int VerticesPerBoxel = 24;
        private const int EmittedVertices = 36;

        public CubeNoGSRenderer(Device1 Device)
            : base("PRShaders.hlsl", "VShader", null, "PShader", PrimitiveTopology.TriangleList, Device)
        {
            
        }

        protected override void PreRender(DeviceContext1 Context)
        {
            
        }

        protected override void GenerateVertexBuffer(IEnumerable<IBoxel> Boxels, Device1 Device, out Buffer VertexBuffer, 
            out VertexBufferBinding Binding, out int VertexCount, int VertexSizeInBytes)
        {
            var Enumerable = Boxels as IBoxel[] ?? Boxels.ToArray();
            VertexCount = Enumerable.Length * EmittedVertices;
            using (var VertexStream = new DataStream(Enumerable.Length * Cube.SizeInBytes, false, true))
            {
                foreach (var Boxel in Enumerable)
                {
                    new Cube(new Vector3(Boxel.Position.X * BoxelSize,
                        Boxel.Position.Y * BoxelSize, Boxel.Position.Z * BoxelSize)).Write(VertexStream);
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

        struct Cube
        {
            private static readonly Vector3[] Offsets;
            private readonly Vector3[] Vertices;
            public static readonly int SizeInBytes;

            static Cube()
            {
                const int CubeOffset = BoxelSize / 2;
                Offsets = new Vector3[8];
                // Offset from center vertex for cube vertices.
                Offsets[0] = new Vector3(-CubeOffset, -CubeOffset, CubeOffset);
                Offsets[1] = new Vector3(CubeOffset, -CubeOffset, CubeOffset);
                Offsets[2] = new Vector3(CubeOffset, CubeOffset, CubeOffset);
                Offsets[3] = new Vector3(-CubeOffset, -CubeOffset, -CubeOffset);
                Offsets[4] = new Vector3(-CubeOffset, CubeOffset, -CubeOffset);
                Offsets[5] = new Vector3(CubeOffset, -CubeOffset, -CubeOffset);
                Offsets[6] = new Vector3(CubeOffset, CubeOffset, -CubeOffset);
                Offsets[7] = new Vector3(-CubeOffset, CubeOffset, CubeOffset);
                SizeInBytes = Vector3.SizeInBytes * EmittedVertices;
            }

            public Cube(Vector3 Position)
            {
                this.Vertices = new Vector3[VerticesPerBoxel];
                this.Vertices[0] = Position + Offsets[0];
                this.Vertices[1] = Position + Offsets[1];
                this.Vertices[2] = Position + Offsets[7];
                this.Vertices[3] = Position + Offsets[2];

                this.Vertices[4] = Position + Offsets[3];
                this.Vertices[5] = Position + Offsets[4];
                this.Vertices[6] = Position + Offsets[5];
                this.Vertices[7] = Position + Offsets[6];

                this.Vertices[8] = Position + Offsets[4];
                this.Vertices[9] = Position + Offsets[7];
                this.Vertices[10] = Position + Offsets[6];
                this.Vertices[11] = Position + Offsets[2];

                this.Vertices[12] = Position + Offsets[3];
                this.Vertices[13] = Position + Offsets[5];
                this.Vertices[14] = Position + Offsets[0];
                this.Vertices[15] = Position + Offsets[1];

                this.Vertices[16] = Position + Offsets[5];
                this.Vertices[17] = Position + Offsets[6];
                this.Vertices[18] = Position + Offsets[1];
                this.Vertices[19] = Position + Offsets[2];

                this.Vertices[20] = Position + Offsets[3];
                this.Vertices[21] = Position + Offsets[0];
                this.Vertices[22] = Position + Offsets[4];
                this.Vertices[23] = Position + Offsets[7];
            }

            public void Write(DataStream Stream)
            {
                Stream.Write(this.Vertices[0]);
                Stream.Write(this.Vertices[1]);
                Stream.Write(this.Vertices[2]);

                Stream.Write(this.Vertices[2]);
                Stream.Write(this.Vertices[1]);
                Stream.Write(this.Vertices[3]);

                Stream.Write(this.Vertices[4]);
                Stream.Write(this.Vertices[5]);
                Stream.Write(this.Vertices[6]);

                Stream.Write(this.Vertices[6]);
                Stream.Write(this.Vertices[5]);
                Stream.Write(this.Vertices[7]);

                Stream.Write(this.Vertices[8]);
                Stream.Write(this.Vertices[9]);
                Stream.Write(this.Vertices[10]);

                Stream.Write(this.Vertices[10]);
                Stream.Write(this.Vertices[9]);
                Stream.Write(this.Vertices[11]);

                Stream.Write(this.Vertices[12]);
                Stream.Write(this.Vertices[13]);
                Stream.Write(this.Vertices[14]);

                Stream.Write(this.Vertices[14]);
                Stream.Write(this.Vertices[13]);
                Stream.Write(this.Vertices[15]);

                Stream.Write(this.Vertices[16]);
                Stream.Write(this.Vertices[17]);
                Stream.Write(this.Vertices[18]);

                Stream.Write(this.Vertices[18]);
                Stream.Write(this.Vertices[17]);
                Stream.Write(this.Vertices[19]);

                Stream.Write(this.Vertices[20]);
                Stream.Write(this.Vertices[21]);
                Stream.Write(this.Vertices[22]);

                Stream.Write(this.Vertices[22]);
                Stream.Write(this.Vertices[21]);
                Stream.Write(this.Vertices[23]);
            }
        }
    }
}
