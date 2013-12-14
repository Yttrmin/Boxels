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

        public static Int3 Absolute(this Int3 Self)
        {
            return new Int3(Math.Abs(Self.X), Math.Abs(Self.Y), Math.Abs(Self.Z));
        }

        public static Vector3 Absolute(this Vector3 Self)
        {
            return new Vector3(Math.Abs(Self.X), Math.Abs(Self.Y), Math.Abs(Self.Z));
        }

        public static Int3 Mask(this Int3 Self, Int3 Other, Int3 Mask)
        {
            return new Int3(Mask.X == 0 ? Self.X : Other.X, Mask.Y == 0 ? Self.Y : Other.Y,
                Mask.Z == 0 ? Self.Z : Other.Z);
        }

        public static Vector3 Mask(this Vector3 Self, Vector3 Other, Vector3 Mask)
        {
            return new Vector3(Mask.X == 0 ? Self.X : Other.X, Mask.Y == 0 ? Self.Y : Other.Y,
                Mask.Z == 0 ? Self.Z : Other.Z);
        }

        public static Vector3 InverseMask(this Vector3 Self, Vector3 Other, Vector3 Mask)
        {
            return new Vector3(Mask.X == 0 ? Other.X : Self.X, Mask.Y == 0 ? Other.Y : Self.Y,
                Mask.Z == 0 ? Other.Z : Self.Z);
        }

        public static void Multiply(this Int3 Self, Int3 Other)
        {
            Self.X *= Other.X;
            Self.Y *= Other.Y;
            Self.Z *= Other.Z;
        }

        public static Int3 ToInt3(this Vector3 Self)
        {
            return new Int3((int)Self.X, (int)Self.Y, (int)Self.Z);
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
