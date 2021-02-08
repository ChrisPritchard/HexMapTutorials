
namespace DarkDomains
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour 
    {
        Mesh hexMesh;
        MeshCollider meshCollider;

        public bool UseCollider;
        public bool UseCellData;
        public bool UseUV;
        public bool UseUV2;

        // these are static as they are temporary buffers, cleared then used for only a given triangulation
        [NonSerialized] List<Vector3> vertices, cellIndices;
        [NonSerialized] List<int> triangles;
        [NonSerialized] List<Color> cellWeights;
        [NonSerialized] List<Vector2> uvs, uv2s;

        private void Awake() 
        {
            hexMesh = new Mesh { name = "Hex Mesh" };
            GetComponent<MeshFilter>().mesh = hexMesh;
            if(UseCollider)
                meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        public void Clear()
        {
            hexMesh.Clear();
            vertices = ListPool<Vector3>.Get();
            if (UseCellData)
            {
                cellWeights = ListPool<Color>.Get();
                cellIndices = ListPool<Vector3>.Get();
            }
            triangles = ListPool<int>.Get();
            if(UseUV)
                uvs = ListPool<Vector2>.Get();
            if(UseUV2)
                uv2s = ListPool<Vector2>.Get();
        }

        public void Apply()
        {
            hexMesh.SetVertices(vertices);
            ListPool<Vector3>.Add(vertices);

            if(UseCellData)
            {
                hexMesh.SetColors(cellWeights);
                ListPool<Color>.Add(cellWeights);
                hexMesh.SetUVs(2, cellIndices);
                ListPool<Vector3>.Add(cellIndices);
            }

            hexMesh.SetTriangles(triangles, 0);
            ListPool<int>.Add(triangles);

            if(UseUV)
            {
                hexMesh.SetUVs(0, uvs);
                ListPool<Vector2>.Add(uvs);
            }

            if(UseUV2)
            {
                hexMesh.SetUVs(1, uv2s);
                ListPool<Vector2>.Add(uv2s);
            }

            hexMesh.RecalculateNormals();

            if(UseCollider)
                meshCollider.sharedMesh = hexMesh;
        }

        // adds a new triangle, both adding the vertices to the vertices list, and 
        // adding the three indices (offset by the current count, as this will be the index after the last set of vertices added)
        // to the triangles list
        public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{v1, v2, v3});
            triangles.AddRange(new[]{index,index+1,index+2});
        }

        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) =>
            AddTriangleUnperturbed(HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), HexMetrics.Perturb(v3));

        public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3) => uvs.AddRange(new[] { uv1, uv2, uv3 });

        public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3) => uv2s.AddRange(new[] { uv1, uv2, uv3 });

        public void AddTriangleCellData(Vector3 indices, Color weights1, Color weights2, Color weights3)
        {
            cellIndices.AddRange(new[] { indices, indices, indices });
            cellWeights.AddRange(new[] { weights1, weights2, weights3 });
        }

        public void AddTriangleCellData(Vector3 indices, Color weights) => AddTriangleCellData(indices, weights, weights, weights);

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), HexMetrics.Perturb(v3), HexMetrics.Perturb(v4)});
            triangles.AddRange(new[]{index, index+2, index+1});
            triangles.AddRange(new[]{index+1, index+2, index+3});
        }

        public void AddQuadUnperterbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{v1, v2, v3, v4});
            triangles.AddRange(new[]{index, index+2, index+1});
            triangles.AddRange(new[]{index+1, index+2, index+3});
        }

        // for when each corner is a different colour
        public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4)
        {
            cellIndices.AddRange(new[] { indices, indices, indices, indices });
            cellWeights.AddRange(new[] { weights1, weights2, weights3, weights4 });
        }

        // for when opposite sides of the quad are different colours
        public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2) => AddQuadCellData(indices, weights1, weights1, weights2, weights2);

        public void AddQuadCellData(Vector3 indices, Color weights) => AddQuadCellData(indices, weights, weights);

        public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) => uvs.AddRange(new[] { uv1, uv2, uv3, uv4 });

        public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) => uv2s.AddRange(new[] { uv1, uv2, uv3, uv4 });

        public void AddQuadUV(float uMin, float uMax, float vMin, float vMax) =>
            AddQuadUV(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax));

        public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax) =>
            AddQuadUV2(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax));
    }
}