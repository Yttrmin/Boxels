using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostSharp;
using PostSharp.Aspects;
using System.Diagnostics;
using PostSharp.Extensibility;

namespace BoxelCommon
{
    [Serializable]
    [MulticastAttributeUsage(MulticastTargets.Method, Inheritance = MulticastInheritance.Multicast)]
    public class Timer : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {
            Stopwatch Watch = new Stopwatch();
            Watch.Start();
            args.Proceed();
            Watch.Stop();
            Trace.WriteLine(String.Format("Timer: {0} took {1}ms to execute.", args.Method.DeclaringType.Name+"::"+args.Method.Name, (float)Watch.ElapsedTicks / (float)Stopwatch.Frequency * 1000.0f));
        }
    }
}
