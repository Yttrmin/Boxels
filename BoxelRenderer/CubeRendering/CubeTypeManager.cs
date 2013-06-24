using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelCommon;

namespace BoxelRenderer
{
    public class CubeTypeManager
    {
        private BoxelTypes<CubeBoxelType> BoxelTypes;

        public CubeTypeManager()
        {
            this.BoxelTypes = new BoxelTypes<CubeBoxelType>();
        }

        public void Add(CubeBoxelType NewType)
        {
            this.BoxelTypes.Add(NewType);
        }
    }
}
