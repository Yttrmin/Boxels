﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device1 = SharpDX.Direct3D11.Device1;
using SharpDX.DXGI;
using Cube = BoxelRenderer.RendererHelpers.Cube;

namespace BoxelRenderer
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CubeNoGSRenderer : BaseRenderer
    {
        private const int BoxelSize = 2;
        private const int VerticesPerBoxel = 24;
        private const int EmittedVertices = 36;

        public CubeNoGSRenderer(Device1 Device)
            : base("PRShaders.hlsl", "VShaderTextured", null, "PShaderTextured", PrimitiveTopology.TriangleList, Device)
        {
            
        }

        protected override void PreRender(DeviceContext1 Context)
        {
            
        }

        protected override void GenerateBuffers(IEnumerable<IBoxel> Boxels, Device1 Device, out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            InstanceBuffer = null;
            InstanceBinding = new VertexBufferBinding();
            IndexBuffer = null;
            InstanceCount = 0;
            var Enumerable = Boxels as IBoxel[] ?? Boxels.ToArray();
            var Random = new Random();
            VertexCount = Enumerable.Length * EmittedVertices;
            using (var VertexStream = new DataStream(Enumerable.Length * 
                (Cube.NonIndexedVertexCount*Vector3.SizeInBytes + Cube.NonIndexedVertexCount*Vector2.SizeInBytes), false, true))
            {
                foreach (var Boxel in Enumerable)
                {
                    new Cube(new Vector3(Boxel.Position.X * BoxelSize,
                        Boxel.Position.Y * BoxelSize, Boxel.Position.Z * BoxelSize), BoxelSize, Random.Next(0, 7), 8).WriteNonIndexedWithUVs(VertexStream);
                }
                VertexBuffer = new Buffer(Device, VertexStream, (int)VertexStream.Length, ResourceUsage.Immutable,
                                               BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                VertexBuffer.DebugName = "BoxelsVertexBuffer";
                Binding = new VertexBufferBinding(VertexBuffer, Vector3.SizeInBytes+Vector2.SizeInBytes, 0);
            }
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            RendererHelpers.InputLayout.PositionTexcoord(out Elements, out VertexSizeInBytes);
        }
    }
}