﻿using SharpDX.Toolkit.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TombLib.Utils;
using TombLib.Wad;
using Buffer = SharpDX.Toolkit.Graphics.Buffer;

namespace TombLib.Graphics
{
    public class RoomModel : Model<ObjectMesh, ObjectVertex>
    {
        public RoomModel(GraphicsDevice device)
            : base(device, ModelType.Room)
        { }

        public override void UpdateBuffers()
        {
            Vertices = new List<ObjectVertex>();
            Indices = new List<int>();

            foreach (var mesh in Meshes)
            {
                int lastBaseIndex = 0;

                Vertices.AddRange(mesh.Vertices);

                foreach (var submesh in mesh.Submeshes)
                {
                    submesh.Value.BaseIndex = lastBaseIndex;
                    foreach (var index in submesh.Value.Indices)
                        if (submesh.Value.NumIndices != 0)
                            Indices.Add((ushort)(index));
                    lastBaseIndex += submesh.Value.NumIndices;
                }

                mesh.UpdateBoundingBox();
            }

            if (Vertices.Count == 0) return;

            if (VertexBuffer != null)
                VertexBuffer.Dispose();
            if (IndexBuffer != null)
                IndexBuffer.Dispose();

            VertexBuffer = Buffer.Vertex.New(GraphicsDevice, Vertices.ToArray<ObjectVertex>(), SharpDX.Direct3D11.ResourceUsage.Dynamic);
            IndexBuffer = Buffer.Index.New(GraphicsDevice, Indices.ToArray(), SharpDX.Direct3D11.ResourceUsage.Dynamic);
        }
    }
}