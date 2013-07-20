using System.Collections.Generic;
using SharpDX.Direct3D11;
using Device1 = SharpDX.Direct3D11.Device1;
using System;
using BoxelCommon;

namespace BoxelRenderer
{
    public interface IRenderer
    {

    }
    
    public interface IBoxelRenderer : IRenderer, IDisposable
    {
        int ViewHash { get; }
        void SetView(IEnumerable<IBoxel> Boxels, int SphereHash, Device1 Device);
        void Render(DeviceContext1 Context);
    }
}
