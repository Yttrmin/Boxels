using BoxelCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelGame
{
    public sealed class CPUProfiler : ICPUProfiler
    {
        private sealed class Event
        {
            private long StartTime;
            private bool InProgress;
            private long ElapsedTicks;
            private uint SampleCount;
            private Stopwatch Timer;
            public float AverageTime { get { return (float)(this.ElapsedTicks / this.SampleCount) / Stopwatch.Frequency * 1000.0f; } }

            public Event(Stopwatch Timer)
            {
                this.Timer = Timer;
            }

            public void Start()
            {
                if (InProgress)
                    throw new InvalidOperationException("Event already started.");
                this.StartTime = Timer.ElapsedTicks;
                this.InProgress = true;
            }

            public void Stop()
            {
                if (!InProgress)
                    throw new InvalidOperationException("Event was not started.");
                this.ElapsedTicks += Timer.ElapsedTicks - this.StartTime;
                this.SampleCount++;
            }

            public void Reset()
            {
                this.SampleCount = 0;
                this.ElapsedTicks = 0;
            }
        }

        private readonly IDictionary<string, Event> EventMap;
        private readonly Stopwatch Timer;
        private readonly StringBuilder Builder;
        private const string FrameTimeID = "Frame Time";

        public CPUProfiler()
        {
            this.EventMap = new Dictionary<string, Event>();
            this.Timer = new Stopwatch();
            this.Builder = new StringBuilder();
        }

        public void PrepareIDs(params string[] OrderedIDs)
        {
            throw new NotImplementedException();
        }

        public void BeginFrame()
        {
            this.Begin("Frame Time");
        }

        public void EndFrame()
        {
            this.End("Frame Time");
            this.Timer.Stop();
            if(this.Timer.ElapsedTicks / Stopwatch.Frequency >= 1.0f)
            {
                this.UpdateString();
            }
        }

        public void Begin(string ID)
        {
            this.GetEvent(ID).Start();
        }

        public void End(string ID)
        {
            this.GetEvent(ID).Stop();
        }

        public override string ToString()
        {
            return this.Builder.ToString();
        }

        private void UpdateString()
        {
            this.Builder.Clear();
            Builder.Append(String.Format("Frame Time: {0}ms", this.GetEvent(FrameTimeID).AverageTime));
            foreach(var Key in this.EventMap.Keys)
            {
                Builder.Append(String.Format("   {0}: {1}ms", Key, this.GetEvent(Key).AverageTime));
            }
        }

        private Event GetEvent(string ID)
        {
            Event Event;
            this.EventMap.TryGetValue(ID, out Event);
            if (Event == null)
                this.EventMap[ID] = Event = new Event(this.Timer);
            return Event;
        }
    }
}
