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
        private readonly Dictionary<Side, FlankingSides<Side>> SideFlanks;

        public TiledPlaneRenderer(RenderDevice Device, BoxelTypes<ICubeBoxelType> Types)
            : base("TiledPlaneShaders.hlsl", "VShaderTextured", null, "PShaderTextured", 
                PrimitiveTopology.TriangleList, Device, Types)
        {
            this.SideFlanks = new Dictionary<Side, FlankingSides<Side>>();
            this.SideFlanks[Side.PosX] = new FlankingSides<Side>(Side.NegZ, Side.PosZ, Side.PosY, Side.NegY);
            this.SideFlanks[Side.NegX] = new FlankingSides<Side>(Side.PosZ, Side.NegZ, Side.PosY, Side.NegY);
            this.SideFlanks[Side.PosY] = new FlankingSides<Side>(Side.NegZ, Side.PosZ, Side.NegX, Side.PosX);
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

        [Timer]
        private Rectangle3D[] CreateRectangleOutline(IEnumerable<IBoxel> Boxels)
        {
            var Checked = new Dictionary<Side, HashSet<IBoxel>>();
            Checked[BoxelHelpers.Side.NegX] = new HashSet<IBoxel>();
            Checked[BoxelHelpers.Side.PosX] = new HashSet<IBoxel>();
            Checked[BoxelHelpers.Side.PosY] = new HashSet<IBoxel>();
            Checked[BoxelHelpers.Side.NegY] = new HashSet<IBoxel>();
            Checked[BoxelHelpers.Side.PosZ] = new HashSet<IBoxel>();
            Checked[BoxelHelpers.Side.NegZ] = new HashSet<IBoxel>();
            var RectList = new List<Rectangle3D>();
            var Grid = new Grid3D<VisibleBoxel>();

            foreach(var VisibleBoxel in BoxelHelpers.SideOcclusionCull(Boxels))
            {
                Grid.Add(VisibleBoxel.Position, VisibleBoxel);
            }

            foreach(var VisibleBoxel in Grid.AllItems)
            {
                /*if (Checked.Contains(VisibleBoxel.Boxel))
                    continue;
                Checked.Add(VisibleBoxel.Boxel);*/

                foreach(var Side in BoxelHelpers.AllSides(VisibleBoxel.VisibleSides))
                {
                    if (Side != Side.NegX && Side != Side.PosX && Side != Side.PosY)
                        continue;

                    if (Checked[Side].Contains(VisibleBoxel.Boxel))
                        continue;
                    Checked[Side].Add(VisibleBoxel.Boxel);

                    var Rect = this.CreateRectangle(VisibleBoxel.Boxel, Side);
                    RectList.Add(Rect);

                    if (Side == BoxelHelpers.Side.PosX)Trace.WriteLine(String.Format("Go {0}", VisibleBoxel.Position));

                    IBoxel RightMost = VisibleBoxel.Boxel;
                    foreach(var Boxel in Grid.AllItemsFromIndexAlongAxis(VisibleBoxel.Position, this.SideFlanks[Side].Right))
                    {
                        if (Side == BoxelHelpers.Side.PosX)Trace.WriteLine(String.Format("Right {0}", Boxel.Position));
                        RightMost = Boxel.Boxel;
                        var Added = Checked[Side].Add(Boxel.Boxel);
                        Debug.Assert(Added);
                        Rect.ExtendRight(BoxelSize);
                    }

                    IBoxel LeftMost = VisibleBoxel.Boxel;
                    foreach (var Boxel in Grid.AllItemsFromIndexAlongAxis(VisibleBoxel.Position, this.SideFlanks[Side].Left))
                    {
                        if (Side == BoxelHelpers.Side.PosX)Trace.WriteLine(String.Format("Left {0}", Boxel.Position));
                        LeftMost = Boxel.Boxel;
                        var Added = Checked[Side].Add(Boxel.Boxel);
                        Debug.Assert(Added);
                        Rect.ExtendLeft(BoxelSize);
                    }

                    foreach (var Boxel in Grid.AllItemsFromIndexAlongAxis(LeftMost.Position, this.SideFlanks[Side].Above))
                    {
                        if (Side == BoxelHelpers.Side.PosX)Trace.WriteLine(String.Format("Left {0}", Boxel.Position));
                        Rect.ExtendAbove(BoxelSize);
                        foreach (var BetwixtBoxel in Grid.AllItemsBetween(Boxel.Position, this.SideFlanks[Side].Right,
                            Rect.Width / 2 - 1))
                        {
                            if (Side == BoxelHelpers.Side.PosX)Trace.WriteLine(String.Format("Betwixt {0}", BetwixtBoxel.Position));
                            var Added = Checked[Side].Add(BetwixtBoxel.Boxel);
                            Debug.Assert(Added);
                        }
                    }
                    foreach (var Boxel in Grid.AllItemsFromIndexAlongAxis(LeftMost.Position, this.SideFlanks[Side].Below))
                    {
                        if(Side == BoxelHelpers.Side.PosX)Trace.WriteLine(String.Format("Down {0}", Boxel.Position));
                        Rect.ExtendBelow(BoxelSize);
                        foreach (var BetwixtBoxel in Grid.AllItemsBetween(Boxel.Position, this.SideFlanks[Side].Right,
                            Rect.Width / 2 - 1))
                        {
                            if (Side == BoxelHelpers.Side.PosX)Trace.WriteLine(String.Format("Betwixt {0}", BetwixtBoxel.Position));
                            var Added = Checked[Side].Add(BetwixtBoxel.Boxel);
                            Debug.Assert(Added);
                        }
                    }
                    Trace.WriteLine(String.Format("Done with Pos {1} Side {0}.", Side, VisibleBoxel.Position));
                }
            }
            Trace.WriteLine(String.Format("Outlined world with {0} rectangles.", RectList.Count));
            return RectList.ToArray();
        }

        private Rectangle3D CreateRectangle(IBoxel Boxel, Side Side)
        {
            Rectangle3D Result = null;
            switch(Side)
            {
                case Side.NegX:
                    // +X forward, -Z right, -Y below
                    Result = new Rectangle3D((Boxel.Position.X - BoxelSize / 2) * BoxelSize,
                        (Boxel.Position.Y + BoxelSize / 2) * BoxelSize, (Boxel.Position.Z + BoxelSize / 2) * BoxelSize,
                        BoxelSize, BoxelSize, Side);
                    break;
                case Side.PosX:
                    Result = new Rectangle3D((Boxel.Position.X + BoxelSize / 2) * BoxelSize,
                        (Boxel.Position.Y + BoxelSize / 2) * BoxelSize, (Boxel.Position.Z - BoxelSize / 2) * BoxelSize,
                        BoxelSize, BoxelSize, Side);
                    break;
                case Side.PosY:
                    // -Y forward, +Z right, +X below
                    Result = new Rectangle3D((Boxel.Position.X - BoxelSize / 2) * BoxelSize,
                        (Boxel.Position.Y + BoxelSize / 2) * BoxelSize, (Boxel.Position.Z + BoxelSize / 2) * BoxelSize,
                        BoxelSize, BoxelSize, Side);
                    break;
                default:
                    throw new NotImplementedException();
                    break;
            }
            return Result;
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
                if (Side == Side.PosX || Side == Side.PosY)
                {
                    Z -= Amount;
                }
                else if (Side == Side.NegX)
                {
                    Z += Amount;
                }
                else
                    throw new NotImplementedException();
                Width += Amount;
            }

            public void ExtendAbove(int Amount)
            {
                if (Side == Side.NegX || Side == Side.PosX)
                {
                    Y += Amount;
                }
                else if(Side == Side.PosY)
                {
                    X -= Amount;
                }
                else
                    throw new NotImplementedException();
                Height += Amount;
            }

            public void ExtendBelow(int Amount)
            {
                Height += Amount;
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
