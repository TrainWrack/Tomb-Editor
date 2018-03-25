﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TombLib.IO;
using TombLib.Utils;

namespace TombLib.Wad
{
    public class WadMesh : /*IEquatable<WadMesh>,*/ ICloneable
    {
        public string Name { get; set; }
        public List<Vector3> VerticesPositions { get; set; } = new List<Vector3>();
        public List<Vector3> VerticesNormals { get; set; } = new List<Vector3>();
        public List<short> VerticesShades { get; set; } = new List<short>();
        public List<WadPolygon> Polys { get; set; } = new List<WadPolygon>();
        public Hash Hash { get; private set; }
        public BoundingSphere BoundingSphere { get; set; }
        public BoundingBox BoundingBox { get; set; }
        public WadMeshLightingType LightingType { get; set; }

        public WadMesh()
        {

        }

        public void RecalculateNormals()
        {
            VerticesNormals.Clear();

            for (int i = 0; i < VerticesPositions.Count; i++)
            {
                int numPolygons = 0;
                var sum = Vector3.Zero;
                foreach (var poly in Polys)
                {
                    if (poly.Index0 == i || poly.Index1 == i || poly.Index2 == i || poly.Index3 == i)
                    {
                        // Calculate the face normal
                        var v1 = VerticesPositions[poly.Index0] - VerticesPositions[poly.Index2];
                        var v2 = VerticesPositions[poly.Index1] - VerticesPositions[poly.Index2];
                        var normal = Vector3.Cross(v1, v2);
                        sum += normal;
                        numPolygons++;
                    }
                }

                if (numPolygons != 0)
                    sum /= (float)numPolygons;

                VerticesNormals.Add(sum / sum.Length() * 16300.0f); // WTF Core?
            }
        }

        public WadMesh Clone()
        {
            var mesh = (WadMesh)MemberwiseClone();
            mesh.VerticesPositions = new List<Vector3>(VerticesPositions);
            mesh.VerticesNormals = new List<Vector3>(VerticesNormals);
            mesh.VerticesShades = new List<short>(VerticesShades);
            mesh.Polys = new List<WadPolygon>(Polys);
            return mesh;
        }
        object ICloneable.Clone() => Clone();

        public byte[] ToByteArray()
        {
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriterEx(ms);
                writer.Write(BoundingSphere.Center.X);
                writer.Write(BoundingSphere.Center.Y);
                writer.Write(BoundingSphere.Center.Z);
                writer.Write(BoundingSphere.Radius);

                int numVertices = VerticesPositions.Count;
                writer.Write(numVertices);

                for (int i = 0; i < VerticesPositions.Count; i++)
                    writer.Write(VerticesPositions[i]);

                if (VerticesNormals.Count > 0)
                    for (int i = 0; i < VerticesNormals.Count; i++)
                        writer.Write(VerticesNormals[i]);

                if (VerticesShades.Count > 0)
                    for (int i = 0; i < VerticesShades.Count; i++)
                        writer.Write(VerticesShades[i]);

                int numPolygons = Polys.Count;
                writer.Write(numPolygons);
                for (int i = 0; i < Polys.Count; i++)
                {
                    WadPolygon poly = Polys[i];
                    writer.Write((ushort)Polys[i].Shape);
                    writer.Write(Polys[i].Index0);
                    writer.Write(Polys[i].Index1);
                    writer.Write(Polys[i].Index2);
                    if (Polys[i].Shape == WadPolygonShape.Quad)
                        writer.Write(Polys[i].Index3);
                    writer.Write(((WadTexture)Polys[i].Texture.Texture).Hash);
                    writer.Write(Polys[i].Texture.DoubleSided);
                    writer.Write((short)Polys[i].Texture.BlendMode);
                    writer.Write(Polys[i].Texture.TexCoord0);
                    writer.Write(Polys[i].Texture.TexCoord1);
                    writer.Write(Polys[i].Texture.TexCoord2);
                    if (Polys[i].Shape == WadPolygonShape.Quad)
                        writer.Write(Polys[i].Texture.TexCoord3);
                    writer.Write(Polys[i].ShineStrength);
                }

                return ms.ToArray();
            }
        }

        public BoundingBox CalculateBoundingBox(Matrix4x4 transform)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            foreach (Vector3 oldVertex in VerticesPositions)
            {
                var transformedVertex = MathC.HomogenousTransform(oldVertex, transform);
                min = Vector3.Min(transformedVertex, min);
                max = Vector3.Max(transformedVertex, max);
            }
            return new BoundingBox(min, max);
        }

        public BoundingBox CalculateBoundingBox()
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            foreach (Vector3 oldVertex in VerticesPositions)
            {
                min = Vector3.Min(oldVertex, min);
                max = Vector3.Max(oldVertex, max);
            }
            return new BoundingBox(min, max);
        }

        public BoundingSphere CalculateBoundingSphere()
        {
            BoundingBox boundingBox = CalculateBoundingBox();
            return new BoundingSphere(
                (boundingBox.Maximum + boundingBox.Minimum) * 0.5f,
                Vector3.Distance(boundingBox.Minimum, boundingBox.Maximum) * 0.5f);
        }

        // DEPRECATED: this code could be useless because meshes doesn't use hashes anymore and additionally 
        // they interfere with List.IndexOf()
        //public static bool operator ==(WadMesh first, WadMesh second) => ReferenceEquals(first, null) ? ReferenceEquals(second, null) : (ReferenceEquals(second, null) ? false : first.Hash == second.Hash);
        //public static bool operator !=(WadMesh first, WadMesh second) => !(first == second);
        //public bool Equals(WadMesh other) => Hash == other.Hash;
        //public override bool Equals(object other) => other is WadMesh && Hash == ((WadMesh)other).Hash;
        //public override int GetHashCode() => Hash.GetHashCode();

        public static WadMesh Empty { get; } = new WadMesh();
    }
}

