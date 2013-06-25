using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;

namespace BoxelCommon
{
    public class ConstantRandomContainer : IBoxelContainer
    {
        internal const int ChunkSize = 16;
        private readonly IDictionary<int, ConstantChunk> Chunks;

        public ConstantRandomContainer()
        {
            this.Chunks = new Dictionary<int, ConstantChunk>();
        }

        public int ChunkCount
        {
            get { return this.Chunks.Count; }
        }

        public int Count
        {
            get
            {
                var Result = 0;
                foreach (var Chunk in this.Chunks.Values)
                {
                    Result += Chunk.Count;
                }
                return Result;
            }
        }

        public IBoxel AtOrDefault(Int3 Position)
        {
            throw new NotImplementedException();
        }

        public void Add(IBoxel Boxel, Int3 Position)
        {
            Byte3 ChunkPos;
            Byte3 InternPos;
            this.FullPosition(Position, out ChunkPos, out InternPos);
            var Chunk = this.LazyGetChunk(ChunkPos.GetHashCode());
            Chunk.Add(Boxel, InternPos);
        }

        public IEnumerable<IBoxel> AllBoxels
        {
            get
            {
                IEnumerable<IBoxel> Result = new IBoxel[0];
                foreach (var Chunk in this.Chunks.Values)
                {
                    Result = Result.Union(Chunk.AllBoxels);
                }
                return Result;
            }
        }

        public IEnumerable<IBoxel> BoxelsInRadius(Byte3 Position, int RadiusInBoxels)
        {
            return AllBoxels;
            //return this.Chunks[Position.GetHashCode()].AllBoxels;
        }

        public void Compact()
        {
            foreach (var Chunk in this.Chunks.Values)
            {
                Chunk.Compact();
            }
        }

        private ConstantChunk LazyGetChunk(int Position)
        {
            if (!this.Chunks.ContainsKey(Position))
                this.Chunks[Position] = new ConstantChunk(ChunkSize);
            return this.Chunks[Position];
        }

        private void FullPosition(Int3 Position, out Byte3 ChunkPosition, out Byte3 InternalPosition)
        {
            ChunkPosition = new Byte3(Position / ChunkSize);
            InternalPosition = new Byte3(Position.Remainder(ChunkSize));
        }
    }

    internal static class Int3Extensions
    {
        public static Int3 Remainder(this Int3 This, int Other)
        {
            return new Int3(This.X % Other, This.Y % Other, This.Z % Other);
        }
    }

    internal class ConstantChunk
    {
        private readonly IDictionary<int, IBoxel> Boxels;
        public int Count { get { return this.Boxels.Count; } }
        public IEnumerable<IBoxel> AllBoxels { get { return this.Boxels.Values; } }

        public ConstantChunk(int Size)
        {
            this.Boxels = new Dictionary<int, IBoxel>((int)Math.Pow(Size, 3));
        }

        public void Compact()
        {
            throw new NotImplementedException();
        }

        public void Add(IBoxel Boxel, Byte3 Position)
        {
            Debug.Assert(!this.Boxels.ContainsKey(Position.GetHashCode()));
            this.Boxels[Position.GetHashCode()] = Boxel;
        }
    }

    public static class ChunkPosition
    {
        public static Byte3 From(Vector3 Vect)
        {
            checked
            {
                return new Byte3((byte)(((byte)Vect.X) / ConstantRandomContainer.ChunkSize),
                                 (byte)(((byte)Vect.Y) / ConstantRandomContainer.ChunkSize),
                                 (byte)(((byte)Vect.Z) / ConstantRandomContainer.ChunkSize));
            }
        }

        public static int HashSphere(Byte3 Center, int Radius)
        {
            checked
            {
                var ChunkRadius = (byte)(Radius / ConstantRandomContainer.ChunkSize);
                var LowX = new Byte3((byte)(Center.X - Math.Min(ChunkRadius, Center.X)), Center.Y, Center.Z);
                var HighX = new Byte3((byte)(Center.X + Math.Min(ChunkRadius, byte.MaxValue - Center.X)), Center.Y,
                                      Center.Z);

                var LowY = new Byte3(Center.X, (byte)(Center.Y - Math.Min(ChunkRadius, Center.Y)), Center.Z);
                var HighY = new Byte3(Center.X, (byte)(Center.Y + Math.Min(ChunkRadius, byte.MaxValue - Center.Y)),
                                      Center.Z);

                var LowZ = new Byte3(Center.X, Center.Y, (byte)(Center.Z - Math.Min(ChunkRadius, Center.Z)));
                var HighZ = new Byte3(Center.X, Center.Y,
                                      (byte)(Center.Z + Math.Min(ChunkRadius, byte.MaxValue - Center.Z)));
                return LowX.GetHashCode() + HighX.GetHashCode() + LowY.GetHashCode() + HighY.GetHashCode() +
                       LowZ.GetHashCode() + HighZ.GetHashCode();
            }
        }
    }
}
