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
        Vector3 Position { get; }
        Vector3 LookDirection { get; }
    }

    public class BasicCamera : ICamera
    {
        public Vector3 Position { get; private set; }
        public Vector3 LookDirection { get; private set; }

        public BasicCamera(Vector3 Position, Vector3 LookDirection)
        {
            this.Position = Position;
            this.LookDirection = LookDirection;
        }
    }

    public interface ILocalPlayer
    {
        Vector3 Position { get; }
        ICamera Camera { get; }
    }
}
