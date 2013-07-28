using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using BoxelCommon;

namespace BoxelGame
{
    public class DeveloperConsole
    {
        private readonly IDictionary<string, IConsoleCommand> MethodMap;
        private readonly IDictionary<string, string> Aliases;
        private readonly static IDictionary<Type, dynamic> InstanceMap;

        static DeveloperConsole()
        {
            InstanceMap = new Dictionary<Type, Object>();
        }

        public DeveloperConsole()
        {
            this.MethodMap = new Dictionary<string, IConsoleCommand>();
            this.Aliases = new Dictionary<string, string>();
            this.AddCommandsFromAssembly(typeof(DeveloperConsole).Assembly);
        }

        public void AddCommandsFromAssembly(Assembly Source)
        {
            var StartCount = this.MethodMap.Keys.Count;
            var ConsoleMethods =  from x in Source.GetTypes()
                                  from y in x.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                  where y.GetCustomAttribute<ConsoleCommandAttribute>() != null
                                  select y;
            foreach(var Method in ConsoleMethods)
            {
                IConsoleCommand Command;
                this.MethodMap.TryGetValue(Method.Name.ToLower(), out Command);
                if(Command != null)
                {
                    Command.AddOverload(Method);
                }
                else
                {
                    this.MethodMap[Method.Name.ToLower()] = Command = new MethodCommand(Method);
                }
            }
            var ConsoleProperties = from x in Source.GetTypes()
                                    from z in x.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                    where z.GetCustomAttribute<ConsoleCommandAttribute>() != null
                                    select z;
            foreach (var Property in ConsoleProperties)
            {
                if (this.MethodMap.ContainsKey(Property.Name.ToLower()))
                    throw new Exception(String.Format(@"Attempted to add property ""{0}"", which already has an entry. Naming conflict between classes?", Property.Name));
                this.MethodMap[Property.Name.ToLower()] = new PropertyCommand(Property, Property.GetMethod != null, Property.SetMethod != null);
            }
            System.Diagnostics.Trace.WriteLine(String.Format("Added {0} console commands from {1}.", this.MethodMap.Keys.Count - StartCount,
                Source.GetName().Name));
        }

        public string Execute(string CommandName, params dynamic[] Parameters)
        {
            IConsoleCommand Command;
            this.MethodMap.TryGetValue(CommandName.ToLower(), out Command);
            if (Command == null)
                return String.Format(@"Unknown command ""{0}"".", CommandName);
            var Info = Command.GetInfo(Parameters);
            if (Info == null)
                return String.Format(@"No overload of ""{0}"" matches {1} parameters.", CommandName, Parameters.Length);
            Object Instance;
            InstanceMap.TryGetValue(Command.DeclaringType, out Instance);
            return this.CommandResultToString(Info.Delegate(Instance, Parameters), Info.Method.ReturnType);
        }

        public ConsoleCommandInfo Lookup(string CommandName, object[] Parameters)
        {
            IConsoleCommand Command;
            this.MethodMap.TryGetValue(CommandName.ToLower(), out Command);
            if (Command == null)
                return null;
            return Command.GetInfo(Parameters);
        }

        public static void SetInstanceForCommands(object Instance)
        {
            InstanceMap[Instance.GetType()] = Instance;
        }

        private string CommandResultToString(Object Result, Type ReturnType)
        {
            if (Result == null)
            {
                if (ReturnType == typeof(void))
                    return String.Empty;
                else
                    return "NULL";
            }
            var EnumerableResult = Result as IEnumerable<Object>;
            if (EnumerableResult != null)
            {
                var Builder = new StringBuilder();
                foreach (var S in EnumerableResult)
                {
                    Builder.AppendLine(S.ToString());
                }
                return Builder.ToString();
            }
            else
            {
                return Result.ToString();
            }
        }

        [ConsoleCommand]
        private void Alias(string Alias, string Command)
        {

        }

        [ConsoleCommand]
        private IEnumerable<string> ListCommands()
        {
            foreach(var Value in this.MethodMap.Values)
            {
                yield return Value.ToString();
            }
        }

        [ConsoleCommand]
        private string PropertyTest { get { return "Properties work!"; } }

        [ConsoleCommand]
        private int IntTest { get; set; }

        [ConsoleCommand]
        private string Test()
        {
            return "Test works!";
        }

        [ConsoleCommand]
        private string TestParams(int a, float b, string s, bool c, string d)
        {
            return String.Format("TestParams! Int: {0} Float: {1} String: {2} Bool: {3} String: {4}", a, b, s, c, d);
        }
    }
}
