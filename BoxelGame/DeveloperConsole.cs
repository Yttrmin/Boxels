using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BoxelGame
{
    public class DeveloperConsole
    {
        private struct ExecMethod
        {
            public readonly string OriginalName;
        }
        private IDictionary<string, ExecMethod> MethodMap;

        public DeveloperConsole()
        {
            this.MethodMap = new Dictionary<string, ExecMethod>();
        }

        public void AddCommandsFromAssembly(Assembly Source)
        {

        }

        private void AddExecMethod(MethodInfo Method)
        {

        }
    }
}
