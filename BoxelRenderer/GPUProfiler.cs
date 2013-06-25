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

    public class GPUProfiler
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
            PipelineInformation
        }
        public struct Stats
        {
            public float FrameTime;
            public float Present;
            public float IdleTime;
            public float PerFrameBufferTime;
            public float Terrain;
            public float Clear;
            public long PrimitivesSent;
            public long PrimitivesRendererd;

            public override string ToString()
            {
                return String.Format("GPU frame: {0}ms  Pre-Draw: {4}ms  PerFrameCBuffer: {5}ms  TerrainDraw: {1}ms  PresentTime: {2}ms  ClearTime: {3}ms | PrimsSent: {6}  PrimsRendered: {7}", 
                    FrameTime, Terrain, Present, Clear, IdleTime, PerFrameBufferTime, PrimitivesSent, PrimitivesRendererd);
            }
        }
        private Device1 Device;
        private DeviceContext1 Immediate;
        private IDictionary<TimeStamp, Query[]> Queries;
        private bool Waiting;
        private bool Disjoint;
        private Stats AverageStats;
        private double TotalCPUDelta;
        private Stopwatch Timer;
        private int FrameCount;
        private bool PendingCalculation;
        private int FreeQueryIndex;
        private int Stalls;

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
            QueryDesc = new QueryDescription()
            {
                Type = QueryType.PipelineStatistics,
                Flags = QueryFlags.None
            };
            this.Queries[TimeStamp.PipelineInformation] = new Query[2];
            this.Queries[TimeStamp.PipelineInformation][0] = new Query(this.Device, QueryDesc);
            this.Queries[TimeStamp.PipelineInformation][1] = new Query(this.Device, QueryDesc);
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
                this.Queries[Stamp][1] = new Query(this.Device, QueryDesc);
            }

        }

        public void StartFrame(double DeltaTime)
        {
            Immediate.Begin(this.GetQueryObject(TimeStamp.PipelineInformation));
            Immediate.Begin(this.GetQueryObject(TimeStamp.FrameDisjoint));
            Immediate.End(this.GetQueryObject(TimeStamp.BeginFrame));
            this.FrameCount++;
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
                this.PrintStats(Elapsed);
                this.Timer.Reset();
            }
        }

        private void PrintStats(double Elapsed)
        {
            var Builder = new StringBuilder();
            Builder.AppendFormat("FPS: {0} CPU frame: {1}ms  ", this.FrameCount / Elapsed, this.TotalCPUDelta / this.FrameCount * 1000);
            if (this.Disjoint)
            {
                Builder.Append("GPU timings disjoint, data unavailable.");
            }
            else
            {
                var AveragedStats = new Stats()
                {
                    Clear = this.AverageStats.Clear / this.FrameCount,
                    FrameTime = this.AverageStats.FrameTime / this.FrameCount,
                    IdleTime = this.AverageStats.IdleTime / this.FrameCount,
                    PerFrameBufferTime = this.AverageStats.PerFrameBufferTime / this.FrameCount,
                    Present = this.AverageStats.Present / this.FrameCount,
                    Terrain = this.AverageStats.Terrain / this.FrameCount,
                    PrimitivesRendererd = this.AverageStats.PrimitivesRendererd / this.FrameCount,
                    PrimitivesSent = this.AverageStats.PrimitivesSent / this.FrameCount,
                };
                Builder.Append(AveragedStats);
            }
            Builder.AppendFormat(" | QueryStalls: {0}", this.Stalls);
            Trace.WriteLine(Builder.ToString());
            this.AverageStats = new Stats();
            this.TotalCPUDelta = 0;
            this.FrameCount = 0;
            this.Stalls = 0;
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

            long BeginFrame, EndFrame, Terrain, Present, Idle, Buffer;
            QueryDataPipelineStatistics PipelineStats;
            bool Success = true;
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.BeginFrame), AsynchronousFlags.None, out BeginFrame);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.EndFrame), AsynchronousFlags.None, out EndFrame);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.DrawTerrain), AsynchronousFlags.None, out Terrain);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.Present), AsynchronousFlags.None, out Present);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.FrameIdle), AsynchronousFlags.None, out Idle);
            Success &= this.Immediate.GetData(this.GetQueryObject(TimeStamp.PerFrameBufferUpdate), AsynchronousFlags.None, out Buffer);
            if (!Success)
                throw new InvalidOperationException("Timestamp queries not ready.");
            this.AverageStats = new Stats()
            {
                FrameTime = (float)(EndFrame - BeginFrame) / (float)DisjointData.Frequency * 1000.0f + AverageStats.FrameTime,
                IdleTime = (float)(Idle - BeginFrame) / (float)DisjointData.Frequency * 1000.0f + AverageStats.IdleTime,
                PerFrameBufferTime = (float)(Buffer - Idle) / (float)DisjointData.Frequency * 1000.0f + AverageStats.PerFrameBufferTime,
                Terrain = (float)(Terrain - Buffer) / (float)DisjointData.Frequency * 1000.0f + AverageStats.Terrain,
                Present = (float)(Present - Terrain) / (float)DisjointData.Frequency * 1000.0f + AverageStats.Present,
                Clear = (float)(EndFrame - Present) / (float)DisjointData.Frequency * 1000.0f + AverageStats.Clear,
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
    }
}
