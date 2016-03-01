using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelCommon
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        
    }
}
