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
        [Timer]
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

        [Timer]
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
