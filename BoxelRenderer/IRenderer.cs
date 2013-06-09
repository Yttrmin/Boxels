using System.Collections.Generic;
using BoxelLib;
using SharpDX.Direct3D11;
using Device1 = SharpDX.Direct3D11.Device1;

namespace BoxelRenderer
{
    public interface IRenderer
    {

    }
    
    public interface IBoxelRenderer : IRenderer
    {
        int ViewHash { get; }
        void SetView(IEnumerable<IBoxel> Boxels, int SphereHash, Device1 Device);
        void Render(DeviceContext1 Context);
    }
}
