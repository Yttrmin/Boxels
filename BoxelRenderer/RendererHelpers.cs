﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using BoxelCommon;
using System.Runtime.InteropServices;

namespace BoxelRenderer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public readonly Vector3 Position;
        public readonly Vector3 TextureCoordinates;
        public static readonly int SizeInBytes;

        static Vertex()
        {
            SizeInBytes = Vector3.SizeInBytes * 2;
        }

        public Vertex(Vector3 Position, Vector3 TextureCoordinates)
        {
            this.Position = Position;
            this.TextureCoordinates = TextureCoordinates;
        }

        public Vertex Offset(Vector3 Offset, Vector3 TextureOffset)
        {
            return new Vertex(this.Position + Offset, this.TextureCoordinates + TextureOffset);
        }
    }

    public struct Face
    {
        public readonly Vertex[] Vertices;
        public const int DrawnVertexCount = 6;

        public Face(params Vertex[] Vertices)
        {
            if (Vertices.Length != 4)
            {
                throw new ArgumentException(String.Format("Vertices has {0} elements, Faces should have 4!", Vertices.Length));
            }
            this.Vertices = Vertices;
        }

        public Face Offset(Vector3 Offset, Vector3 TextureOffset)
        {
            var NewVertices = new Vertex[this.Vertices.Length];
            for (var i = 0; i < NewVertices.Length; i++)
            {
                NewVertices[i] = this.Vertices[i].Offset(Offset, TextureOffset);
            }
            return new Face(NewVertices);
        }

        public void Write(ref IntPtr Pointer)
        {
            Pointer = Utilities.Write<Vertex>(Pointer, this.Vertices, 0, 3);
            Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref this.Vertices[2]);
            Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref this.Vertices[1]);
            Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref this.Vertices[3]);
        }
    }

    public struct SmartCube
    {
        private static IDictionary<BoxelHelpers.Side, Face> FaceTemplates;
        private readonly IList<Face> Faces;
        public const int MaxDrawnVertexCount = Face.DrawnVertexCount * 6;

        static SmartCube()
        {
            FaceTemplates = new Dictionary<BoxelHelpers.Side, Face>(6);
            // Works.
            FaceTemplates[BoxelHelpers.Side.NegX] = new Face(
                new Vertex(new Vector3(-1, 1, 1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(-1, 1, -1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, -1, 1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(-1, -1, -1), new Vector3(1, 1, 0)));
            // Works.
            FaceTemplates[BoxelHelpers.Side.PosX] = new Face(
            new Vertex(new Vector3(1, 1, -1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, 1, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(1, -1, -1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, -1, 1), new Vector3(1, 1, 0)));
            // Works.
            FaceTemplates[BoxelHelpers.Side.NegY] = new Face(
                new Vertex(new Vector3(-1, -1, -1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, -1, -1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, -1, 1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, -1, 1), new Vector3(1, 1, 0)));
            // Works.
            FaceTemplates[BoxelHelpers.Side.PosY] = new Face(
                new Vertex(new Vector3(-1, 1, 1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, 1, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, 1, -1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, 1, -1), new Vector3(1, 1, 0)));
            // Works.
            FaceTemplates[BoxelHelpers.Side.NegZ] = new Face(
                new Vertex(new Vector3(-1, 1, -1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, 1, -1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, -1, -1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, -1, -1), new Vector3(1, 1, 0)));
            FaceTemplates[BoxelHelpers.Side.PosZ] = new Face(
                new Vertex(new Vector3(-1, -1, 1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, -1, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, 1, 1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, 1, 1), new Vector3(1, 1, 0)));
        }

        public SmartCube(Vector3 Position, int BoxelSize, BoxelHelpers.Side Sides, int TextureIndex)
        {
            this.Faces = new List<Face>(6);
            var SideKey = BoxelHelpers.Side.None;
            var TextureOffset = new Vector3(0, 0, TextureIndex);
            //@TODO - Roll.
            if (Sides.HasFlag(BoxelHelpers.Side.NegX))
            {
                this.Faces.Add(FaceTemplates[BoxelHelpers.Side.NegX].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.NegX;
            }
            if (Sides.HasFlag(BoxelHelpers.Side.PosX))
            {
                this.Faces.Add(FaceTemplates[BoxelHelpers.Side.PosX].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.PosX;
            }
            if (Sides.HasFlag(BoxelHelpers.Side.NegY))
            {
                this.Faces.Add(FaceTemplates[BoxelHelpers.Side.NegY].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.NegY;
            }
            if (Sides.HasFlag(BoxelHelpers.Side.PosY))
            {
                this.Faces.Add(FaceTemplates[BoxelHelpers.Side.PosY].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.PosY;
            }
            if (Sides.HasFlag(BoxelHelpers.Side.NegZ))
            {
                this.Faces.Add(FaceTemplates[BoxelHelpers.Side.NegZ].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.NegZ;
            }
            if (Sides.HasFlag(BoxelHelpers.Side.PosZ))
            {
                this.Faces.Add(FaceTemplates[BoxelHelpers.Side.PosZ].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.PosZ;
            }
        }

        public int Write(ref IntPtr Pointer)
        {
            int i;
            for (i = 0; i < this.Faces.Count; i++)
            {
                this.Faces[i].Write(ref Pointer);
            }
            return i * (Vertex.SizeInBytes * 6);
        }
    }

    internal static class RendererHelpers
    {
        public static class InputLayout
        {
            public static void PositionInstanced(out InputElement[] Elements, out int VertexSizeInBytes)
            {
                Elements = new[]
				{
					new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
					new InputElement("POSITION", 1, Format.R32G32B32_Float, 0, 1, InputClassification.PerInstanceData, 1),
				};
                VertexSizeInBytes = Vector3.SizeInBytes * 2;
            }

            public static void Position(out InputElement[] Elements, out int VertexSizeInBytes)
            {
                Elements = new[]
				{
					new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
				};
                VertexSizeInBytes = Vector3.SizeInBytes;
            }

            public static void PositionTexcoord(out InputElement[] Elements, out int VertexSizeInBytes)
            {
                Elements = new[]
				{
					new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
					new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0, InputClassification.PerVertexData, 0),
				};
                VertexSizeInBytes = Vector3.SizeInBytes + Vector2.SizeInBytes;
            }
        }

        public static class VertexBuffer
        {
            public static void RawCube(out Buffer VertexBuffer, out VertexBufferBinding VertexBinding,
                                       out int VertexCount, SharpDX.Direct3D11.Device1 Device, int BoxelSize)
            {
                VertexCount = Cube.NonIndexedVertexCount;
                using (var Stream = new DataStream(VertexCount * Vector3.SizeInBytes, false, true))
                {
                    new Cube(Vector3.Zero, BoxelSize).WriteVertices(Stream);
                    VertexBuffer = new Buffer(Device, Stream, (int)Stream.Length, ResourceUsage.Immutable,
                                              BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                    VertexBinding = new VertexBufferBinding(VertexBuffer, 12, 0);
                }
            }

            public static void NonIndexedCube(out Buffer VertexBuffer, out VertexBufferBinding VertexBinding,
                                       out int VertexCount, SharpDX.Direct3D11.Device1 Device, int BoxelSize)
            {
                VertexCount = Cube.NonIndexedVertexCount;
                using (var Stream = new DataStream(VertexCount * Vector3.SizeInBytes, false, true))
                {
                    new Cube(Vector3.Zero, BoxelSize).WriteNonIndexed(Stream);
                    VertexBuffer = new Buffer(Device, Stream, (int)Stream.Length, ResourceUsage.Immutable,
                                              BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                    VertexBinding = new VertexBufferBinding(VertexBuffer, 12, 0);
                }
            }
        }

        //@TODO - Rename me, annoying naming conflict.
        /// <summary>
        /// Helper class for rendering units as cubes (Minecraft style).
        /// </summary>
        [Obsolete]
        public struct Cube
        {
            public const int NonIndexedVertexCount = 36;
            public const int UniqueVertexCount = 24;

            private readonly Vector3[] Offsets;
            private readonly Vector3[] Vertices;
            private readonly int BoxelSize;
            private readonly Vector2[] TextureCoordinates;

            public Cube(Vector3 Position, int BoxelSize, int TextureIndex = -1, int TextureCount = -1)
                : this()
            {
                if (BoxelSize <= 0)
                    throw new ArgumentException(String.Format("BoxelSize must be a positive number, got {0} instead.", BoxelSize));
                this.BoxelSize = BoxelSize;
                ConstructOffsets(out this.Offsets);
                if (TextureIndex == -1)
                    this.TextureCoordinates = null;
                else
                {
                    if (TextureCount <= 0)
                        throw new ArgumentException(String.Format("TextureCount must be a positive number, got {0} instead.", TextureCount));
                    this.ConstructTextureCoordinates(out this.TextureCoordinates, TextureIndex, TextureCount);
                }
                this.Vertices = new Vector3[UniqueVertexCount];
                // All of this is powered by many hours of trial and error many months ago.
                // Don't ask questions.
                this.Vertices[0] = Position + Offsets[0];
                this.Vertices[1] = Position + Offsets[1];
                this.Vertices[2] = Position + Offsets[7];
                this.Vertices[3] = Position + Offsets[2];

                this.Vertices[4] = Position + Offsets[3];
                this.Vertices[5] = Position + Offsets[4];
                this.Vertices[6] = Position + Offsets[5];
                this.Vertices[7] = Position + Offsets[6];

                this.Vertices[8] = Position + Offsets[4];
                this.Vertices[9] = Position + Offsets[7];
                this.Vertices[10] = Position + Offsets[6];
                this.Vertices[11] = Position + Offsets[2];

                this.Vertices[12] = Position + Offsets[3];
                this.Vertices[13] = Position + Offsets[5];
                this.Vertices[14] = Position + Offsets[0];
                this.Vertices[15] = Position + Offsets[1];

                this.Vertices[16] = Position + Offsets[5];
                this.Vertices[17] = Position + Offsets[6];
                this.Vertices[18] = Position + Offsets[1];
                this.Vertices[19] = Position + Offsets[2];

                this.Vertices[20] = Position + Offsets[3];
                this.Vertices[21] = Position + Offsets[0];
                this.Vertices[22] = Position + Offsets[4];
                this.Vertices[23] = Position + Offsets[7];
            }

            public void WriteNonIndexed(DataStream Stream)
            {
                // All of this is powered by many hours of trial and error many months ago.
                // Don't ask questions.
                Stream.Write(this.Vertices[0]);
                Stream.Write(this.Vertices[1]);
                Stream.Write(this.Vertices[2]);

                Stream.Write(this.Vertices[2]);
                Stream.Write(this.Vertices[1]);
                Stream.Write(this.Vertices[3]);

                Stream.Write(this.Vertices[4]);
                Stream.Write(this.Vertices[5]);
                Stream.Write(this.Vertices[6]);

                Stream.Write(this.Vertices[6]);
                Stream.Write(this.Vertices[5]);
                Stream.Write(this.Vertices[7]);

                Stream.Write(this.Vertices[8]);
                Stream.Write(this.Vertices[9]);
                Stream.Write(this.Vertices[10]);

                Stream.Write(this.Vertices[10]);
                Stream.Write(this.Vertices[9]);
                Stream.Write(this.Vertices[11]);

                Stream.Write(this.Vertices[12]);
                Stream.Write(this.Vertices[13]);
                Stream.Write(this.Vertices[14]);

                Stream.Write(this.Vertices[14]);
                Stream.Write(this.Vertices[13]);
                Stream.Write(this.Vertices[15]);

                Stream.Write(this.Vertices[16]);
                Stream.Write(this.Vertices[17]);
                Stream.Write(this.Vertices[18]);

                Stream.Write(this.Vertices[18]);
                Stream.Write(this.Vertices[17]);
                Stream.Write(this.Vertices[19]);

                Stream.Write(this.Vertices[20]);
                Stream.Write(this.Vertices[21]);
                Stream.Write(this.Vertices[22]);

                Stream.Write(this.Vertices[22]);
                Stream.Write(this.Vertices[21]);
                Stream.Write(this.Vertices[23]);
            }

            public void WriteNonIndexedWithUVs(DataStream Stream)
            {
                Stream.Write(this.Vertices[0]);
                Stream.Write(this.TextureCoordinates[0]);
                Stream.Write(this.Vertices[1]);
                Stream.Write(this.TextureCoordinates[1]);
                Stream.Write(this.Vertices[2]);
                Stream.Write(this.TextureCoordinates[2]);

                Stream.Write(this.Vertices[2]);
                Stream.Write(this.TextureCoordinates[2]);
                Stream.Write(this.Vertices[1]);
                Stream.Write(this.TextureCoordinates[1]);
                Stream.Write(this.Vertices[3]);
                Stream.Write(this.TextureCoordinates[3]);

                Stream.Write(this.Vertices[4]);
                Stream.Write(this.TextureCoordinates[4]);
                Stream.Write(this.Vertices[5]);
                Stream.Write(this.TextureCoordinates[5]);
                Stream.Write(this.Vertices[6]);
                Stream.Write(this.TextureCoordinates[6]);

                Stream.Write(this.Vertices[6]);
                Stream.Write(this.TextureCoordinates[6]);
                Stream.Write(this.Vertices[5]);
                Stream.Write(this.TextureCoordinates[5]);
                Stream.Write(this.Vertices[7]);
                Stream.Write(this.TextureCoordinates[7]);

                Stream.Write(this.Vertices[8]);
                Stream.Write(this.TextureCoordinates[8]);
                Stream.Write(this.Vertices[9]);
                Stream.Write(this.TextureCoordinates[9]);
                Stream.Write(this.Vertices[10]);
                Stream.Write(this.TextureCoordinates[10]);

                Stream.Write(this.Vertices[10]);
                Stream.Write(this.TextureCoordinates[10]);
                Stream.Write(this.Vertices[9]);
                Stream.Write(this.TextureCoordinates[9]);
                Stream.Write(this.Vertices[11]);
                Stream.Write(this.TextureCoordinates[11]);

                Stream.Write(this.Vertices[12]);
                Stream.Write(this.TextureCoordinates[12]);
                Stream.Write(this.Vertices[13]);
                Stream.Write(this.TextureCoordinates[13]);
                Stream.Write(this.Vertices[14]);
                Stream.Write(this.TextureCoordinates[14]);

                Stream.Write(this.Vertices[14]);
                Stream.Write(this.TextureCoordinates[14]);
                Stream.Write(this.Vertices[13]);
                Stream.Write(this.TextureCoordinates[13]);
                Stream.Write(this.Vertices[15]);
                Stream.Write(this.TextureCoordinates[15]);

                Stream.Write(this.Vertices[16]);
                Stream.Write(this.TextureCoordinates[16]);
                Stream.Write(this.Vertices[17]);
                Stream.Write(this.TextureCoordinates[17]);
                Stream.Write(this.Vertices[18]);
                Stream.Write(this.TextureCoordinates[18]);

                Stream.Write(this.Vertices[18]);
                Stream.Write(this.TextureCoordinates[18]);
                Stream.Write(this.Vertices[17]);
                Stream.Write(this.TextureCoordinates[17]);
                Stream.Write(this.Vertices[19]);
                Stream.Write(this.TextureCoordinates[19]);

                Stream.Write(this.Vertices[20]);
                Stream.Write(this.TextureCoordinates[20]);
                Stream.Write(this.Vertices[21]);
                Stream.Write(this.TextureCoordinates[21]);
                Stream.Write(this.Vertices[22]);
                Stream.Write(this.TextureCoordinates[22]);

                Stream.Write(this.Vertices[22]);
                Stream.Write(this.TextureCoordinates[22]);
                Stream.Write(this.Vertices[21]);
                Stream.Write(this.TextureCoordinates[21]);
                Stream.Write(this.Vertices[23]);
                Stream.Write(this.TextureCoordinates[23]);
            }

            public void WriteNonIndexedWithTypedUVs(BaseRenderer Renderer, int Type, DataStream Stream)
            {
                var ZeroZero = new Vector3(0, 0, Type);
                var ZeroOne = new Vector3(0, 1, Type);
                var OneZero = new Vector3(1, 0, Type);
                var OneOne = new Vector3(1, 1, Type);

                var NegX = Renderer.GetTextureIndexByType(Axis.NegX, Type);
                var PosX = Renderer.GetTextureIndexByType(Axis.PosX, Type);
                var NegY = Renderer.GetTextureIndexByType(Axis.NegY, Type);
                var PosY = Renderer.GetTextureIndexByType(Axis.PosY, Type);
                var NegZ = Renderer.GetTextureIndexByType(Axis.NegZ, Type);
                var PosZ = Renderer.GetTextureIndexByType(Axis.PosZ, Type);


                // Side 1
                Stream.Write(this.Vertices[0]);
                Stream.Write(new Vector3(0, 1, PosX));
                Stream.Write(this.Vertices[1]);
                Stream.Write(new Vector3(1, 1, PosX));
                Stream.Write(this.Vertices[2]);
                Stream.Write(new Vector3(0, 0, PosX));

                Stream.Write(this.Vertices[2]);
                Stream.Write(new Vector3(0, 0, PosX));
                Stream.Write(this.Vertices[1]);
                Stream.Write(new Vector3(1, 1, PosX));
                Stream.Write(this.Vertices[3]);
                Stream.Write(new Vector3(1, 0, PosX));

                // Side 2
                Stream.Write(this.Vertices[4]);
                Stream.Write(new Vector3(0, 1, NegX));
                Stream.Write(this.Vertices[5]);
                Stream.Write(new Vector3(0, 0, NegX));
                Stream.Write(this.Vertices[6]);
                Stream.Write(new Vector3(1, 1, NegX));

                Stream.Write(this.Vertices[6]);
                Stream.Write(new Vector3(0, 1, NegX));
                Stream.Write(this.Vertices[5]);
                Stream.Write(new Vector3(1, 0, NegX));
                Stream.Write(this.Vertices[7]);
                Stream.Write(new Vector3(0, 0, NegX));

                // Side 3
                Stream.Write(this.Vertices[8]);
                Stream.Write(new Vector3(0, 0, PosY));
                Stream.Write(this.Vertices[9]);
                Stream.Write(new Vector3(1, 0, PosY));
                Stream.Write(this.Vertices[10]);
                Stream.Write(new Vector3(0, 1, PosY));

                Stream.Write(this.Vertices[10]);
                Stream.Write(new Vector3(0, 1, PosY));
                Stream.Write(this.Vertices[9]);
                Stream.Write(new Vector3(1, 0, PosY));
                Stream.Write(this.Vertices[11]);
                Stream.Write(new Vector3(1, 1, PosY));

                // Side 4
                Stream.Write(this.Vertices[12]);
                Stream.Write(new Vector3(0, 0, NegY));
                Stream.Write(this.Vertices[13]);
                Stream.Write(new Vector3(1, 0, NegY));
                Stream.Write(this.Vertices[14]);
                Stream.Write(new Vector3(0, 1, NegY));

                Stream.Write(this.Vertices[14]);
                Stream.Write(new Vector3(0, 1, NegY));
                Stream.Write(this.Vertices[13]);
                Stream.Write(new Vector3(1, 0, NegY));
                Stream.Write(this.Vertices[15]);
                Stream.Write(new Vector3(1, 1, NegY));

                // Side 5
                Stream.Write(this.Vertices[16]);
                Stream.Write(new Vector3(0, 1, PosZ));
                Stream.Write(this.Vertices[17]);
                Stream.Write(new Vector3(0, 0, PosZ));
                Stream.Write(this.Vertices[18]);
                Stream.Write(new Vector3(1, 1, PosZ));

                Stream.Write(this.Vertices[18]);
                Stream.Write(new Vector3(0, 1, PosZ));
                Stream.Write(this.Vertices[17]);
                Stream.Write(new Vector3(1, 0, PosZ));
                Stream.Write(this.Vertices[19]);
                Stream.Write(new Vector3(0, 0, PosZ));

                // Side 6
                Stream.Write(this.Vertices[20]);
                Stream.Write(new Vector3(1, 1, NegZ));
                Stream.Write(this.Vertices[21]);
                Stream.Write(new Vector3(0, 1, NegZ));
                Stream.Write(this.Vertices[22]);
                Stream.Write(new Vector3(1, 0, NegZ));

                Stream.Write(this.Vertices[22]);
                Stream.Write(new Vector3(0, 0, NegZ));
                Stream.Write(this.Vertices[21]);
                Stream.Write(new Vector3(1, 1, NegZ));
                Stream.Write(this.Vertices[23]);
                Stream.Write(new Vector3(0, 0, NegZ));
            }

            /// <summary>
            /// Writes out UniqueVertexCount vertices in sequential order. Will not render properly by itself.
            /// </summary>
            public void WriteVertices(DataStream Stream)
            {
                for (var i = 0; i < this.Vertices.Length; i++)
                {
                    Stream.Write(this.Vertices[i]);
                }
            }

            public static void WriteIndexed(DataStream Stream)
            {
                throw new NotImplementedException();
            }

            private void ConstructOffsets(out Vector3[] Offsets)
            {
                int CubeOffset = BoxelSize / 2;
                Offsets = new Vector3[8];
                // Offset from center vertex for cube vertices.
                Offsets[0] = new Vector3(-CubeOffset, -CubeOffset, CubeOffset);
                Offsets[1] = new Vector3(CubeOffset, -CubeOffset, CubeOffset);
                Offsets[2] = new Vector3(CubeOffset, CubeOffset, CubeOffset);
                Offsets[3] = new Vector3(-CubeOffset, -CubeOffset, -CubeOffset);
                Offsets[4] = new Vector3(-CubeOffset, CubeOffset, -CubeOffset);
                Offsets[5] = new Vector3(CubeOffset, -CubeOffset, -CubeOffset);
                Offsets[6] = new Vector3(CubeOffset, CubeOffset, -CubeOffset);
                Offsets[7] = new Vector3(-CubeOffset, CubeOffset, CubeOffset);
            }

            private void ConstructTextureCoordinates(out Vector2[] Coordinates, int Index, int TextureCount)
            {
                Coordinates = new Vector2[UniqueVertexCount];
                float IndexOverCount = ((float)Index) / TextureCount;
                float IndexPlusOneOverCount = ((float)(Index + 1)) / TextureCount;

                // All of this is powered by many hours of trial and error many months ago.
                // Don't ask questions.
                Coordinates[0] = new Vector2(0.125f, IndexPlusOneOverCount);
                Coordinates[1] = new Vector2(0, IndexPlusOneOverCount);
                Coordinates[2] = new Vector2(0.125f, IndexOverCount);
                Coordinates[3] = new Vector2(0, IndexOverCount);

                Coordinates[4] = new Vector2(0.125f, IndexPlusOneOverCount);
                Coordinates[5] = new Vector2(0.125f, IndexOverCount);
                Coordinates[6] = new Vector2(0.25f, IndexPlusOneOverCount);
                Coordinates[7] = new Vector2(0.25f, IndexOverCount);

                Coordinates[8] = new Vector2(0.375f, IndexPlusOneOverCount);
                Coordinates[9] = new Vector2(0.25f, IndexPlusOneOverCount);
                Coordinates[10] = new Vector2(0.375f, IndexOverCount);
                Coordinates[11] = new Vector2(0.25f, IndexOverCount);

                Coordinates[12] = new Vector2(0.5f, IndexPlusOneOverCount);
                Coordinates[13] = new Vector2(0.375f, IndexPlusOneOverCount);
                Coordinates[14] = new Vector2(0.5f, IndexOverCount);
                Coordinates[15] = new Vector2(0.375f, IndexOverCount);

                Coordinates[16] = new Vector2(0.5f, IndexPlusOneOverCount);
                Coordinates[17] = new Vector2(0.5f, IndexOverCount);
                Coordinates[18] = new Vector2(0.625f, IndexPlusOneOverCount);
                Coordinates[19] = new Vector2(0.625f, IndexOverCount);

                Coordinates[20] = new Vector2(0.75f, IndexPlusOneOverCount);
                Coordinates[21] = new Vector2(0.625f, IndexPlusOneOverCount);
                Coordinates[22] = new Vector2(0.75f, IndexOverCount);
                Coordinates[23] = new Vector2(0.625f, IndexOverCount);
            }
        }
    }
}