using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelCommon
{
    public sealed class Grid3D<T>
    {
        private readonly IDictionary<int, T> Contents;
        public int Count { get { throw new NotImplementedException(); } }
        public T this[int Index] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public Grid3D()
        {
            this.Contents = new Dictionary<int, T>();
        }

        public Grid3D(IEnumerable<IBoxel> Boxels)
        {
            throw new NotImplementedException();
        }

        public void Add(int Position, T Item)
        {
            if(this.Contents.ContainsKey(Position))
            {
                throw new InvalidOperationException(String.Format("Position (hash {0}) already occupied in Grid.", Position));
            }
            else
            {
                this.Contents[Position] = Item;
            }
        }

        public void Add(ref Byte3 Position, T Item)
        {
            this.Add(Position.GetHashCode(), Item);
        }

        public void Remove(int Position)
        {
            if (!this.Contents.Remove(Position))
                throw new InvalidOperationException(String.Format("Tried to remove an unoccupied point (hash {0}).", Position));
        }

        public void Remove(ref Byte3 Position)
        {
            this.Remove(Position.GetHashCode());
        }

        public T At(int Position)
        {
            T Result;
            if (!this.Contents.TryGetValue(Position, out Result))
                throw new InvalidOperationException(String.Format("Tried to get item at unoccupied point (hash {0}).", Position));
            return Result;
        }

        public T At(ref Byte3 Position)
        {
            return this.At(Position.GetHashCode());
        }

        public T[] AllItems()
        {
            return this.Contents.Values.ToArray();
        }
    }
}
