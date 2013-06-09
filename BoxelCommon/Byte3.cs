using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;

namespace BoxelCommon
{
    [StructLayout(LayoutKind.Auto)]
    public struct Byte3
    {
        public readonly byte X;
        public readonly byte Y;
        public readonly byte Z;

        public Byte3(byte X, byte Y, byte Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Byte3(int HashCode)
        {
            throw new NotImplementedException();
        }

        public Byte3(Int3 XYZ)
        {
            this.X = (byte)XYZ.X;
            this.Y = (byte)XYZ.Y;
            this.Z = (byte)XYZ.Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override int GetHashCode()
        {
            return ZOrderCurveHelper.XWeave[this.X] | ZOrderCurveHelper.YWeave[this.Y] | ZOrderCurveHelper.ZWeave[this.Z];
        }
    }
}
