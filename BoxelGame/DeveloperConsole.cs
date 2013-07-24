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
        private class ExecMethod
        {
            public readonly string OriginalName;
            // Is an Action<> with the appropriate parameters.
            public readonly dynamic Delegate;
            public readonly MethodInfo Method;
            public readonly bool InstanceMethod;
            public readonly Type ContainingClass;

            public ExecMethod(string Name, dynamic Delegate, bool IsInstanceMethod, Type ContainingType, MethodInfo Method)
            {
                this.OriginalName = Name;
                this.Delegate = Delegate;
                this.InstanceMethod = IsInstanceMethod;
                this.ContainingClass = ContainingType;
                this.Method = Method;
            }
        }
        private IDictionary<string, ExecMethod> MethodMap;
        private static IDictionary<Type, dynamic> InstanceMap;

        static DeveloperConsole()
        {
            InstanceMap = new Dictionary<Type, Object>();
        }

        public DeveloperConsole()
        {
            this.MethodMap = new Dictionary<string, ExecMethod>();
            this.AddCommandsFromAssembly(typeof(DeveloperConsole).Assembly);
        }

        public void AddCommandsFromAssembly(Assembly Source)
        {
            var StartCount = this.MethodMap.Keys.Count;
            var ConsoleMethods = from x in Source.GetTypes()
                                 from y in x.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                 where y.GetCustomAttribute<ConsoleCommand>() != null
                                 select y;
            foreach(var Method in ConsoleMethods)
            {
                this.AddExecMethod(Method, !Method.Attributes.HasFlag(MethodAttributes.Static));
            }
            System.Diagnostics.Trace.WriteLine(String.Format("Added {0} console commands from {1}.", this.MethodMap.Keys.Count - StartCount,
                Source.GetName().Name));
        }

        public string Execute(string CommandName, params Object[] Parameters)
        {
            var Info = this.MethodMap[CommandName.ToLower()];
            bool ReturnsStrings = typeof(string).IsAssignableFrom(Info.Method.ReturnType) || typeof(IEnumerable<string>).IsAssignableFrom(Info.Method.ReturnType);
            Object Result;
            if(Info.InstanceMethod)
            {
                if (Parameters == null || Parameters.Length == 0)
                {
                    if (ReturnsStrings)
                    {
                        Result = Info.Delegate.Invoke(InstanceMap[Info.ContainingClass]);
                        return CommandResultToString(Result);
                    }
                    else
                    {
                        Info.Delegate.Invoke(InstanceMap[Info.ContainingClass]);
                        return null;
                    }
                }
                else
                {
                    if (ReturnsStrings)
                    {
                        Result = Info.Delegate.Invoke(InstanceMap[Info.ContainingClass], Parameters);
                        return CommandResultToString(Result);
                    }
                    else
                    {
                        Info.Delegate.Invoke(InstanceMap[Info.ContainingClass], Parameters);
                        return null;
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
                Info.Delegate.Invoke();
            }
        }

        public static void SetInstanceForCommands(object Instance)
        {
            InstanceMap[Instance.GetType()] = Instance;
        }

        private string CommandResultToString(Object Result)
        {
            var StringResult = Result as string;
            if(StringResult != null)
            {
                return StringResult;
            }
            else
            {
                var EnumerableResult = (IEnumerable<string>)Result;
                var Builder = new StringBuilder();
                foreach(var S in EnumerableResult)
                {
                    Builder.AppendLine(S);
                }
                return Builder.ToString();
            }
        }

        private void AddExecMethod(MethodInfo Method, bool IsInstance)
        {
            if (Method.GetParameters().Length > 0)
                throw new ArgumentException("Methods with parameters not supported yet.", "Method");
            if (!IsInstance)
                throw new ArgumentException("Static methods not supported yet.");
            System.Diagnostics.Trace.WriteLine(String.Format("Adding console command from method {2} {0}.{1}", Method.DeclaringType, Method.Name, Method.ReturnType));
            bool NonVoidReturn = Method.ReturnType != typeof(void);
            Type DeleType;
            if (IsInstance)
            {
                if(NonVoidReturn)
                    DeleType = typeof(Func<,>).MakeGenericType(Method.DeclaringType,Method.ReturnType);
                else
                    DeleType = typeof(Action<>).MakeGenericType(Method.DeclaringType);
            }
            else
            {
                DeleType = typeof(Action);
            }
            this.MethodMap[Method.Name.ToLower()] = new ExecMethod(Method.Name, Delegate.CreateDelegate(DeleType, null, Method),
                    IsInstance, Method.DeclaringType, Method);
        }

        [ConsoleCommand]
        private IEnumerable<string> ListCommands()
        {
            foreach(var Value in this.MethodMap.Values)
            {
                yield return Value.OriginalName;
            }
        }

        [ConsoleCommand]
        private string Test()
        {
            return "Test works!";
        }
    }
}
