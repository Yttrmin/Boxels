using BoxelLib;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Timer]
        public static IEnumerable<Tuple<IBoxel, BoxelHelpers.Side>> SideOcclusionCull(IEnumerable<IBoxel> Boxels)
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
                yield return new Tuple<IBoxel, Side>(Boxel, Sides);
            }
        }
    }
}
