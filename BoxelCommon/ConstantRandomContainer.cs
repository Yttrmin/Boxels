using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;
using ProtoBuf;

namespace BoxelCommon
{
	[ProtoContract]
	public class ConstantRandomContainer : IBoxelContainer
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
			if (!this.Chunks.ContainsKey(Position))
			{
				//@TODO - ReadOnly optimization.
				this.Chunks[Position] = new ConstantChunk(ChunkSize);
			}
			return this.Chunks[Position];
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
	internal class ConstantChunk : IChunk
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
			if (this.Boxels.ContainsKey(Position.GetHashCode()))
				return this.Boxels[Position.GetHashCode()];
			return null;
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

	public static class BoxelHelpers
	{
		[Flags]
		public enum Side
		{
			None = 1 << 0,
			PosX = 1 << 1,
			NegX = 1 << 2,
			PosY = 1 << 3,
			NegY = 1 << 4,
			PosZ = 1 << 5,
			NegZ = 1 << 6,
			All = PosX | NegX | PosY | NegY | PosZ | NegZ
		}

		public static int NumberOfSides(Side Sides)
		{
			var Result = 0;
			while (Sides != 0)
			{
				Sides &= Sides - 1;
				Result++;
			}
			return Result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Boxels"></param>
		/// <param name="Container"></param>
		/// <returns></returns>
		[Timer]
		public static IEnumerable<IBoxel> OcclusionCull(IEnumerable<IBoxel> Boxels)
		{
			foreach (var Boxel in Boxels)
			{
				if (Boxel.Container.AtOrDefault(Boxel.Position + Int3.UnitX) != null)
				{
					yield return Boxel;
				}
			}
		}

		[Timer]
		public static IEnumerable<Tuple<IBoxel, BoxelHelpers.Side>> SideOcclusionCull(IEnumerable<IBoxel> Boxels)
		{
			foreach (var Boxel in Boxels)
			{
				var Sides = BoxelHelpers.Side.None;
				var Container = Boxel.Container;
				if (Container.AtOrDefault(Boxel.Position + Int3.UnitX) == null)
					Sides |= Side.PosX;
				if (Container.AtOrDefault(Boxel.Position - Int3.UnitX) == null)
					Sides |= Side.NegX;
				if (Container.AtOrDefault(Boxel.Position + Int3.UnitY) == null)
					Sides |= Side.PosY;
				if (Container.AtOrDefault(Boxel.Position - Int3.UnitY) == null)
					Sides |= Side.NegY;
				if (Container.AtOrDefault(Boxel.Position + Int3.UnitZ) == null)
					Sides |= Side.PosZ;
				if (Container.AtOrDefault(Boxel.Position - Int3.UnitZ) == null)
					Sides |= Side.NegZ;
				yield return new Tuple<IBoxel, Side>(Boxel, Sides);
			}
		}
	}
}
