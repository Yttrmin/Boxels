using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelCommon
{
    public interface IProfiler
    {
        void BeginFrame(double DeltaTime);
        void EndFrame();
        void Begin(string ID);
        void End(string ID);
    }

    public interface ICPUProfiler : IProfiler
    {

    }

    public interface IGPUProfiler : IProfiler
    {

    }
}
