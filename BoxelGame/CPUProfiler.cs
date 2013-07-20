using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelGame
{
    public class CPUProfiler
    {
        private readonly Stopwatch Timer = new Stopwatch();

        public enum Event
        {
            StartFrame,
            PostTick,
            EndFrame
        }

        public void StartFrame(double DeltaTime)
        {
            throw new NotImplementedException();
        }

        public void RegisterEvent(Event Event)
        {

        }
    }
}
