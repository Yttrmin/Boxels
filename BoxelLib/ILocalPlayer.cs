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

    public class BasicCamera : ICamera
    {
        public Vector3 Position { get; private set; }
        public Vector3 LookDirection { get; private set; }
        public Matrix Projection { get; private set; }
        public Matrix View { get
        {
            Position = new Vector3(Position.X + 0.01f, Position.Y, Position.Z + 0.01f);
            return Matrix.LookAtLH(Position, new Vector3(1000000, 0, 0), new Vector3(0, 1, 0));
        }
        }

        public BasicCamera(Vector3 Position, Vector3 LookDirection)
        {
            this.Position = Position;
            this.LookDirection = LookDirection;
            this.Projection = Matrix.PerspectiveFovLH((float) (90.0f * (Math.PI/180.0f)), 1.333333f, 1.0f, 500.0f);
        }

        private Matrix CalculateViewMatrix()
        {
            var Up = new Vector3(0,1,0);
            var Right = new Vector3(1,0,0);
            var Look = new Vector3(0,0,1);
            var NewLook = Look;
            var NewRight = Right;
            var NewUp = Up;
            Matrix Yaw;
            Yaw = Matrix.RotationAxis(Up, 0);
            NewLook = Vector3.TransformCoordinate(Look, Yaw);
            NewRight = Vector3.TransformCoordinate(Right, Yaw);
            Matrix Pitch;
            Pitch = Matrix.RotationAxis(Right, 0);
            NewLook = Vector3.TransformCoordinate(NewLook, Pitch);
            NewUp = Vector3.TransformCoordinate(NewUp, Pitch);

            var ViewMatrix = new Matrix();
            ViewMatrix.M11 = NewRight.X; ViewMatrix.M12 = NewUp.X; ViewMatrix.M13 = NewLook.X;
            ViewMatrix.M21 = NewRight.Y; ViewMatrix.M22 = NewUp.Y; ViewMatrix.M23 = NewLook.Y;
            ViewMatrix.M31 = NewRight.Z; ViewMatrix.M32 = NewUp.Z; ViewMatrix.M33 = NewLook.Z;

            ViewMatrix.M41 = -Vector3.Dot(Position, NewRight);
            ViewMatrix.M42 = -Vector3.Dot(Position, NewUp);
            ViewMatrix.M43 = -Vector3.Dot(Position, NewLook);
            ViewMatrix.M44 = 1.0f;
            return ViewMatrix;
        }
    }

    public interface ILocalPlayer
    {
        Vector3 Position { get; }
        ICamera Camera { get; }
    }
}
