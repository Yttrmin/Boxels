using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Side = BoxelCommon.BoxelHelpers.Side;
using Int3 = SharpDX.Int3;

namespace BoxelCommon
{
    //@TODO - Public methods can only take an Int3 or Byte3. No ints.
    public sealed class Grid3D<T> where T: IPositionable
    {
        private readonly IDictionary<int, T> Contents;
        private static readonly IDictionary<Side, Int3> SideToUnit;
        public int Count { get { throw new NotImplementedException(); } }
        public T this[int Index] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        static Grid3D()
        {
            SideToUnit = new Dictionary<Side, Int3>();
            SideToUnit[Side.PosX] = Int3.UnitX;
            SideToUnit[Side.NegX] = -Int3.UnitX;
            SideToUnit[Side.PosY] = Int3.UnitY;
            SideToUnit[Side.NegY] = -Int3.UnitY;
            SideToUnit[Side.PosZ] = Int3.UnitZ;
            SideToUnit[Side.NegZ] = -Int3.UnitZ;
        }

        public Grid3D()
        {
            this.Contents = new Dictionary<int, T>();
        }

        public void Add(Int3 Position, T Item)
        {
            //@TODO - Byte3 check.
            this.Add(Position.ToInt(), Item);
        }

        public void Add(ref Byte3 Position, T Item)
        {
            this.Add(Position.GetHashCode(), Item);
        }

        private void Add(int Code, T Item)
        {
            if (this.Contents.ContainsKey(Code))
            {
                throw new InvalidOperationException(String.Format("Position (hash {0}) already occupied in Grid.", Code));
            }
            else
            {
                this.Contents[Code] = Item;
            }
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

        private bool IsOccupied(int Position)
        {
            return this.Contents.ContainsKey(Position);
        }

        private T AtOrDefault(int Position)
        {
            T Result;
            if (this.Contents.TryGetValue(Position, out Result))
                return Result;
            else
                return default(T);
        }

        public T At(T Item, Side Side)
        {
            switch(Side)
            {
                case Side.PosX:
                    return this.AtOrDefault((Item.Position + Int3.UnitX).ToInt());
                default:
                    return default(T);
            }
        }

        public bool TryAt(Int3 Position, out T Item)
        {
            if (!Byte3.CanFit(Position))
            {
                Item = default(T);
                return false;
            }
            return this.Contents.TryGetValue(Position.ToInt(), out Item);
        }

        public IEnumerable<T> AllItems
        {
            get
            {
                foreach(var Object in this.Contents.Values)
                {
                    yield return Object;
                }
            }
        }

        public IEnumerable<T> AllItemsFromIndexAlongAxis(Int3 Position, Side Axis)
        {

            T Item;
            switch(Axis)
            {
                case Side.PosX:
                    for (var Pos = Position + Int3.UnitX; this.TryAt(Pos, out Item) != false; Pos += Int3.UnitX)
                    {
                        yield return Item;
                    }
                    break;
                case Side.NegX:
                    for (var Pos = Position - Int3.UnitX; this.TryAt(Pos, out Item) != false; Pos -= Int3.UnitX)
                    {
                        yield return Item;
                    }
                    break;
                case Side.PosZ:
                    for(var Pos = Position + Int3.UnitZ; this.TryAt(Pos, out Item) != false; Pos += Int3.UnitZ)
                    {
                        yield return Item;
                    }
                    break;
                case Side.NegZ:
                    for (var Pos = Position - Int3.UnitZ; this.TryAt(Pos, out Item) != false; Pos -= Int3.UnitZ)
                    {
                        yield return Item;
                    }
                    break;
                case Side.NegY:
                    for (var Pos = Position - Int3.UnitY; this.TryAt(Pos, out Item) != false; Pos -= Int3.UnitY)
                    {
                        yield return Item;
                    }
                    break;
                case Side.PosY:
                    for (var Pos = Position + Int3.UnitY; this.TryAt(Pos, out Item) != false; Pos += Int3.UnitY)
                    {
                        yield return Item;
                    }
                    break;
                default:
                    throw new NotImplementedException();
                    break;
            }
        }

        public IEnumerable<T> AllItemsBetween(Int3 Start, Int3 End)
        {
            if (Start == End)
                yield break;

            Side Axis = Side.None;
            if (Start.X < End.X)
                Axis = Side.PosX;
            else if (Start.X > End.X)
                Axis = Side.NegX;
            else if (Start.Y < End.Y)
                Axis = Side.PosY;
            else if (Start.Y > End.Y)
                Axis = Side.NegY;
            else if (Start.Z < End.Z)
                Axis = Side.PosZ;
            else if (Start.Z > End.Z)
                Axis = Side.NegZ;
            if (Axis == Side.None)
                throw new InvalidOperationException();

            T Item;

            for(var Pos = Start; Pos != End; Pos += SideToUnit[Axis])
            {
                if (this.TryAt(Pos, out Item))
                    yield return Item;
            }

            if (this.TryAt(End, out Item))
                yield return Item;
        }

        public IEnumerable<T> AllItemsBetween(Int3 Start, Side Axis, int Amount)
        {
            switch (Axis)
            {
                case Side.PosX:
                    return this.AllItemsBetween(Start, new Int3(Start.X + Amount, Start.Y, Start.Z));
                case Side.NegX:
                    return this.AllItemsBetween(Start, new Int3(Start.X - Amount, Start.Y, Start.Z));
                case Side.PosY:
                    return this.AllItemsBetween(Start, new Int3(Start.X, Start.Y + Amount, Start.Z));
                case Side.NegY:
                    return this.AllItemsBetween(Start, new Int3(Start.X, Start.Y - Amount, Start.Z));
                case Side.PosZ:
                    return this.AllItemsBetween(Start, new Int3(Start.X, Start.Y, Start.Z + Amount));
                case Side.NegZ:
                    return this.AllItemsBetween(Start, new Int3(Start.X, Start.Y, Start.Z - Amount));
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
