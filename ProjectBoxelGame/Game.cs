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

namespace ProjectBoxelGame
{
    sealed class Game : GameBase, ITickable
    {
        private readonly BoxelManager Manager;
        private readonly RenderForm Window;
        private readonly RenderDevice RenderDevice;
        private readonly ICamera Camera;

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
            this.Window.MouseMove += this.OnMouseMove;
            this.Window.KeyDown += this.OnKeyDown;
        }

        public void Tick(double DeltaTime)
        {
            //Trace.WriteLine(String.Format("DT: {0}", DeltaTime));
            this.Camera.Tick(DeltaTime);
            this.Manager.Render(this.Camera);
        }

        /// <summary>
        /// Starts game execution.
        /// </summary>
        public void Run()
        {
            RenderLoop.Run(Window, this.OnMessagePump);
        }

        private void OnMouseMove(Object Sender, MouseEventArgs Args)
        {

        }

        private void OnKeyDown(Object Sender, KeyEventArgs Args)
        {
            switch(Args.KeyCode)
            {
                case Keys.W:
                    this.Camera.MoveForward(1);
                    break;
                case Keys.S:
                    this.Camera.MoveForward(-1);
                    break;
                case Keys.A:
                    this.Camera.MoveRight(-1);
                    break;
                case Keys.D:
                    this.Camera.MoveRight(1);
                    break;
            }
        }
    }
}
