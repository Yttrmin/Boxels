using BoxelCommon;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;
using VisibleBoxel = BoxelCommon.BoxelHelpers.VisibleBoxel;
using Side = BoxelCommon.BoxelHelpers.Side;

namespace BoxelRenderer
{
    public sealed class TiledPlaneRenderer : BaseRenderer
    {
        private Buffer BoxelTextureLookup;
        private const int BoxelSize = 2;

        public TiledPlaneRenderer(RenderDevice Device, BoxelTypes<ICubeBoxelType> Types)
            : base("TiledPlaneShaders.hlsl", "VShaderTextured", null, "PShaderTextured", 
                PrimitiveTopology.TriangleList, Device, Types)
        {
            throw new NotImplementedException();
        }

        protected override void GenerateBuffers(IEnumerable<IBoxel> Boxels, SharpDX.Direct3D11.Device1 Device, out Buffer VertexBuffer,
            out VertexBufferBinding Binding, out int VertexCount, out Buffer IndexBuffer, out Buffer InstanceBuffer,
            out VertexBufferBinding InstanceBinding, out int InstanceCount, int VertexSizeInBytes)
        {
            throw new NotImplementedException();
        }

        private void CreatePlanarOutline(IEnumerable<IBoxel> Boxels, ref IntPtr BufferPointer)
        {
            var BoxelPlaneMap = new Dictionary<Tuple<int, Side>, RectangleReference>();
            var Grid = new Grid3D<VisibleBoxel>();
            foreach(var VisibleBoxel in BoxelHelpers.SideOcclusionCull(Boxels))
            {
                Grid.Add(VisibleBoxel.Boxel.Position.GetHashCode(), VisibleBoxel);
            }

            var Checked = new HashSet<VisibleBoxel>();
            foreach(var VisibleBoxel in Grid.AllItems)
            {
                foreach (var Side in BoxelHelpers.AllSides(VisibleBoxel.VisibleSides))
                {
                    // Foreach visible side, check if there's any planes already to the
                    // left, right, top, and bottom. If so, grow the plane to include it.
                    // Else, create a new plane.
                    // Inefficient, but simple.
                    switch(Side)
                    {
                        case Side.PosX:
                            var OurKey = new Tuple<int, Side>(VisibleBoxel.Position.ToInt(), Side.PosX); 
                            var LeftKey = new Tuple<int, Side>((VisibleBoxel.Position - Int3.UnitZ).ToInt(), Side.PosX);
                            var RightKey = new Tuple<int, Side>((VisibleBoxel.Position + Int3.UnitZ).ToInt(), Side.PosX);
                            RectangleReference Left, Right;
                            BoxelPlaneMap.TryGetValue(LeftKey, out Left);
                            BoxelPlaneMap.TryGetValue(RightKey, out Right);
                            if(Left != null && Right == null)
                            {
                                Left.Rectangle.Right += BoxelSize;
                                BoxelPlaneMap[OurKey] = Left;
                            }
                            else if(Right != null && Left == null)
                            {
                                Right.Rectangle.Left -= BoxelSize;
                                BoxelPlaneMap[OurKey] = Right;
                            }
                            else if(Right != null && Left != null)
                            {
                                throw new NotImplementedException();
                            }
                            else if(Right == null && Left == null)
                            {
                                BoxelPlaneMap[OurKey] = new RectangleReference(
                                    new Rectangle(VisibleBoxel.Position.X - BoxelSize/2, 
                                        VisibleBoxel.Position.Y - BoxelSize/2, BoxelSize, BoxelSize));
                            }
                            break;
                    }
                }
                Checked.Add(VisibleBoxel);
            }
        }

        protected override void PreRender(DeviceContext1 Context)
        {
            throw new NotImplementedException();
        }

        protected override void SetupInputElements(out InputElement[] Elements, out int VertexSizeInBytes)
        {
            throw new NotImplementedException();
            Elements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                    new InputElement("TEXCOORD", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0),
                };
            VertexSizeInBytes = Vector3.SizeInBytes * 2;
        }

        private sealed class BoxelRectangle
        {
            public VisibleBoxel Boxel { get; private set; }
            public RectangleReference Rectangle { get; private set; }
        }

        private sealed class RectangleReference
        {
            public Rectangle Rectangle;

            public RectangleReference(Rectangle Rectangle)
            {
                this.Rectangle = Rectangle;
            }
        }
    }
}
