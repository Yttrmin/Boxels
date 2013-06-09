﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace BoxelLib
{
    public interface ILocalPlayer
    {
        Vector3 Position { get; }
        ICamera Camera { get; }
    }
}
