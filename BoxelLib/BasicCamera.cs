using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace BoxelLib
{
    public class BasicCamera : ICamera
    {
        public Vector3 Position { get; private set; }
        public Vector3 LookDirection { get; private set; }
        public Matrix Projection { get; private set; }
        private float Roll, Pitch, Yaw;
        private float NextMoveRight, NextMoveForward;
        private readonly Vector3 DefaultForward, DefaultRight;
        private Vector3 Up;
        public Matrix View
        {
            get
            {
                return Matrix.LookAtLH(Position, new Vector3(10000, -5000, 10000), new Vector3(0, 1, 0));
            }
        }

        public BasicCamera(Vector3 Position, Vector3 LookDirection)
        {
            this.Position = Position;
            this.LookDirection = LookDirection;
            this.Projection = Matrix.PerspectiveFovLH((float)(90.0f * (Math.PI / 180.0f)), 1.25f, 1.0f, 2000.0f);
            this.DefaultForward = new Vector3(0, 0, 1);
            this.DefaultRight = new Vector3(1, 0, 0);
            this.Up = new Vector3(0, 1, 0);
        }

        public void Tick(double DeltaTime)
        {
            Position = new Vector3((float) (this.Position.X + (0.5*DeltaTime)), Position.Y, (float) (this.Position.Z + (0.5*DeltaTime)));
        }

        public void MoveRight(float Amount)
        {
            NextMoveRight += Amount;
        }

        public void MoveForward(float Amount)
        {
            NextMoveForward += Amount;
        }

        private Matrix CalculateViewMatrix()
        {
            var CamRotation = Matrix.RotationYawPitchRoll(Yaw, Pitch, Roll);
            var CamTarget = Vector3.TransformCoordinate(this.DefaultForward, CamRotation);
            Vector3.Normalize(ref CamTarget, out CamTarget);

            var CamYRotation = Matrix.RotationY(this.Pitch * (float)(Math.PI / 180.0f));
            var CamRight = Vector3.TransformCoordinate(this.DefaultRight, CamRotation);
            Vector3.TransformCoordinate(ref this.Up, ref CamRotation, out this.Up);
            var CamForward = Vector3.TransformCoordinate(this.DefaultForward, CamRotation);

            this.Position += CamRight * this.NextMoveRight;
            this.Position += CamForward * this.NextMoveForward;

            this.NextMoveForward = 0;
            this.NextMoveRight = 0;

            this.Position = CamTarget + this.Position;

            return Matrix.LookAtLH(this.Position, CamTarget, this.Up);
        }
    }
}
