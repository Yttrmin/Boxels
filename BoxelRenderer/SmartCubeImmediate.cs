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
    public struct SmartCubeImmediate
    {
        private static IDictionary<BoxelHelpers.Side, FaceImmediate> FaceTemplates;
        private static IList<FaceImmediate> Faces;
        //private readonly IList<Face> Faces;
        public const int MaxDrawnVertexCount = FaceImmediate.DrawnVertexCount * 6;

        static SmartCubeImmediate()
        {
            Faces = new List<FaceImmediate>(6);
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

        public SmartCubeImmediate(Vector3 Position, int BoxelSize, BoxelHelpers.Side Sides, BaseRenderer Renderer, int TextureIndex)
        {
            //this.Faces = new List<Face>(6);
            var SideKey = BoxelHelpers.Side.None;
            var TextureOffset = new Vector3(0, 0, -1);
            //@TODO - Roll.
            if ((Sides & BoxelHelpers.Side.NegX) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.NegX, TextureIndex);
                Faces.Add(FaceTemplates[BoxelHelpers.Side.NegX].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.NegX;
            }
            if ((Sides & BoxelHelpers.Side.PosX) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.PosX, TextureIndex);
                Faces.Add(FaceTemplates[BoxelHelpers.Side.PosX].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.PosX;
            }
            if ((Sides & BoxelHelpers.Side.NegY) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.NegY, TextureIndex);
                Faces.Add(FaceTemplates[BoxelHelpers.Side.NegY].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.NegY;
            }
            if ((Sides & BoxelHelpers.Side.PosY) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.PosY, TextureIndex);
                Faces.Add(FaceTemplates[BoxelHelpers.Side.PosY].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.PosY;
            }
            if ((Sides & BoxelHelpers.Side.NegZ) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.NegZ, TextureIndex);
                Faces.Add(FaceTemplates[BoxelHelpers.Side.NegZ].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.NegZ;
            }
            if ((Sides & BoxelHelpers.Side.PosZ) != 0)
            {
                TextureOffset.Z = Renderer.GetTextureIndexByType(Axis.PosZ, TextureIndex);
                Faces.Add(FaceTemplates[BoxelHelpers.Side.PosZ].Offset(Position, TextureOffset));
                SideKey |= BoxelHelpers.Side.PosZ;
            }
        }

        public int Write(ref IntPtr Pointer)
        {
            int i;
            for (i = 0; i < Faces.Count; i++)
            {
                Faces[i].Write(ref Pointer);
            }
            Faces.Clear();
            return i * (Vertex.SizeInBytes * 6);
        }
    }

    public struct FaceImmediate
    {
        private static Vertex[][] NewVertices;
        private static int Index;
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

        public FaceImmediate Offset(Vector3 Offset, Vector3 TextureOffset)
        {
            //var NewVertices = new Vertex[this.Vertices.Length];
            for (var i = 0; i < NewVertices[Index].Length; i++)
            {
                NewVertices[Index][i] = this.Vertices[i].Offset(Offset, TextureOffset);
            }
            return new FaceImmediate(NewVertices[Index++]);
        }

        public void Write(ref IntPtr Pointer)
        {
            Pointer = Utilities.Write<Vertex>(Pointer, this.Vertices, 0, 3);
            Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref this.Vertices[2]);
            Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref this.Vertices[1]);
            Pointer = Utilities.WriteAndPosition<Vertex>(Pointer, ref this.Vertices[3]);
            Index = 0;
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
