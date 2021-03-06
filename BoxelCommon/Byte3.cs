﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

        public static bool CanFit(Int3 Other)
        {
            return Other.X >= byte.MinValue && Other.X <= byte.MaxValue &&
                Other.Y >= byte.MinValue && Other.Y <= byte.MaxValue &&
                Other.Z >= byte.MinValue && Other.Z <= byte.MaxValue;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override int GetHashCode()
        {
            return ZOrderCurveHelper.XWeave[this.X] | ZOrderCurveHelper.YWeave[this.Y] | ZOrderCurveHelper.ZWeave[this.Z];
        }

        public override bool Equals(object obj)
        {
            return this.Equals((Byte3)obj);
        }

        public bool Equals(Byte3 Other)
        {
            // FYI tests showed this is faster than a hash code comparison.
            // Unless the hash codes are cached. Which is never.
            return this.X == Other.X && this.Y == Other.Y && this.Z == Other.Z;
        }
    }
}
