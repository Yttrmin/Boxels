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
        private bool FixedTimestep;
        private double TimeStep;

        protected GameBase()
        {
            this.GameTimer = new Stopwatch();
        }

        protected void EnableFixedTimestep(double FixedTimestep)
        {
            this.FixedTimestep = true;
            this.TimeStep = FixedTimestep;
        }

        protected void DisableFixedTimestep()
        {
            this.FixedTimestep = false;
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
            double DeltaTime;
            if (FixedTimestep)
            {
                DeltaTime = TimeStep;
            }
            else
            {
                DeltaTime = ((double)GameTimer.ElapsedTicks / (double)Stopwatch.Frequency);
                GameTimer.Restart();
            }
            if (this.ToTick != null)
                this.ToTick(DeltaTime);
        }
    }
}
