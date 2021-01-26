
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
        public bool UseColours;
        public bool UseUV;
        public bool UseTerrainTypes;

        // these are static as they are temporary buffers, cleared then used for only a given triangulation
        [NonSerialized] List<Vector3> vertices, terrainTypes;
        [NonSerialized] List<int> triangles;
        [NonSerialized] List<Color> colours;
        [NonSerialized] List<Vector2> uvs;

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
            if (UseTerrainTypes)
                terrainTypes = ListPool<Vector3>.Get();
            triangles = ListPool<int>.Get();
            if(UseColours)
                colours = ListPool<Color>.Get();
            if(UseUV)
                uvs = ListPool<Vector2>.Get();
        }

        public void Apply()
        {
            hexMesh.SetVertices(vertices);
            ListPool<Vector3>.Add(vertices);

            if(UseTerrainTypes)
            {
                hexMesh.SetUVs(2, terrainTypes);
                ListPool<Vector3>.Add(terrainTypes);
            }

            hexMesh.SetTriangles(triangles, 0);
            ListPool<int>.Add(triangles);

            if(UseColours)
            {
                hexMesh.SetColors(colours);
                ListPool<Color>.Add(colours);
            }

            if(UseUV)
            {
                hexMesh.SetUVs(0, uvs);
                ListPool<Vector2>.Add(uvs);
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

        // one colour for triangle
        public void AddTriangleColour(Color c1) => colours.AddRange(new[]{c1, c1, c1});

        // adds colours for each vertex
        public void AddTriangleColour(Color c1, Color c2, Color c3) => colours.AddRange(new[]{c1, c2, c3});

        public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3) => uvs.AddRange(new[] { uv1, uv2, uv3 });

        public void AddTriangleTerrainTypes(Vector3 types) => terrainTypes.AddRange(new[] { types, types, types });

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), HexMetrics.Perturb(v3), HexMetrics.Perturb(v4)});
            triangles.AddRange(new[]{index, index+2, index+1});
            triangles.AddRange(new[]{index+1, index+2, index+3});
        }

        // for when each corner is a different colour
        public void AddQuadColour(Color c1, Color c2, Color c3, Color c4) => colours.AddRange(new[]{c1, c2, c3, c4});

        // for when opposite sides of the quad are different colours
        public void AddQuadColour(Color c1, Color c2) => AddQuadColour(c1, c1, c2, c2);

        public void AddQuadColour(Color c1) => AddQuadColour(c1, c1);

        public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) => uvs.AddRange(new[] { uv1, uv2, uv3, uv4 });

        public void AddQuadUV(float uMin, float uMax, float vMin, float vMax) =>
            AddQuadUV(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax));

        public void AddQuadTerrainTypes(Vector3 types) => terrainTypes.AddRange(new[] { types, types, types, types });
    }
}