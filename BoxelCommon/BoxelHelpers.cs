using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BoxelCommon
{
    public static class BoxelHelpers
    {
        private static readonly IDictionary<Side, Int3> SideToInt3Map;
        private static readonly Dictionary<Side, FlankingSides<Side>> SideFlanks;

        [Flags]
        public enum Side
        {
            None = 1 << 0,
            PosX = 1 << 1,
            NegX = 1 << 2,
            PosY = 1 << 3,
            NegY = 1 << 4,
            PosZ = 1 << 5,
            NegZ = 1 << 6,
            All = PosX | NegX | PosY | NegY | PosZ | NegZ
        }

        static BoxelHelpers()
        {
            SideToInt3Map = new Dictionary<Side, Int3>();
            SideToInt3Map[Side.PosX] = Int3.UnitX;
            SideToInt3Map[Side.NegX] = -Int3.UnitX;
            SideToInt3Map[Side.PosY] = Int3.UnitY;
            SideToInt3Map[Side.NegY] = -Int3.UnitY;
            SideToInt3Map[Side.PosZ] = Int3.UnitZ;
            SideToInt3Map[Side.NegZ] = -Int3.UnitZ;

            SideFlanks = new Dictionary<Side, FlankingSides<Side>>();
            SideFlanks[Side.PosX] = new FlankingSides<Side>(Side.NegZ, Side.PosZ, Side.PosY, Side.NegY, Side.PosX, Side.NegX);
            SideFlanks[Side.NegX] = new FlankingSides<Side>(Side.PosZ, Side.NegZ, Side.PosY, Side.NegY, Side.NegX, Side.PosX);
            SideFlanks[Side.PosY] = new FlankingSides<Side>(Side.NegZ, Side.PosZ, Side.NegX, Side.PosX, Side.PosY, Side.NegY);
            SideFlanks[Side.NegY] = new FlankingSides<Side>(Side.PosZ, Side.NegZ, Side.NegX, Side.PosX, Side.NegY, Side.PosY);
            SideFlanks[Side.PosZ] = new FlankingSides<Side>(Side.PosX, Side.NegX, Side.PosY, Side.NegY, Side.PosZ, Side.NegZ);
            SideFlanks[Side.NegZ] = new FlankingSides<Side>(Side.NegX, Side.PosX, Side.PosY, Side.NegY, Side.NegZ, Side.PosZ);
        }

        public static int NumberOfSides(Side Sides)
        {
            var Result = 0;
            while (Sides != 0)
            {
                Sides &= Sides - 1;
                Result++;
            }
            return Result;
        }

        public static IEnumerable<Side> AllSides(Side Sides)
        {
            if ((Sides & Side.NegX) == Side.NegX)
                yield return Side.NegX;
            if ((Sides & Side.NegY) == Side.NegY)
                yield return Side.NegY;
            if ((Sides & Side.NegZ) == Side.NegZ)
                yield return Side.NegZ;
            if ((Sides & Side.PosX) == Side.PosX)
                yield return Side.PosX;
            if ((Sides & Side.PosY) == Side.PosY)
                yield return Side.PosY;
            if ((Sides & Side.PosZ) == Side.PosZ)
                yield return Side.PosZ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Boxels"></param>
        /// <param name="Container"></param>
        /// <returns></returns>
        public static IEnumerable<IBoxel> OcclusionCull(IEnumerable<IBoxel> Boxels)
        {
            foreach (var Boxel in Boxels)
            {
                if (Boxel.Container.AtOrDefault(Boxel.Position + Int3.UnitX) != null)
                {
                    yield return Boxel;
                }
            }
        }

        public static Int3 SideToInt3(Side Side)
        {
            return BoxelHelpers.SideToInt3Map[Side];
        }

        public sealed class FlankingSides<T>
        {
            public readonly T Left, Right, Above, Below, Forward, Backward;

            public FlankingSides(T Left, T Right, T Above, T Below, T Forward, T Backward)
            {
                this.Left = Left;
                this.Right = Right;
                this.Above = Above;
                this.Below = Below;
                this.Forward = Forward;
                this.Backward = Backward;
            }
        }

        public static FlankingSides<Side> GetSideFlanks(Side SideFacing)
        {
            return BoxelHelpers.SideFlanks[SideFacing];
        }

        public static FlankingSides<Int3> GetInt3Flanks(Side SideFacing)
        {
            var SideFlanks = GetSideFlanks(SideFacing);
            return new FlankingSides<Int3>(SideToInt3Map[SideFlanks.Left], SideToInt3Map[SideFlanks.Right],
                SideToInt3Map[SideFlanks.Above], SideToInt3Map[SideFlanks.Below], SideToInt3Map[SideFlanks.Forward],
                SideToInt3Map[SideFlanks.Backward]);
        }

        [StructLayout(LayoutKind.Auto)]
        public struct VisibleBoxel : IBoxel
        {
            public readonly IBoxel Boxel;
            public readonly BoxelHelpers.Side VisibleSides;

            public VisibleBoxel(IBoxel Boxel, BoxelHelpers.Side VisibleSides)
            {
                this.Boxel = Boxel;
                this.VisibleSides = VisibleSides;
            }

            public float Scale
            {
                get { return this.Boxel.Scale; }
            }

            public Int3 Position
            {
                get { return this.Boxel.Position; }
            }

            public int Type
            {
                get { return this.Boxel.Type; }
            }

            public IBoxelContainer Container
            {
                get
                {
                    return this.Boxel.Container;
                }
                set
                {
                    this.Boxel.Container = value;
                }
            }
        }
        
        public static IEnumerable<VisibleBoxel> SideOcclusionCull(IEnumerable<IBoxel> Boxels)
        {
            foreach (var Boxel in Boxels)
            {
                var Sides = BoxelHelpers.Side.None;
                var Container = Boxel.Container;
                if (Container.AtOrDefault(Boxel.Position + Int3.UnitX) == null)
                    Sides |= Side.PosX;
                if (Container.AtOrDefault(Boxel.Position - Int3.UnitX) == null)
                    Sides |= Side.NegX;
                if (Container.AtOrDefault(Boxel.Position + Int3.UnitY) == null)
                    Sides |= Side.PosY;
                if (Container.AtOrDefault(Boxel.Position - Int3.UnitY) == null)
                    Sides |= Side.NegY;
                if (Container.AtOrDefault(Boxel.Position + Int3.UnitZ) == null)
                    Sides |= Side.PosZ;
                if (Container.AtOrDefault(Boxel.Position - Int3.UnitZ) == null)
                    Sides |= Side.NegZ;
                if (Sides == BoxelHelpers.Side.None)
                    continue;
                yield return new VisibleBoxel(Boxel, Sides);
            }
        }
    }
}
