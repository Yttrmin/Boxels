using BoxelCommon;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BoxelRenderer
{
    public static class SmartCubeImmediate
    {
        private static IDictionary<BoxelHelpers.Side, FaceImmediate> FaceTemplates;
        private static Vector3 TextureOffset;
        public const int MaxDrawnVertexCount = FaceImmediate.DrawnVertexCount * 6;

        static SmartCubeImmediate()
        {
            TextureOffset = new Vector3(0, 0, -1);
            FaceTemplates = new Dictionary<BoxelHelpers.Side, FaceImmediate>(6);
            FaceTemplates[BoxelHelpers.Side.NegX] = new FaceImmediate(
                new Vertex(new Vector3(-1, 1, 1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(-1, 1, -1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, -1, 1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(-1, -1, -1), new Vector3(1, 1, 0)));
            FaceTemplates[BoxelHelpers.Side.PosX] = new FaceImmediate(
            new Vertex(new Vector3(1, 1, -1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, 1, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(1, -1, -1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, -1, 1), new Vector3(1, 1, 0)));
            FaceTemplates[BoxelHelpers.Side.NegY] = new FaceImmediate(
                new Vertex(new Vector3(-1, -1, -1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, -1, -1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, -1, 1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, -1, 1), new Vector3(1, 1, 0)));
            FaceTemplates[BoxelHelpers.Side.PosY] = new FaceImmediate(
                new Vertex(new Vector3(-1, 1, 1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, 1, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, 1, -1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, 1, -1), new Vector3(1, 1, 0)));
            FaceTemplates[BoxelHelpers.Side.NegZ] = new FaceImmediate(
                new Vertex(new Vector3(-1, 1, -1), new Vector3(0, 0, 0)),
                new Vertex(new Vector3(1, 1, -1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-1, -1, -1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(1, -1, -1), new Vector3(1, 1, 0)));
            FaceTemplates[BoxelHelpers.Side.PosZ] = new FaceImmediate(
                new Vertex(new Vector3(-1, -1, 1), new Vector3(1, 1, 0)),
                new Vertex(new Vector3(1, -1, 1), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(-1, 1, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(1, 1, 1), new Vector3(0, 0, 0)));
        }

        public static void SetCube(Vector3 Position, int BoxelSize, BoxelHelpers.Side Sides, BaseRenderer Renderer, int TextureIndex)
        {
            //@TODO - Roll.
            if ((Sides & BoxelHelpers.Side.NegX) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.NegX, TextureIndex);
                FaceTemplates[BoxelHelpers.Side.NegX].AddOffset(Position, TextureOffset);
            }
            if ((Sides & BoxelHelpers.Side.PosX) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.PosX, TextureIndex);
                FaceTemplates[BoxelHelpers.Side.PosX].AddOffset(Position, TextureOffset);
            }
            if ((Sides & BoxelHelpers.Side.NegY) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.NegY, TextureIndex);
                FaceTemplates[BoxelHelpers.Side.NegY].AddOffset(Position, TextureOffset);
            }
            if ((Sides & BoxelHelpers.Side.PosY) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.PosY, TextureIndex);
                FaceTemplates[BoxelHelpers.Side.PosY].AddOffset(Position, TextureOffset);
            }
            if ((Sides & BoxelHelpers.Side.NegZ) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.NegZ, TextureIndex);
                FaceTemplates[BoxelHelpers.Side.NegZ].AddOffset(Position, TextureOffset);
            }
            if ((Sides & BoxelHelpers.Side.PosZ) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.PosZ, TextureIndex);
                FaceTemplates[BoxelHelpers.Side.PosZ].AddOffset(Position, TextureOffset);
            }
        }

        public static int Write(ref IntPtr Pointer)
        {
            var Size = FaceImmediate.Index * (Vertex.SizeInBytes * 6);
            FaceImmediate.Write(ref Pointer);
            return Size;
        }

        private struct FaceImmediate
        {
            private static Vertex[][] NewVertices;
            public static int Index { get; private set; }
            public readonly Vertex[] Vertices;
            public const int DrawnVertexCount = 6;

            static FaceImmediate()
            {
                NewVertices = new Vertex[6][];
                for (var i = 0; i < NewVertices.Length; i++)
                {
                    NewVertices[i] = new Vertex[4];
                }
                Index = 0;
            }

            public FaceImmediate(params Vertex[] Vertices)
            {
                if (Vertices.Length != 4)
                {
                    throw new ArgumentException(String.Format("Vertices has {0} elements, Faces should have 4!", Vertices.Length));
                }
                this.Vertices = Vertices;
            }

            public void AddOffset(Vector3 Offset, Vector3 TextureOffset)
            {
                for (var i = 0; i < NewVertices[Index].Length; i++)
                {
                    NewVertices[Index][i] = this.Vertices[i].Offset(Offset, TextureOffset);
                }
                Index++;
            }

            public static void Write(ref IntPtr Pointer)
            {
                for (var i = 0; i < Index; i++)
                {
                    var WriteVertices = NewVertices[i];
                    Pointer = Utilities.Write<Vertex>(Pointer, WriteVertices, 0, 3);
                    Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref WriteVertices[2]);
                    Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref WriteVertices[1]);
                    Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref WriteVertices[3]);
                }
                Index = 0;
            }
        }
    }

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
}
