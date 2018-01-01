﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TombLib.Utils;

namespace TombLib.GeometryIO
{
    public class IOMaterial
    {
        // Material names
        public const string Material_Opaque = "TeMatOpaque";
        public const string Material_OpaqueDoubleSided = "TeMatOpaqueDoubleSided";
        public const string Material_AdditiveBlending = "TeMatAdditiveBlend";
        public const string Material_AdditiveBlendingDoubleSided = "TeMatAdditiveBlendDoubleSided";

        public string Name { get; private set; }
        public Texture Texture { get; set; }
        public bool AdditiveBlending { get; set; }
        public bool DoubleSided { get; set; }

        public IOMaterial(string name)
        {
            Name = name;
        }

        public IOMaterial(string name, Texture texture, bool additiveBlending, bool doubleSided)
        {
            Name = name;
            Texture = texture;
            AdditiveBlending = additiveBlending;
            DoubleSided = doubleSided;
        }
    }
}
