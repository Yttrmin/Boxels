using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using ProtoBuf;

namespace BoxelCommon
{
    [Obsolete("Use Grid3D.")]
    [ProtoContract]
    public sealed class ConstantRandomContainer : IBoxelContainer
    {
        /// <summary>
        /// Number of boxels per chunk in each dimension.
        /// </summary>
        internal const int ChunkSize = 16;
        [ProtoMember(1)]
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
            Byte3 ChunkPos;
            Byte3 InternPos;
            this.FullPosition(Position, out ChunkPos, out InternPos);
            var Chunk = this.LazyGetChunk(ChunkPos.GetHashCode(), true);
            return Chunk.AtOrDefault(InternPos);
        }

        public void Add(IBoxel Boxel, Int3 Position)
        {
            Byte3 ChunkPos;
            Byte3 InternPos;
            this.FullPosition(Position, out ChunkPos, out InternPos);
            var Chunk = this.LazyGetChunk(ChunkPos.GetHashCode(), false);
            Chunk.Add(Boxel, InternPos);
        }

        [Timer]
        public IEnumerable<IBoxel> AllBoxels
        {
            get
            {
                var Result = new List<IBoxel>(this.Count);
                foreach (var Chunk in this.Chunks.Values)
                {
                    Result.AddRange(Chunk.AllBoxels);
                }
                return Result.ToArray();
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

        public void Save(System.IO.Stream Stream)
        {
            Serializer.Serialize(Stream, this);
        }

        public static ConstantRandomContainer Load(System.IO.Stream Stream)
        {
            return Serializer.Deserialize<ConstantRandomContainer>(Stream);
        }

        private ConstantChunk LazyGetChunk(int Position, bool ReadOnly)
        {
            ConstantChunk Result;
            this.Chunks.TryGetValue(Position, out Result);
            if (Result != null)
            {
                return Result;
            }
            else
            {
                Result = new ConstantChunk(ChunkSize);
                this.Chunks[Position] = Result;
                return Result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Position"></param>
        /// <param name="ChunkPosition"></param>
        /// <param name="InternalPosition"></param>
        private void FullPosition(Int3 Position, out Byte3 ChunkPosition, out Byte3 InternalPosition)
        {
            ChunkPosition = new Byte3(Position / ChunkSize);
            InternalPosition = new Byte3(Position.Remainder(ChunkSize));
        }

        [ProtoAfterDeserialization]
        [Timer]
        private void PostDeserializationSetContainerReference()
        {
            foreach (var Boxel in this.AllBoxels)
            {
                Boxel.Container = this;
            }
        }
    }

    internal static class Int3Extensions
    {
        public static Int3 Remainder(this Int3 This, int Other)
        {
            return new Int3(This.X % Other, This.Y % Other, This.Z % Other);
        }
    }

    [ProtoContract]
    internal sealed class ConstantChunk : IChunk
    {
        [ProtoMember(1)]
        private readonly IDictionary<int, IBoxel> Boxels;
        private const bool PreAllocate = false;
        public int Count { get { return this.Boxels.Count; } }
        public IEnumerable<IBoxel> AllBoxels { get { return this.Boxels.Values; } }

        /// <summary>
        /// Exists for protobuf only.
        /// </summary>
        private ConstantChunk()
        {

        }

        public ConstantChunk(int Size)
        {
            this.Boxels = PreAllocate ? new Dictionary<int, IBoxel>((int)Math.Pow(Size, 3)) : new Dictionary<int,IBoxel>(0);
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

        public IBoxel AtOrDefault(Byte3 Position)
        {
            IBoxel Result;
            this.Boxels.TryGetValue(Position.GetHashCode(), out Result);
            return Result;
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
