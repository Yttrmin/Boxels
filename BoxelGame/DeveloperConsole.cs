using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace BoxelGame
{
    public class DeveloperConsole
    {
        public sealed class ExecMethod
        {
            public readonly string OriginalName;
            public readonly string Parameters;
            public readonly Func<dynamic, dynamic[], Object> Delegate;
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
                var Builder = new StringBuilder();
                Builder.Append("(");
                foreach(var Param in Method.GetParameters())
                {
                    if (Builder.Length > 1)
                        Builder.Append(", ");
                    Builder.Append(Param.ParameterType.Name);
                }
                Builder.Append(")");
                this.Parameters = Builder.ToString();
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
            var ConsoleMethods =  (from x in Source.GetTypes()
                                  from y in x.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                  where y.GetCustomAttribute<ConsoleCommand>() != null
                                  select y).Union(
                                      from x in Source.GetTypes()
                                 from z in x.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                 where z.GetCustomAttribute<ConsoleCommand>() != null
                                 from w in z.GetAccessors(true)
                                 select w);
            foreach(var Method in ConsoleMethods)
            {
                this.AddExecMethod(Method, !Method.Attributes.HasFlag(MethodAttributes.Static));
            }
            System.Diagnostics.Trace.WriteLine(String.Format("Added {0} console commands from {1}.", this.MethodMap.Keys.Count - StartCount,
                Source.GetName().Name));
        }

        /// <summary>
        /// Constructs a special-purpose lambda for calling a variety of methods with identical syntax.
        /// Essentially just generates a lambda that correctly calls the permutation of: return Method.Invoke(Instance, P0, P1, ...)
        /// that is correct for this Method. 
        /// </summary>
        /// <param name="Method">The method this lambda targets.</param>
        /// <returns>The delegate that invokes the lambda expression.</returns>
        private Func<dynamic, dynamic[], Object> CreateCommandLambda(MethodInfo Method)
        {
            var Parameters = Method.GetParameters();

            // To the CLR, dynamic is just Object. There is no typeof(dynamic).
            var Instance = Expression.Parameter(typeof(Object), "Instance");
            // So we'll just cast it to the proper type in here and avoid any generic mucking.
            var ConvertedInstance = Expression.Convert(Instance, Method.DeclaringType);
            // Similarly typeof(Object[]) would function identically.
            var LambdaParameters = Expression.Parameter(typeof(dynamic[]), "Parameters");

            // Create the expressions to add as parameters to the method call.
            var DynamicParameters = new List<Expression>();
            for (var i = 0; i < Parameters.Length; i++)
            {
                DynamicParameters.Add(Expression.Convert(Expression.ArrayIndex(LambdaParameters, Expression.Constant(i)), Parameters[i].ParameterType));
            }

            MethodCallExpression Invocation;
            if(Method.IsStatic)
                Invocation = Expression.Call(Method, DynamicParameters.ToArray());
            else
                Invocation = Expression.Call(ConvertedInstance, Method, DynamicParameters.ToArray());

            // The actual block of expressions executed when the lambda is called.
            BlockExpression LambdaBlock;
            if (Method.ReturnType == typeof(void))
            {
                // Return null.
                LambdaBlock = Expression.Block(Invocation, Expression.Constant(null));
            }
            else
            {
                // Return whatever the method returned.
                // Need to cast to Object, otherwise fails on things like ints. Presumebly Convert() boxes?
                LambdaBlock = Expression.Block(Expression.Convert(Invocation, typeof(Object)));
            }
            // Return a lambda that takes an Instance object (ignored if static), array of parameter values, and returns an object.
            return Expression.Lambda<Func<dynamic, dynamic[], Object>>(LambdaBlock, Instance, LambdaParameters).Compile();
        }

        public string Execute(string CommandName, params dynamic[] Parameters)
        {
            ExecMethod Info;
            this.MethodMap.TryGetValue(CommandName.ToLower(), out Info);
            if (Info == null)
                return String.Format(@"Unknown command ""{0}"".", CommandName);
            Object Instance;
            InstanceMap.TryGetValue(Info.Method.DeclaringType, out Instance);
            if (Info.InstanceMethod && Instance == null)
            {
                throw new Exception(String.Format(@"Attempted to execute ""{0}"" which is mapped to an instance method ""{1}"". However, there's no instance registered for type ""{2}"". Did you forget to call SetInstanceForCommands()?",
                    CommandName, Info.Method.ToString(), Info.ContainingClass));
            }
            return this.CommandResultToString(Info.Delegate(InstanceMap[Info.ContainingClass], Parameters));
        }

        public static void SetInstanceForCommands(object Instance)
        {
            InstanceMap[Instance.GetType()] = Instance;
        }

        public ExecMethod GetCommand(string Name)
        {
            ExecMethod Result;
            this.MethodMap.TryGetValue(Name.ToLower(), out Result);
            return Result;
        }

        private string CommandResultToString(Object Result)
        {
            if (Result == null)
                return "NULL";
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

        private void AddExecMethod(MethodInfo Method, bool IsInstance)
        {
            System.Diagnostics.Trace.WriteLine(String.Format("Adding console command from method {2} {0}.{1}", Method.DeclaringType, Method.Name, Method.ReturnType));
            this.MethodMap[Method.Name.ToLower()] = new ExecMethod(Method.Name, this.CreateCommandLambda(Method),
                    IsInstance, Method.DeclaringType, Method);
        }

        [ConsoleCommand]
        private IEnumerable<string> ListCommands()
        {
            foreach(var Value in this.MethodMap.Values)
            {
                yield return String.Format("{0} - {1}", Value.OriginalName, Value.Parameters);
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
