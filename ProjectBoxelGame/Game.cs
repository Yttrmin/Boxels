using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BoxelCommon;
using BoxelLib;
using BoxelRenderer;
using SharpDX;
using SharpDX.Windows;
using System.Drawing;
using SharpDX.RawInput;
using SharpDX.Multimedia;

namespace ProjectBoxelGame
{
    sealed class Game : GameBase, ITickable
    {
        private readonly BoxelManager Manager;
        private readonly RenderForm Window;
        private readonly RenderDevice RenderDevice;
        private readonly ICamera Camera;
        private readonly Input Input;

        public Game(VBL.vbl Level)
        {
            this.RegisterTick(this);
            this.Camera = new BasicCamera(new Vector3(0, 10, 0), new Vector3(1, 0, 0));
            this.Window = new RenderForm("Project Boxel (Open PV Editor)");
            this.Window.FormBorderStyle = FormBorderStyle.Fixed3D;
            this.Window.MaximizeBox = false;
            this.Window.ClientSize = new System.Drawing.Size(1280, 1024);
            this.RenderDevice = new RenderDevice(this.Window);
            this.Manager = new BoxelManager(new BoxelManager.BoxelManagerSettings()
                {
                    Width = Level.properties.width,
                    Length = Level.properties.depth,
                    Height = Level.properties.depth
                }, RenderDevice, new PVTypeManager());
            foreach (var Voxel in Level.voxels)
            {
                this.Manager.Add(new BasicBoxel(new Int3(Voxel.x, Voxel.y, Voxel.z), 1.0f, Voxel.id), 
                    new Int3(Voxel.x, Voxel.y, Voxel.z));
            }
            this.Window.Show();
            this.Input = new Input(this.Window);
        }

        public void Tick(double DeltaTime)
        {
            if (this.Input.IsDown(Keys.Escape))
            {
                this.Window.Close();
                return;
            }
            var Magnitude = 10;
            Magnitude *= this.Input.IsDown(Keys.ShiftKey) ? 10 : 1;
            //Trace.WriteLine(String.Format("DT: {0}", DeltaTime));
            if (this.Input.IsDown(Keys.W))
                this.Camera.MoveForward(Magnitude * (float)DeltaTime);
            if (this.Input.IsDown(Keys.A))
                this.Camera.MoveRight(-Magnitude * (float)DeltaTime);
            if (this.Input.IsDown(Keys.S))
                this.Camera.MoveForward(-Magnitude * (float)DeltaTime);
            if (this.Input.IsDown(Keys.D))
                this.Camera.MoveRight(Magnitude * (float)DeltaTime);
            this.Camera.TurnRight(this.Input.DeltaX);
            this.Camera.TurnUp(this.Input.DeltaY);
            this.Camera.Tick(DeltaTime);
            this.Manager.Render(this.Camera);
            this.Input.ResetMouse();
        }

        /// <summary>
        /// Starts game execution.
        /// </summary>
        public void Run()
        {
            RenderLoop.Run(Window, this.OnMessagePump);
        }
    }
}
