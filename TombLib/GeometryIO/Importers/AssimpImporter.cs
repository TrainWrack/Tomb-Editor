﻿using Assimp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TombLib.Utils;

namespace TombLib.GeometryIO.Importers
{
    public class AssimpImporter : BaseGeometryImporter
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public AssimpImporter(IOGeometrySettings settings, GetTextureDelegate getTextureCallback)
            : base(settings, getTextureCallback)
        {

        }

        public override IOModel ImportFromFile(string filename)
        {
            string path = Path.GetDirectoryName(filename);

            // Use Assimp.NET for importing model
            AssimpContext context = new AssimpContext();
            Scene scene = context.ImportFile(filename, PostProcessPreset.TargetRealTimeMaximumQuality /* | PostProcessSteps.MakeLeftHanded*/);

            var newModel = new IOModel();
            var textures = new Dictionary<int, Texture>();

            // Create the list of materials to load
            for (int i = 0; i < scene.Materials.Count; i++)
            {
                var mat = scene.Materials[i];
                var material = new IOMaterial(mat.HasName ? mat.Name : "Material_" + i);

                var diffusePath = (mat.HasTextureDiffuse ? mat.TextureDiffuse.FilePath : null);
                if (string.IsNullOrWhiteSpace(diffusePath))
                    continue;

                textures.Add(i, GetTexture(path, diffusePath));

                // Create the new material
                material.Texture = textures[i];
                material.AdditiveBlending = mat.HasBlendMode && mat.BlendMode == Assimp.BlendMode.Additive;
                material.DoubleSided = mat.HasTwoSided && mat.IsTwoSided;
                newModel.Materials.Add(material);
            }

            /*foreach (var text in textures)
                newModel.Textures.Add(text.Value);*/

            var lastBaseVertex = 0;

            // Loop for each mesh loaded in scene
            foreach (var mesh in scene.Meshes)
            {
                // Import only textured meshes with valid materials
                Texture faceTexture;
                if (!textures.TryGetValue(mesh.MaterialIndex, out faceTexture))
                {
                    logger.Warn("Mesh '" + (mesh.Name ?? "") + "' does have material index " + mesh.MaterialIndex + " which can't be found.");
                    continue;
                }

                // Assimp's mesh is our IOSubmesh so we import meshes with just one submesh
                var material = newModel.Materials[mesh.MaterialIndex];
                var newMesh = new IOMesh();
                var newSubmesh = new IOSubmesh(material);
                newMesh.Submeshes.Add(material, newSubmesh);

                var hasTexCoords = mesh.HasTextureCoords(0);
                var hasColors = mesh.HasVertexColors(0);

                //newMesh.Texture = faceTexture;

                // Source data
                var positions = mesh.Vertices;
                var texCoords = mesh.TextureCoordinateChannels[0];
                var colors = mesh.VertexColorChannels[0];

                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    // Create position
                    var position = new Vector3(positions[i].X, positions[i].Y, positions[i].Z);
                    position = ApplyAxesTransforms(position);
                    newMesh.Positions.Add(position);

                    // Create UV
                    var currentUV = new Vector2(texCoords[i].X, texCoords[i].Y);
                    currentUV = ApplyUVTransform(currentUV, faceTexture.Image.Width, faceTexture.Image.Height);
                    newMesh.UV.Add(currentUV);

                    // Create colors
                    if (hasColors)
                    {
                        var color = new Vector4(colors[i].R, colors[i].G, colors[i].B, colors[i].A);
                        newMesh.Colors.Add(color);
                    }
                }

                // Add polygons
                foreach (var face in mesh.Faces)
                {
                    if (face.IndexCount == 3)
                    {
                        var poly = new IOPolygon(IOPolygonShape.Triangle);

                        poly.Indices.Add(lastBaseVertex + face.Indices[0]);
                        poly.Indices.Add(lastBaseVertex + face.Indices[1]);
                        poly.Indices.Add(lastBaseVertex + face.Indices[2]);

                        if (_settings.InvertFaces)
                            poly.Indices.Reverse();

                        newSubmesh.Polygons.Add(poly);
                    }
                    else if (face.IndexCount == 4)
                    {
                        var poly = new IOPolygon(IOPolygonShape.Quad);

                        poly.Indices.Add(lastBaseVertex + face.Indices[0]);
                        poly.Indices.Add(lastBaseVertex + face.Indices[1]);
                        poly.Indices.Add(lastBaseVertex + face.Indices[2]);
                        poly.Indices.Add(lastBaseVertex + face.Indices[3]);

                        if (_settings.InvertFaces)
                            poly.Indices.Reverse();

                        newSubmesh.Polygons.Add(poly);
                    }
                }

                newModel.Meshes.Add(newMesh);
            }

            return newModel;
        }
    }
}
