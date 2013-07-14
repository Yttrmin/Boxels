using System;
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
    }
}