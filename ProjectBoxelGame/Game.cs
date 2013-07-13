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
    sealed class Game : GameBase, ITickable, IDisposable
    {
        private readonly BoxelManager Manager;
        private readonly RenderForm Window;
        private readonly RenderDevice RenderDevice;
        private readonly ICamera Camera;
        private readonly Input Input;
        private bool MouseEnabled;
        private bool Resized;
        private const int Width = 1280;
        private const int Height = 1024;

		private Game()
		{
			this.RegisterTick(this);
			this.Window = new RenderForm("Project Boxel (Open PV Editor)");
			this.Window.FormBorderStyle = FormBorderStyle.Sizable;
			this.Window.MaximizeBox = true;
			this.Window.ClientSize = new System.Drawing.Size(Width, Height);
			this.Window.UserResized += (a, b) => { this.Resized = true; };
			this.Camera = new BasicCamera(new Vector3(0, 10, 0), new Vector3(1, 0, 0), Width, Height);
			this.RenderDevice = new RenderDevice(this.Window);
		}

        public Game(VBL.vbl Level) : this()
        {
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
			Trace.WriteLine("Writing map data out to binary. Use this instead of VBL!");
			this.Manager.Save("level_out.bin");
            this.Window.Show();
            this.MouseEnabled = true;
            this.Input = new Input(this.Window);
        }

		public Game(IBoxelContainer Container) : this()
		{
			this.Manager = new BoxelManager(Container, this.RenderDevice, new PVTypeManager());
			this.Window.Show();
			this.MouseEnabled = true;
			this.Input = new Input(this.Window);
		}

        public void Tick(double DeltaTime)
        {
            this.RenderDevice.Profiler.StartFrame(DeltaTime);
            if (this.Input.IsDown(Keys.Escape))
            {
                this.Window.Close();
                return;
            }
            if (this.Resized)
            {
                var Width = this.Window.ClientSize.Width;
                var Height = this.Window.ClientSize.Height;
                this.RenderDevice.Resize(Width, Height);
                this.Camera.SetDimensions(Width, Height);
                this.Resized = false;
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
            if (this.Input.WasPressed(Keys.ControlKey))
                this.MouseEnabled = !this.MouseEnabled;
            if (this.Input.WasPressed(Keys.F2))
                this.RenderDevice.SetFullscreen();
            if (this.MouseEnabled)
            {
                this.Camera.TurnRight(this.Input.DeltaX);
                this.Camera.TurnUp(this.Input.DeltaY);
            }
            this.Input.ResetKeyPresses();
            this.Camera.Tick(DeltaTime);
            this.Manager.Render(this.Camera);
            this.Input.ResetMouse(this.MouseEnabled);
        }

        /// <summary>
        /// Starts game execution.
        /// </summary>
        public void Run()
        {
            RenderLoop.Run(Window, this.OnMessagePump);
        }

        private void Dispose(bool Disposing)
        {
            Trace.WriteLine("Disposing Game...");
            if (Disposing)
            {
                this.RenderDevice.Dispose();
                this.Window.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        ~Game()
        {
            this.Dispose(false);
        }
    }
}
