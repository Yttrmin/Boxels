using BoxelCommon;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelPhysics
{
    public class BoxelCollisionTester
    {
        private readonly int BoxelSize;

        public BoxelCollisionTester(int BoxelSize)
        {
            this.BoxelSize = BoxelSize;
        }

        public void AddContainer(IBoxelContainer Container)
        {

        }

        public IBoxel Trace(Ray Ray)
        {
            throw new NotImplementedException();
        }
    }
}
