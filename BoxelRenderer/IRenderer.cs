using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX.Direct3D11;

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
