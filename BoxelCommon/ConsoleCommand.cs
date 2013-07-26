using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostSharp;

namespace BoxelGame
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class ConsoleCommand : Attribute
    {
        
    }
}
