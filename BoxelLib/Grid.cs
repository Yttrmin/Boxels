using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NativeWrappers;

namespace BoxelLib
{
    public class Grid
    {
        private Octree<IBoxel> Chunks;
        private int X;
        private int Y;
        private int Z;

        public Grid(int Size)
        {
            if (Math.Log(Size, 2) % 1 != 0.0)
            {
                throw new ArgumentException(String.Format("Size parameter must be a power of 2, got {0} instead.", Size));
            }
            this.X = this.Y = this.Z = (int)Math.Log(Size, 2);
            this.Chunks = new Octree<IBoxel>(Size);
        }

        public Grid(int X, int Y, int Z)
            : this(Math.Max(Math.Max(X,Y),Z))
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public IEnumerable<IBoxel> Boxels
        {
            get { return null; }
        }
    }
}
