using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelCommon;
using BoxelLib;
using BoxelRenderer;
using SharpDX.Windows;

namespace ProjectBoxelGame
{
    sealed class Game : GameBase, ITickable
    {
        private BoxelManager Manager;
        private RenderForm Window;
        private RenderDevice RenderDevice;

        public Game(VBL.vbl Level)
        {
            this.RegisterTick(this);
            this.Window = new RenderForm("Project Boxel (Open PV Editor)");
            this.Manager = new BoxelManager(new BoxelManager.BoxelManagerSettings()
                {
                    Width = Level.properties.width,
                    Length = Level.properties.depth,
                    Height = Level.properties.depth
                }, RenderDevice);
            foreach (var Voxel in Level.voxels)
            {
                
            }
        }

        public void Tick(double DeltaTime)
        {
            
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
