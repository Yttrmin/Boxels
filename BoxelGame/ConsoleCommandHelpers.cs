using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BoxelGame
{
    public sealed class ConsoleCommandInfo
    {
        public readonly MethodInfo Method;
        public readonly ParameterInfo[] Parameters;
        public readonly string ParametersString;
        public readonly Func<dynamic, dynamic[], Object> Delegate;

        public ConsoleCommandInfo(MethodInfo Method, Func<dynamic, dynamic[], Object> Delegate)
        {
            this.Method = Method;
            this.Delegate = Delegate;
            this.Parameters = Method.GetParameters();

            var Builder = new StringBuilder();
            Builder.Append("(");
            foreach (var Param in this.Parameters)
            {
                if (Builder.Length > 1)
                    Builder.Append(", ");
                Builder.AppendFormat("{0} {1}", Param.ParameterType.Name, Param.Name);
            }
            if (this.Parameters.Length == 0)
                Builder.Append("Void");
            Builder.Append(")");
            this.ParametersString = Builder.ToString();
        }

        public override string ToString()
        {
            return String.Format("[{0}] {1} - {2}", this.Method.ReturnType.Name, this.Method.Name, this.ParametersString);
        }
    }

    public interface IConsoleCommand
    {
        ConsoleCommandInfo GetInfo(object[] Parameters);
        IEnumerable<ConsoleCommandInfo> AllInfo { get; }
        Type DeclaringType { get; }
        void AddOverload(MethodInfo Method);
    }

    public abstract class BaseCommand : IConsoleCommand
    {
        private readonly string Name;
        private readonly IList<ConsoleCommandInfo> Infos;
        public Type DeclaringType { get; private set; }
        public IEnumerable<ConsoleCommandInfo> AllInfo
        {
            get
            {
                foreach (var Info in this.Infos)
                {
                    yield return Info;
                }
            }
        }
        protected int OverloadCount { get { return this.Infos.Count; } }
        protected BaseCommand(Type DeclaringType, string Name)
        {
            this.Infos = new List<ConsoleCommandInfo>();
            this.DeclaringType = DeclaringType;
            this.Name = Name;
        }

        protected void AddMethod(MethodInfo Method)
        {
            if (Method.DeclaringType != this.DeclaringType)
                throw new Exception(String.Format("Attempted to add method from type {0} to a command that only accepts from type {1}", Method.DeclaringType, this.DeclaringType));
            var MethodParams = Method.GetParameters();
            foreach(var Info in Infos)
            {
                if (Info.Parameters.Length == MethodParams.Length)
                    throw new Exception(String.Format("There already exists an overload with {0} parameters. Overloading based on type is not supported yet.", MethodParams.Length));
            }
            this.Infos.Add(new ConsoleCommandInfo(Method, this.CreateCommandLambda(Method)));
        }

        public ConsoleCommandInfo GetInfo(object[] Parameters)
        {
            foreach(var Info in Infos)
            {
                if(Parameters.Length == Info.Parameters.Length)
                {
                    return Info;
                }
            }
            return null;
        }

        public abstract void AddOverload(MethodInfo Method);

        public override string ToString()
        {
            if (this.OverloadCount == 1)
                return this.Infos[0].ToString();
            var Builder = new StringBuilder();
            Builder.AppendLine(String.Format("{0} ({1} overloads)", this.Name, this.OverloadCount));
            foreach (var Info in this.AllInfo)
            {
                Builder.Append(String.Format("   {0}", Info.ToString()));
            }
            return Builder.ToString();
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
            // Similarly typeof(Object[]) would function identically.
            var LambdaParameters = Expression.Parameter(typeof(dynamic[]), "Parameters");

            // Create the expressions to add as parameters to the method call.
            var DynamicParameters = new List<Expression>();
            for (var i = 0; i < Parameters.Length; i++)
            {
                DynamicParameters.Add(Expression.Convert(Expression.ArrayIndex(LambdaParameters, Expression.Constant(i)), Parameters[i].ParameterType));
            }

            var BodyExpressions = new List<Expression>();
            MethodCallExpression Invocation;
            if (Method.IsStatic)
            {
                Invocation = Expression.Call(Method, DynamicParameters.ToArray());
            }
            else
            {
                // So we'll just cast it to the proper type in here and avoid any generic mucking.
                var ConvertedInstance = Expression.Convert(Instance, Method.DeclaringType);
                Invocation = Expression.Call(ConvertedInstance, Method, DynamicParameters.ToArray());
                var NullCheck = Expression.IfThenElse(Expression.Equal(Instance, Expression.Constant(null)), Expression.Throw(Expression.Constant(new ArgumentNullException("Attempted to invoke a lambda for an instance method, but passed a null 'this' reference. Did you forget to call SetInstanceForCommands()?"), typeof(ArgumentNullException))), Invocation);
                BodyExpressions.Add(NullCheck);
            }

            // The actual block of expressions executed when the lambda is called.
            BlockExpression LambdaBlock;
            if (Method.ReturnType == typeof(void))
            {
                // Return null.
                BodyExpressions.Add(Invocation);
                BodyExpressions.Add(Expression.Constant(null));
                LambdaBlock = Expression.Block(BodyExpressions);
            }
            else
            {
                // Return whatever the method returned.
                // Need to cast to Object, otherwise fails on things like ints. Presumebly Convert() boxes?
                BodyExpressions.Add(Expression.Convert(Invocation, typeof(Object)));
                LambdaBlock = Expression.Block(BodyExpressions);
            }
            // Return a lambda that takes an Instance object (ignored if static), array of parameter values, and returns an object.
            return Expression.Lambda<Func<dynamic, dynamic[], Object>>(LambdaBlock, Instance, LambdaParameters).Compile();
        }
    }

    public sealed class PropertyCommand : BaseCommand
    {
        private readonly PropertyInfo Property;
        public PropertyCommand(PropertyInfo Property, bool AddGetter, bool AddSetter) : base(Property.DeclaringType, Property.Name)
        {
            if (Property.GetIndexParameters().Length > 0)
                throw new NotImplementedException("Indexed properties not supported yet.");
            this.Property = Property;
            if (AddGetter)
                this.AddMethod(Property.GetMethod);
            if (AddSetter)
                this.AddMethod(Property.SetMethod);
        }

        public override void AddOverload(MethodInfo Method)
        {
            throw new NotImplementedException(String.Format(@"Attempted to overload property ""{0}"". Properties can't be overloaded. Is there a naming conflict between classes?", this.Property.Name));
        }
    }

    public sealed class MethodCommand : BaseCommand
    {
        public MethodCommand(MethodInfo InitialMethod) : base(InitialMethod.DeclaringType, InitialMethod.Name)
        {
            this.AddMethod(InitialMethod);
        }

        public override void AddOverload(MethodInfo Method)
        {
            this.AddMethod(Method);
        }
    }
}
