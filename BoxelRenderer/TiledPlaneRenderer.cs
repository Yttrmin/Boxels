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

namespace BoxelRenderer
{
    public sealed class TiledPlaneRenderer : BaseRenderer
    {
        private Buffer BoxelTextureLookup;
        private const int BoxelSize = 2;
        private readonly Dictionary<Side, FlankingSides<Int3>> SideFlanks;

        public TiledPlaneRenderer(RenderDevice Device, BoxelTypes<ICubeBoxelType> Types)
            : base("TiledPlaneShaders.hlsl", "VShaderTextured", null, "PShaderTextured", 
                PrimitiveTopology.TriangleList, Device, Types)
        {
            this.SideFlanks = new Dictionary<Side, FlankingSides<Int3>>();
            this.SideFlanks[Side.PosX] = new FlankingSides<Int3>(-Int3.UnitZ, Int3.UnitZ, Int3.UnitY, -Int3.UnitY);
            this.SideFlanks[Side.NegX] = new FlankingSides<Int3>(Int3.UnitZ, -Int3.UnitZ, Int3.UnitY, -Int3.UnitY);
            this.SideFlanks[Side.PosY] = new FlankingSides<Int3>(-Int3.UnitZ, Int3.UnitZ, -Int3.UnitX, Int3.UnitX);
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
            using(var Buffer = new DataBuffer(BoxelArray.Length * VertexSizeInBytes))
            {
                IntPtr CurrentPosition = Buffer.DataPointer;
                int FinalSize = 0;
                foreach(var Rect in this.CreateRectangleOutline(BoxelArray))
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

        private Rectangle3D[] CreateRectangleOutline(IEnumerable<IBoxel> Boxels)
        {
            var BoxelPlaneMap = new Dictionary<Tuple<int, Side>, Rectangle3D>();

            foreach (var VisibleBoxel in BoxelHelpers.SideOcclusionCull(Boxels))
            {
                foreach (var Side in BoxelHelpers.AllSides(VisibleBoxel.VisibleSides))
                {
                    // Foreach visible side, check if there's any planes already to the
                    // left, right, top, and bottom. If so, grow the plane to include it.
                    // Else, create a new plane.
                    // Inefficient, but simple.
                    if (Side == Side.NegX || Side == Side.PosX || Side == Side.PosY)
                        RectanglizeBoxel(VisibleBoxel.Boxel, Side, BoxelPlaneMap);
                }
            }

            var FinalSet = new HashSet<Rectangle3D>();
            foreach(var Rect in BoxelPlaneMap.Values)
            {
                FinalSet.Add(Rect);
            }
            return FinalSet.ToArray();
        }

        private void RectanglizeBoxel(IBoxel Boxel, Side Side, 
            Dictionary<Tuple<int, Side>, Rectangle3D> BoxelPlaneMap)
        {
            var RectangleFlanks = LookupFlankBoxels(Boxel, Side, BoxelPlaneMap);
            
            var OurKey = new Tuple<int, Side>(Boxel.Position.ToInt(), Side);


            if (RectangleFlanks.Left != null && RectangleFlanks.Right == null)
            {
                RectangleFlanks.Left.ExtendRight(BoxelSize);
                BoxelPlaneMap[OurKey] = RectangleFlanks.Left;
            }
            else if (RectangleFlanks.Right != null && RectangleFlanks.Left == null)
            {
                RectangleFlanks.Right.ExtendLeft(BoxelSize);
                BoxelPlaneMap[OurKey] = RectangleFlanks.Right;
            }
            else if (RectangleFlanks.Right != null && RectangleFlanks.Left != null)
            {
                //@TODO
            }
            else if (RectangleFlanks.Right == null && RectangleFlanks.Left == null)
            {
                if (Side == Side.PosX)
                {
                    // -X forward, +Z right, -Y below
                    BoxelPlaneMap[OurKey] = new Rectangle3D((Boxel.Position.X + BoxelSize / 2) * BoxelSize,
                        (Boxel.Position.Y + BoxelSize / 2) * BoxelSize, (Boxel.Position.Z - BoxelSize / 2) * BoxelSize,
                        BoxelSize, BoxelSize, Side);
                }
                else if(Side == Side.NegX)
                {
                    // +X forward, -Z right, -Y below
                    BoxelPlaneMap[OurKey] = new Rectangle3D((Boxel.Position.X - BoxelSize / 2) * BoxelSize,
                        (Boxel.Position.Y + BoxelSize / 2) * BoxelSize, (Boxel.Position.Z + BoxelSize / 2) * BoxelSize,
                        BoxelSize, BoxelSize, Side);
                }
                else if(Side == Side.PosY)
                {
                    // -Y forward, +Z right, +X below
                    BoxelPlaneMap[OurKey] = new Rectangle3D((Boxel.Position.X - BoxelSize / 2) * BoxelSize,
                        (Boxel.Position.Y + BoxelSize / 2) * BoxelSize, (Boxel.Position.Z - BoxelSize / 2) * BoxelSize,
                        BoxelSize, BoxelSize, Side);
                }
            }
        }

        private FlankingSides<Rectangle3D> LookupFlankBoxels(IBoxel Boxel, Side Side, 
            Dictionary<Tuple<int, Side>, Rectangle3D> BoxelPlaneMap)
        {
            Rectangle3D Left = null, Right = null;
            var LeftKey = (Boxel.Position + this.SideFlanks[Side].Left).ToIntOrNull();
            var RightKey = (Boxel.Position + this.SideFlanks[Side].Right).ToIntOrNull();
            if(LeftKey.HasValue)
                BoxelPlaneMap.TryGetValue(new Tuple<int, Side>(LeftKey.Value, Side), out Left);
            if(RightKey.HasValue)
                BoxelPlaneMap.TryGetValue(new Tuple<int, Side>(RightKey.Value, Side), out Right);
            return new FlankingSides<Rectangle3D>(Left, Right, null, null);
        }

        protected override void PreRender(DeviceContext1 Context)
        {
            
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

        private sealed class BoxelRectangle
        {
            public VisibleBoxel Boxel { get; private set; }
            public Rectangle3D Rectangle { get; private set; }
        }

        private sealed class Rectangle3D
        {
            // Coordinates of upper-left corner.
            public int X, Y, Z;
            public int Width, Height;
            private readonly Side Side;

            public Rectangle3D(int X, int Y, int Z, int Width, int Height, Side Side)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
                this.Width = Width;
                this.Height = Height;
                this.Side = Side;
            }

            public void ExtendRight(int Amount)
            {
                Width += Amount;
            }

            public void ExtendLeft(int Amount)
            {
                if(Side == Side.PosX)
                {
                    Z -= Amount;
                }
                else if(Side == Side.NegX)
                {
                    Z += Amount;
                }
                Width += Amount;
            }

            public Vertex[] ToVertices()
            {
                var Result = new Vertex[6];
                /*
                  0,5       3
                   +        +
                +X+Y-Z    +X+Y+Z
                        0
                +X-Y-Z    +X-Y+Z 
                   2       1,4
                   +        +
                 */
                if (Side == Side.PosX)
                {
                    Result[0] = Result[5] = new Vertex(new Vector3(X, Y, Z), new Vector3(X, Y, 0));
                    Result[1] = Result[4] = new Vertex(new Vector3(X, Y - Height, Z + Width), new Vector3(X + Width, Y + Height, 0));
                    Result[2]  = new Vertex(new Vector3(X, Y - Height, Z), new Vector3(X, Y + Height, 0));
                    Result[3] = new Vertex(new Vector3(X, Y, Z + Width), new Vector3(X + Width, Y, 0));
                }
                else if (Side == Side.NegX)
                {
                    /*
                       0,5       3 
                       +        +
                      +++      ++-              
                            0
                       2      1,4
                      +-+      +-- 
                       +        +
                     */
                    Result[0] = Result[5] = new Vertex(new Vector3(X, Y, Z), new Vector3(X, Y, 1));
                    Result[1] = Result[4] = new Vertex(new Vector3(X, Y - Height, Z - Width), new Vector3(X + Width, Y + Height, 1));
                    Result[2] = new Vertex(new Vector3(X, Y - Height, Z), new Vector3(X, Y + Height, 1));
                    Result[3] = new Vertex(new Vector3(X, Y, Z - Width), new Vector3(X + Width, Y, 1));
                }
                else if(Side == Side.PosY)
                {
                    Result[0] = Result[5] = new Vertex(new Vector3(X, Y, Z), new Vector3(X, Y, 2));
                    Result[1] = Result[4] = new Vertex(new Vector3(X + Height, Y, Z + Width), new Vector3(X + Width, Y + Height, 2));
                    Result[2] = new Vertex(new Vector3(X + Height, Y, Z), new Vector3(X, Y + Height, 2));
                    Result[3] = new Vertex(new Vector3(X, Y, Z + Width), new Vector3(X + Width, Y, 2));
                }
                return Result;
            }
        }

        private sealed class FlankingSides<T>
        {
            public readonly T Left, Right, Above, Below;

            public FlankingSides(T Left, T Right, T Above, T Below)
            {
                this.Left = Left;
                this.Right = Right;
                this.Above = Above;
                this.Below = Below;
            }
        }
    }
}
