﻿using TombLib.Utils;

namespace TombLib.GeometryIO
{
    public class IOMaterial
    {
        public string Name { get; private set; }
        public string Path { get; set; }
        public Texture Texture { get; set; }
        public bool AdditiveBlending { get; set; }
        public bool DoubleSided { get; set; }
        public int Shininess { get; set; }
        public string TexturePath { get; set; }

        public IOMaterial(string name)
        {
            Name = name;
        }

        public IOMaterial(string name, Texture texture, string texturePath, bool additiveBlending, bool doubleSided, int shininess)
        {
            Name = name;
            Texture = texture;
            AdditiveBlending = additiveBlending;
            DoubleSided = doubleSided;
            Shininess = shininess;
            TexturePath = texturePath;
        }
    }
}
