
namespace DarkDomains
{
    using UnityEngine;
    using System.Collections.Generic;
    
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class HexMesh : MonoBehaviour 
    {
        Mesh hexMesh;
        MeshCollider collider;

        // these are static as they are temporary buffers, cleared then used for only a given triangulation
        static List<Vector3> vertices = new List<Vector3>();
        static List<int> triangles = new List<int>();
        static List<Color> colours = new List<Color>();

        private void Awake() 
        {
            hexMesh = new Mesh { name = "Hex Mesh" };
            GetComponent<MeshFilter>().mesh = hexMesh;
            collider = GetComponent<MeshCollider>();
        }

        public void Clear()
        {
            hexMesh.Clear();
            vertices.Clear();
            triangles.Clear();
            colours.Clear();
        }

        public void Apply()
        {
            hexMesh.SetVertices(vertices);
            hexMesh.SetTriangles(triangles, 0);
            hexMesh.SetColors(colours);
            hexMesh.RecalculateNormals();
            collider.sharedMesh = hexMesh;
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
    }
}