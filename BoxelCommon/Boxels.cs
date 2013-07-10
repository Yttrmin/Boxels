﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxelLib;
using SharpDX;

namespace BoxelCommon
{
    public class BasicBoxel : IBoxel
    {
        public float Scale { get; private set; }
        public Int3 Position { get; private set; }
        public int Type { get; private set; }
        public IBoxelContainer Container { get; set; }

        public BasicBoxel(Int3 Position, float Scale, int Type)
        {
            this.Position = Position;
            this.Scale = Scale;
            this.Type = Type;
        }
    }
}
