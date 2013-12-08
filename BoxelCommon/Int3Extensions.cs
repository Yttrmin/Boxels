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
    }
}
