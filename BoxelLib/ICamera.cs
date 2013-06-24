using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelCommon;
using SharpDX;

namespace BoxelLib
{
    public interface ICamera : ITickable
    {
        Matrix Projection { get; }
        Matrix View { get; }
        Vector3 Position { get; }
        Vector3 LookDirection { get; }

        void MoveRight(float Amount);
        void MoveForward(float Amount);
    }
}
