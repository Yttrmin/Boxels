using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisibleBoxel = BoxelCommon.BoxelHelpers.VisibleBoxel;
using Side = BoxelCommon.BoxelHelpers.Side;
using BoxelCommon;
using System.Diagnostics;

namespace BoxelRenderer
{
    internal interface IBoxelMesher
    {
        Rectangle3D[] CreateRectangleOutline(IEnumerable<IBoxel> Boxels);
    }

    internal class PlaneMesherLowPoly : PlaneMesher
    {
        public PlaneMesherLowPoly(int BoxelSize)
            : base(BoxelSize)
        {

        }
    }

    internal abstract class PlaneMesher : IBoxelMesher
    {
        private readonly int BoxelSize;

        public PlaneMesher(int BoxelSize)
        {
            this.BoxelSize = BoxelSize;
        }

        [Timer]
        public Rectangle3D[] CreateRectangleOutline(IEnumerable<IBoxel> Boxels)
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

            foreach (var VisibleBoxel in BoxelHelpers.SideOcclusionCull(Boxels))
            {
                Grid.Add(VisibleBoxel.Position, VisibleBoxel);
            }

            foreach (var VisibleBoxel in Grid.AllItems)
            {
                foreach (var Side in BoxelHelpers.AllSides(VisibleBoxel.VisibleSides))
                {
                    if (Side != Side.NegX && Side != Side.PosX && Side != Side.PosY)
                        continue;

                    if (Checked[Side].Contains(VisibleBoxel.Boxel))
                        continue;
                    Checked[Side].Add(VisibleBoxel.Boxel);

                    var Rect = this.CreateRectangle(VisibleBoxel.Boxel, Side);
                    RectList.Add(Rect);

                    var Flanks = BoxelHelpers.GetSideFlanks(Side);
                    this.IncludeAllBoxelsInDirection(VisibleBoxel.Position, Flanks.Right, Rect, Grid, Checked[Side]);
                    this.IncludeAllBoxelsInDirection(VisibleBoxel.Position, Flanks.Left, Rect, Grid, Checked[Side]);
                   
                    Trace.WriteLine(String.Format("Done with Pos {1} Side {0}.", Side, VisibleBoxel.Position));
                }
            }
            Trace.WriteLine(String.Format("Outlined world with {0} rectangles.", RectList.Count));
            return RectList.ToArray();
        }

        private void IncludeAllBoxelsInDirection(Int3 StartingPoint, Side Direction, Rectangle3D Rectangle, 
            Grid3D<VisibleBoxel> Grid, ISet<IBoxel> Checked)
        {
            foreach(var VBoxel in Grid.AllItemsFromIndexAlongAxis(StartingPoint, Direction))
            {
                var Added = Checked.Add(VBoxel.Boxel);
                Debug.Assert(Added);
                Rectangle.ExtendDirection(BoxelSize, BoxelHelpers.SideToInt3(Direction));
            }
        }

        private Rectangle3D CreateRectangle(IBoxel Boxel, Side Side)
        {
            Rectangle3D Result = null;
            switch (Side)
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
    }

    internal sealed class BoxelRectangle
    {
        public VisibleBoxel Boxel { get; private set; }
        public Rectangle3D Rectangle { get; private set; }
    }

    internal sealed class Rectangle3D
    {
        // Coordinates of upper-left corner.
        [Obsolete("Use Vector3")]
        public int X, Y, Z;
        [Obsolete("Use two points.")]
        public int Width, Height;
        private Vector3 UpperLeft, LowerRight;
        private readonly BoxelCommon.BoxelHelpers.FlankingSides<Int3> Int3Flanks;
        private readonly Side Side;
        private Int3 LeftToRight, TopToBottom;

        public Rectangle3D(int X, int Y, int Z, int Width, int Height, Side Side)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.Width = Width;
            this.Height = Height;
            this.Side = Side;

            this.UpperLeft = new Vector3(X, Y, Z);
            this.Int3Flanks = BoxelHelpers.GetInt3Flanks(Side);
            this.LowerRight = this.UpperLeft + (Vector3)(this.Int3Flanks.Right * Width) 
                + (Vector3)(this.Int3Flanks.Below * Height);
            var LeftToRightVec = this.LowerRight - (this.UpperLeft + (Vector3)(this.Int3Flanks.Below * Height));
            LeftToRightVec.Normalize();
            this.LeftToRight = new Int3((int)LeftToRightVec.X, (int)LeftToRightVec.Y, (int)LeftToRightVec.Z);
            var TopToBottomVec = this.LowerRight - (this.UpperLeft + (Vector3)(this.Int3Flanks.Right * Width));
            TopToBottomVec.Normalize();
            this.TopToBottom = new Int3((int)TopToBottomVec.X, (int)TopToBottomVec.Y, (int)TopToBottomVec.Z);
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
            else if (Side == Side.PosY)
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

        public void ExtendDirection(int Amount, Int3 Direction)
        {
            if(!Direction.IsUnit())
                throw new InvalidOperationException("Direction is not a unit vector.");

            if(Direction == this.LeftToRight || Direction == this.TopToBottom)
            {
                // Move LowerRight.
                this.LowerRight += (Vector3)(Direction * Amount);
            }
            else if(Direction == -this.LeftToRight || Direction == -this.TopToBottom)
            {
                // Move UpperLeft.
                this.UpperLeft += (Vector3)(Direction * Amount);
            }
            else
            {
                throw new InvalidOperationException("Something went wrong.");
            }
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
                Result[2] = new Vertex(new Vector3(X, Y - Height, Z), new Vector3(X, Y + Height, 0));
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
            else if (Side == Side.PosY)
            {
                Result[0] = Result[5] = new Vertex(new Vector3(X, Y, Z), new Vector3(X, Y, 2));
                Result[1] = Result[4] = new Vertex(new Vector3(X + Height, Y, Z + Width), new Vector3(X + Width, Y + Height, 2));
                Result[2] = new Vertex(new Vector3(X + Height, Y, Z), new Vector3(X, Y + Height, 2));
                Result[3] = new Vertex(new Vector3(X, Y, Z + Width), new Vector3(X + Width, Y, 2));
            }
            return Result;
        }
    }
}
