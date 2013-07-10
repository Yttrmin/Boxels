using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelRenderer
{
    interface IGPUProfiler
    {

    }

    /// <summary>
    /// Profiler that performs no GPU profiling, just FPS and CPU delta time.
    /// </summary>
    public class FPSOnlyProfiler
    {

    }

    public class GPUProfiler : IDisposable
    {
        public enum TimeStamp
        {
            FrameDisjoint,
            BeginFrame,
            EndFrame, // Implicitly clear time from Present.
            FrameIdle,
            PerFrameBufferUpdate,
            DrawTerrain,
            Present,
            PipelineInformation,
            Draw2D,
        }
        public struct Stats
        {
            public float FrameTime;
            public float Present;
            public float IdleTime;
            public float PerFrameBufferTime;
            public float Terrain;
            public float Clear;
            public float Draw2DTime;
            public float FrameRate;
            public float CPUTime;
            public long PrimitivesSent;
            public long PrimitivesRendererd;

            public override string ToString()
            {
                return String.Format("GPU frame: {0}ms  Pre-Draw: {4}ms  PerFrameCBuffer: {5}ms  TerrainDraw: {1}ms  Draw2D: {8}ms  PresentTime: {2}ms  ClearTime: {3}ms | PrimsSent: {6}  PrimsRendered: {7}", 
                    FrameTime, Terrain, Present, Clear, IdleTime, PerFrameBufferTime, PrimitivesSent, PrimitivesRendererd, Draw2DTime);
            }
        }
        private Device1 Device;
        private DeviceContext1 Immediate;
        private IDictionary<TimeStamp, Query[]> Queries;
        private bool Waiting;
        private bool Disjoint;
        private Stats AverageStats, PreviousStats;
        private double TotalCPUDelta;
        private Stopwatch Timer;
        private int FrameCount;
        private long TotalFrameCount;
        private bool PendingCalculation;
        private int FreeQueryIndex;
        private int Stalls;
        private const bool TraceStats = true;

        public GPUProfiler(Device1 Device)
        {
            this.Device = Device;
            this.Immediate = this.Device.ImmediateContext1;
            this.Queries = new Dictionary<TimeStamp, Query[]>();
            this.Timer = new Stopwatch();
            var QueryDesc = new QueryDescription()
                {
                    Type = QueryType.TimestampDisjoint,
                    Flags=QueryFlags.None
                };
            this.Queries[TimeStamp.FrameDisjoint] = new Query[2];
            this.Queries[TimeStamp.FrameDisjoint][0] = new Query(this.Device, QueryDesc);
            this.Queries[TimeStamp.FrameDisjoint][1] = new Query(this.Device, QueryDesc);
            this.Queries[TimeStamp.FrameDisjoint][0].DebugName = TimeStamp.FrameDisjoint.ToString() + "_0";
            this.Queries[TimeStamp.FrameDisjoint][1].DebugName = TimeStamp.FrameDisjoint.ToString() + "_1";
            QueryDesc = new QueryDescription()
            {
                Type = QueryType.PipelineStatistics,
                Flags = QueryFlags.None
            };
            this.Queries[TimeStamp.PipelineInformation] = new Query[2];
            this.Queries[TimeStamp.PipelineInformation][0] = new Query(this.Device, QueryDesc);
            this.Queries[TimeStamp.PipelineInformation][1] = new Query(this.Device, QueryDesc);
            this.Queries[TimeStamp.PipelineInformation][0].DebugName = TimeStamp.PipelineInformation.ToString() + "_0";
            this.Queries[TimeStamp.PipelineInformation][0].DebugName = TimeStamp.PipelineInformation.ToString() + "_1";
            QueryDesc = new QueryDescription()
            {
                Type = QueryType.Timestamp,
                Flags = QueryFlags.None
            };
            foreach (TimeStamp Stamp in Enum.GetValues(typeof(TimeStamp)))
            {
                if (Stamp == TimeStamp.FrameDisjoint || Stamp == TimeStamp.PipelineInformation)
                    continue;
                this.Queries[Stamp] = new Query[2];
                this.Queries[Stamp][0] = new Query(this.Device, QueryDesc);
                this.Queries[Stamp][0].DebugName = Stamp.ToString() + "_0";
                this.Queries[Stamp][1] = new Query(this.Device, QueryDesc);
                this.Queries[Stamp][1].DebugName = Stamp.ToString() + "_1";
            }

        }

        public void StartFrame(double DeltaTime)
        {
            Immediate.Begin(this.GetQueryObject(TimeStamp.PipelineInformation));
            Immediate.Begin(this.GetQueryObject(TimeStamp.FrameDisjoint));
            Immediate.End(this.GetQueryObject(TimeStamp.BeginFrame));
            this.FrameCount++;
            this.TotalFrameCount++;
            this.TotalCPUDelta += DeltaTime;
            this.Timer.Start();
        }

        public void RecordTimeStamp(TimeStamp Stamp)
        {
            Immediate.End(this.GetQueryObject(Stamp));
        }

        public void EndFrame()
        {
            Immediate.End(this.GetQueryObject(TimeStamp.EndFrame));
            Immediate.End(this.GetQueryObject(TimeStamp.FrameDisjoint));
            Immediate.End(this.GetQueryObject(TimeStamp.PipelineInformation));
            this.ToggleFreeQueries();
            if (this.PendingCalculation)
            {   
                this.CalculateStats();
            }
            this.PendingCalculation = true;

            var Elapsed = (double)this.Timer.ElapsedTicks / (double)Stopwatch.Frequency;
            if (Elapsed >= 1.0f)
            {
                this.CommitStats(Elapsed);
                this.Timer.Reset();
            }
        }

        public void Render(BoxelRenderer.RenderDevice.RenderDevice2D RenderDevice)
        {
            var Builder = new StringBuilder();
            Builder.AppendLine(String.Format("FPS: {0:00.000}", this.PreviousStats.FrameRate));
            Builder.AppendLine(String.Format("CPU Time: {0:0.000}ms", this.PreviousStats.CPUTime));
            Builder.AppendLine(String.Format("GPU Time: {0:0.000}ms", this.PreviousStats.FrameTime));
            Builder.AppendLine(String.Format("  PreDraw: {0:0.000}ms", this.PreviousStats.IdleTime));
            Builder.AppendLine(String.Format("  CBuffer: {0:0.000}ms", this.PreviousStats.PerFrameBufferTime));
            Builder.AppendLine(String.Format("  Terrain: {0:0.000}ms", this.PreviousStats.Terrain));
            Builder.AppendLine(String.Format("  2D: {0:0.000}ms", this.PreviousStats.Draw2DTime));
            Builder.AppendLine(String.Format("  Present: {0:0.000}ms", this.PreviousStats.Present));
            Builder.AppendLine(String.Format("  Clear: {0:0.000}ms", this.PreviousStats.Clear));
            var Rect = new RectangleF(RenderDevice.Width - 140, 0, 140, 400);
            RenderDevice.DrawText(Builder.ToString(), Rect, Color.Green);
            if (TraceStats && this.FrameCount == 1)
                Trace.WriteLine(Builder.ToString());
        }

        private void CommitStats(double Elapsed)
        {
            PreviousStats = new Stats()
            {
                FrameRate = this.FrameCount/(float)Elapsed,
                CPUTime = (float)this.TotalCPUDelta / this.FrameCount*1000,
                Clear = this.AverageStats.Clear / this.FrameCount,
                FrameTime = this.AverageStats.FrameTime / this.FrameCount,
                IdleTime = this.AverageStats.IdleTime / this.FrameCount,
                PerFrameBufferTime = this.AverageStats.PerFrameBufferTime / this.FrameCount,
                Present = this.AverageStats.Present / this.FrameCount,
                Terrain = this.AverageStats.Terrain / this.FrameCount,
                PrimitivesRendererd = this.AverageStats.PrimitivesRendererd / this.FrameCount,
                PrimitivesSent = this.AverageStats.PrimitivesSent / this.FrameCount,
                Draw2DTime = this.AverageStats.Draw2DTime / this.FrameCount
            };
            this.AverageStats = new Stats();
            this.TotalCPUDelta = 0;
            this.FrameCount = 0;
            this.Stalls = 0;
            this.Disjoint = false;
        }

        private void CalculateStats()
        {
            if (this.Disjoint)
                return;
            QueryDataTimestampDisjoint DisjointData;
            this.WaitForTimestampData(out DisjointData);

            if (DisjointData.Disjoint)
            {
                this.Disjoint = true;
                return;
            }

            long BeginFrame, EndFrame, Terrain, Present, Idle, Buffer, Draw2D;
            QueryDataPipelineStatistics PipelineStats;
            bool Success = true;
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.BeginFrame), AsynchronousFlags.None, out BeginFrame);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.EndFrame), AsynchronousFlags.None, out EndFrame);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.DrawTerrain), AsynchronousFlags.None, out Terrain);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.Present), AsynchronousFlags.None, out Present);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.FrameIdle), AsynchronousFlags.None, out Idle);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.PerFrameBufferUpdate), AsynchronousFlags.None, out Buffer);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.Draw2D), AsynchronousFlags.None, out Draw2D);
            if(!Success)
                throw new InvalidOperationException("Timestamp queries not ready.");
            this.AverageStats = new Stats()
            {
                FrameTime = (float)(EndFrame - BeginFrame) / (float)DisjointData.Frequency * 1000.0f + AverageStats.FrameTime,
                IdleTime = (float)(Idle - BeginFrame) / (float)DisjointData.Frequency * 1000.0f + AverageStats.IdleTime,
                PerFrameBufferTime = (float)(Buffer - Idle) / (float)DisjointData.Frequency * 1000.0f + AverageStats.PerFrameBufferTime,
                Terrain = (float)(Terrain - Buffer) / (float)DisjointData.Frequency * 1000.0f + AverageStats.Terrain,
                Present = (float)(Present - Terrain) / (float)DisjointData.Frequency * 1000.0f + AverageStats.Present,
                Clear = (float)(EndFrame - Present) / (float)DisjointData.Frequency * 1000.0f + AverageStats.Clear,
                Draw2DTime = (float)(Draw2D - Terrain) / (float)DisjointData.Frequency * 1000.0f + AverageStats.Draw2DTime,
            };
            this.WaitForPipelineData(out PipelineStats);
            this.AverageStats.PrimitivesRendererd = PipelineStats.CPrimitiveCount;
            this.AverageStats.PrimitivesSent = PipelineStats.IAPrimitiveCount;
        }

        private void ToggleFreeQueries()
        {
            if (this.FreeQueryIndex == 0)
                this.FreeQueryIndex = 1;
            else
                this.FreeQueryIndex = 0;
        }

        private void WaitForTimestampData(out QueryDataTimestampDisjoint Data)
        {
            while (this.Immediate.GetData(this.GetQueryObject(TimeStamp.FrameDisjoint), AsynchronousFlags.None, out Data) == false)
                this.Stalls++;
        }

        private void WaitForPipelineData(out QueryDataPipelineStatistics Data)
        {
            while (this.Immediate.GetData(this.GetQueryObject(TimeStamp.PipelineInformation), AsynchronousFlags.None, out Data) == false)
                this.Stalls++;
        }

        private Query GetQueryObject(TimeStamp Stamp)
        {
            return this.Queries[Stamp][this.FreeQueryIndex];
        }

        private void Dispose(bool Disposing)
        {
            Trace.WriteLine(this.PreviousStats.ToString());
            foreach (var QueryArray in this.Queries.Values)
            {
                QueryArray[0].Dispose();
                QueryArray[1].Dispose();
            }
            if (Disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        ~GPUProfiler()
        {
            this.Dispose(false);
        }
    }
}
