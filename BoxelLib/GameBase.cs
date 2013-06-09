using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelCommon;

namespace BoxelLib
{
    public abstract class GameBase
    {
        private readonly Stopwatch GameTimer;
        private event Action<double> ToTick;

        protected GameBase()
        {
            this.GameTimer = new Stopwatch();
        }

        public void RegisterTick(ITickable Tickable)
        {
            this.ToTick += Tickable.Tick;
        }

        public void UnregisterTick(ITickable Tickable)
        {
            this.ToTick -= Tickable.Tick;
        }

        public void OnMessagePump()
        {
            var DeltaTime = ((double)GameTimer.ElapsedTicks/Stopwatch.Frequency)*1000;
            if (this.ToTick != null)
                this.ToTick(DeltaTime);
            GameTimer.Restart();
        }
    }
}
