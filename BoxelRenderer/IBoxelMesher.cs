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

    internal class PlaneMesherLeftRight : PlaneMesher
    {
        public PlaneMesherLeftRight(int BoxelSize)
            : base(BoxelSize)
        {

        }

        protected override Rectangle3D ProcessBoxelSide(VisibleBoxel VBoxel, Side Side, Grid3D<VisibleBoxel> Grid,
            ISet<IBoxel> Checked)
        {
            var Pos = VBoxel.Position;
            var Rect = new Rectangle3D((Vector3)Pos * this.BoxelSize, this.BoxelSize, Side);

            var Flanks = BoxelHelpers.GetSideFlanks(Side);
            this.IncludeAllBoxelsInDirection(Pos, Flanks.Right, Side, Rect, Grid, Checked);
            this.IncludeAllBoxelsInDirection(Pos, Flanks.Left, Side, Rect, Grid, Checked);

            return Rect;
        }
    }

    internal class PlaneMesherLowPoly : PlaneMesher
    {
        public PlaneMesherLowPoly(int BoxelSize)
            : base(BoxelSize)
        {

        }

        protected override Rectangle3D ProcessBoxelSide(VisibleBoxel VBoxel, Side Side, Grid3D<VisibleBoxel> Grid,
            ISet<IBoxel> Checked)
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class PlaneMesher : IBoxelMesher
    {
        protected readonly int BoxelSize;

        public PlaneMesher(int BoxelSize)
        {
            this.BoxelSize = BoxelSize;
        }
        
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
                    if (Checked[Side].Contains(VisibleBoxel.Boxel))
                        continue;
                    Checked[Side].Add(VisibleBoxel.Boxel);

                    RectList.Add(this.ProcessBoxelSide(VisibleBoxel, Side, Grid, Checked[Side]));
                   
                    //Trace.WriteLine(String.Format("Done with Pos {1} Side {0}.", Side, VisibleBoxel.Position));
                }
            }
            Trace.WriteLine(String.Format("Outlined world with {0} rectangles.", RectList.Count));
            return RectList.ToArray();
        }

        protected abstract Rectangle3D ProcessBoxelSide(VisibleBoxel VBoxel, Side Side, Grid3D<VisibleBoxel> Grid,
            ISet<IBoxel> Checked);

        protected void IncludeAllBoxelsInDirection(Int3 StartingPoint, Side Direction, Side Facing, Rectangle3D Rectangle, 
            Grid3D<VisibleBoxel> Grid, ISet<IBoxel> Checked)
        {
            foreach(var VBoxel in Grid.AllItemsFromIndexAlongAxis(StartingPoint, Direction))
            {
                if ((VBoxel.VisibleSides & Facing) != Facing)
                    break;
                var Added = Checked.Add(VBoxel.Boxel);
                Debug.Assert(Added);
                Rectangle.ExtendDirection(BoxelSize, BoxelHelpers.SideToInt3(Direction));
            }
        }
    }

    internal sealed class BoxelRectangle
    {
        public VisibleBoxel Boxel { get; private set; }
        public Rectangle3D Rectangle { get; private set; }
    }

    internal sealed class Rectangle3D
    {
        private Vector3 UpperLeft, LowerRight;
        private readonly BoxelCommon.BoxelHelpers.FlankingSides<Int3> Int3Flanks;
        private readonly Side Side;
        private Int3 LeftToRight, TopToBottom;

        public Rectangle3D(Vector3 CenterPoint, int BoxelSize, Side Side)
        {
            this.Side = Side;
            this.UpperLeft = CenterPoint;
            this.Int3Flanks = BoxelHelpers.GetInt3Flanks(Side);
            this.LowerRight = this.UpperLeft + (Vector3)(this.Int3Flanks.Right * BoxelSize)
                + (Vector3)(this.Int3Flanks.Below * BoxelSize);
            var LeftToRightVec = this.LowerRight - (this.UpperLeft + (Vector3)(this.Int3Flanks.Below * BoxelSize));
            LeftToRightVec.Normalize();
            this.LeftToRight = new Int3((int)LeftToRightVec.X, (int)LeftToRightVec.Y, (int)LeftToRightVec.Z);
            var TopToBottomVec = this.LowerRight - (this.UpperLeft + (Vector3)(this.Int3Flanks.Right * BoxelSize));
            TopToBottomVec.Normalize();
            this.TopToBottom = new Int3((int)TopToBottomVec.X, (int)TopToBottomVec.Y, (int)TopToBottomVec.Z);

            Debug.Assert(this.LeftToRight.IsUnit());
            Debug.Assert(this.TopToBottom.IsUnit());

            // To align:
            // Move FORWARD BoxelSize/2.
            // Move ABOVE BoxelSize/2.
            // Move LEFT BoxelSize/2.
            this.MoveDirection(BoxelSize / 2, Int3Flanks.Forward);
            this.MoveDirection(BoxelSize / 2, Int3Flanks.Above);
            this.MoveDirection(BoxelSize / 2, Int3Flanks.Left);
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

        private void MoveDirection(int Amount, Int3 Direction)
        {
            var Delta = (Vector3)(Amount * Direction);

            this.LowerRight += Delta;
            this.UpperLeft += Delta;
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

            var LowerLeft = ((Vector3)this.TopToBottom).Absolute();
            LowerLeft *= this.LowerRight;
            LowerLeft = LowerLeft.InverseMask(this.UpperLeft, (Vector3)this.TopToBottom.Absolute());

            var UpperRight = ((Vector3)this.LeftToRight).Absolute();
            UpperRight *= this.LowerRight;
            UpperRight = UpperRight.InverseMask(this.UpperLeft, (Vector3)this.LeftToRight.Absolute());

            Result[0] = Result[5] = new Vertex((Vector3)this.UpperLeft, new Vector3(this.UpperLeft.X, this.UpperLeft.Y, 0));
            Result[1] = Result[4] = new Vertex((Vector3)this.LowerRight, new Vector3(this.LowerRight.X, this.LowerRight.Y, 0));
            Result[2] = new Vertex(new Vector3(LowerLeft.X, LowerLeft.Y, LowerLeft.Z), new Vector3(0, 0, 0));
            Result[3] = new Vertex(new Vector3(UpperRight.X, UpperRight.Y, UpperRight.Z), Vector3.Zero);

            //Trace.WriteLine(String.Format("{4}\n{0}-----{1}\n{2}-----{3}", Result[0].Position, Result[3].Position, 
            //    Result[2].Position, Result[1].Position, this.Side));

            return Result;
        }
    }
}
