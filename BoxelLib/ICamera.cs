using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace BoxelLib
{
    public interface ICamera
    {
        Matrix Projection { get; }
        Matrix View { get; }
        Vector3 Position { get; }
        Vector3 LookDirection { get; }
    }
}
