using BoxelCommon;
using BoxelRenderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectBoxelGame
{
    class PVTypeManager : BoxelTypes<ICubeBoxelType>
    {
        public PVTypeManager() : base()
        {
            this.Add(new CubeBoxelType("PVGrass_PosNeg_XY.png", "PVGrass_PosNeg_XY.png", "PVGrass_Pos_Z.png", "PVGrass_Neg_Z.png", "PVGrass_PosNeg_XY.png", "PVGrass_PosNeg_XY.png"));
            this.Add(new CubeBoxelType("PVDirt_PosNeg_XYZ.png", "PVDirt_PosNeg_XYZ.png", "PVDirt_PosNeg_XYZ.png", "PVDirt_PosNeg_XYZ.png", "PVDirt_PosNeg_XYZ.png", "PVDirt_PosNeg_XYZ.png"));
            this.Add(new CubeBoxelType("PVStone_PosNeg_XYZ.png", "PVStone_PosNeg_XYZ.png", "PVStone_PosNeg_XYZ.png", "PVStone_PosNeg_XYZ.png", "PVStone_PosNeg_XYZ.png", "PVStone_PosNeg_XYZ.png"));
        }
    }
}
