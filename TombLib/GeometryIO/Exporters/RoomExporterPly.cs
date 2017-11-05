﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TombLib.GeometryIO.Exporters
{
    public class RoomExporterPly : BaseGeometryExporter
    {
        public RoomExporterPly(IOGeometrySettings settings)
            : base(settings)
        {

        }

        public override bool ExportToFile(IOModel model, string filename)
        {
            var path = Path.GetDirectoryName(filename);
            var material = path + "\\" + Path.GetFileNameWithoutExtension(filename) + ".mtl";

            if (File.Exists(filename)) File.Delete(filename);
            if (File.Exists(material)) File.Delete(material);

            using (var writer = new StreamWriter(File.OpenWrite(filename)))
            {
                var mesh = model.Meshes[0];

                writer.WriteLine("ply");
                writer.WriteLine("format ascii 1.0");
                writer.WriteLine("comment Created by Tomb Editor");
                writer.WriteLine("comment TextureFile " + mesh.Texture.Name);
                writer.WriteLine("element vertex " + mesh.Positions.Count);
                writer.WriteLine("property float x");
                writer.WriteLine("property float y");
                writer.WriteLine("property float z");
                writer.WriteLine("property float s");
                writer.WriteLine("property float t");
                writer.WriteLine("property uchar red");
                writer.WriteLine("property uchar green");
                writer.WriteLine("property uchar blue");
                writer.WriteLine("element face " + mesh.Polygons.Count);
                writer.WriteLine("property list uchar uint vertex_indices");
                writer.WriteLine("end_header");

                // Save vertices
                for (var i = 0; i < mesh.Positions.Count; i++)
                {
                    var position = ApplyAxesTransforms(mesh.Positions[i]);
                    writer.Write(position.X.ToString(CultureInfo.InvariantCulture) + " " +
                                 position.Y.ToString(CultureInfo.InvariantCulture) + " " +
                                 position.Z.ToString(CultureInfo.InvariantCulture) + " ");

                    var uv = ApplyUVTransform(mesh.UV[i], mesh.Texture.Width, mesh.Texture.Height);
                    writer.Write(uv.X.ToString(CultureInfo.InvariantCulture) + " " +
                                 uv.Y.ToString(CultureInfo.InvariantCulture) + " ");

                    var color = ApplyColorTransform(mesh.Colors[i]);
                    writer.WriteLine((int)(color.X * 128.0f) + " " + 
                                     (int)(color.Y * 128.0f) + " " +
                                     (int)(color.Z * 128.0f));
                }

                // Save faces
                foreach (var poly in mesh.Polygons)
                {
                    var indices = poly.Indices;
                    var v1 = indices[0];
                    var v2 = indices[1];
                    var v3 = indices[2];
                    var v4 = (poly.Shape == IOPolygonShape.Quad ? indices[3] : 0);

                    if (poly.Shape == IOPolygonShape.Triangle)
                    {
                        writer.WriteLine("3 " + v1 + " " + v2 + " " + v3);
                    }
                    else
                    {
                        writer.WriteLine("4 " + v1 + " " + v2 + " " + v3 + " " + v4);
                    }
                }               
            }

            return true;
        }
    }
}
