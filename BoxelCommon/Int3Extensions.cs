using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelCommon
{
    public static class Int3Extensions
    {
        public static Int3 Remainder(this Int3 This, int Other)
        {
            return new Int3(This.X % Other, This.Y % Other, This.Z % Other);
        }

        public static int ToInt(this Int3 Self)
        {
            return ZOrderCurveHelper.XWeave[Self.X] | ZOrderCurveHelper.YWeave[Self.Y] | ZOrderCurveHelper.ZWeave[Self.Z];
        }

        public static bool IsUnit(this Int3 Self)
        {
            return Self == Int3.UnitX || Self == -Int3.UnitX || Self == Int3.UnitY || Self == -Int3.UnitY ||
                Self == Int3.UnitZ || Self == -Int3.UnitZ;
        }

        public static int? ToIntOrNull(this Int3 Self)
        {
            int Result;
            if (Self.TryToInt(out Result))
                return Result;
            else
                return null;
        }

        public static bool TryToInt(this Int3 Self, out int Result)
        {
            if(Byte3.CanFit(Self))
            {
                Result = Self.ToInt();
                return true;
            }
            else
            {
                Result = default(int);
                return false;
            }
        }
    }
}
