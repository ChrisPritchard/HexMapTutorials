
namespace DarkDomains
{
    using UnityEngine;
    using System.Collections.Generic;
    
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour 
    {
        Mesh hexMesh;
        List<Vector3> vertices;
        List<int> triangles;

        private void Awake() 
        {
            hexMesh = new Mesh { name = "Hex Mesh" };
            GetComponent<MeshFilter>().mesh = hexMesh;

            vertices = new List<Vector3>();
            triangles = new List<int>();
        }

        public void Triangulate(HexCell[] cells)
        {
            hexMesh.Clear();
            vertices.Clear();
            triangles.Clear();

            foreach(var cell in cells)
                Triangulate(cell);

            hexMesh.vertices = vertices.ToArray();
            hexMesh.triangles = triangles.ToArray();
            hexMesh.RecalculateNormals();
        }

        private void Triangulate(HexCell cell)
        {
            var centre = cell.transform.localPosition;
            for (var i = 0; i < 6; i++)
                AddTriangle(centre, centre + HexMetrics.Corners[i], centre + HexMetrics.Corners[(i+1)%6]);
        }

        // adds a new triangle, both adding the vertices to the vertices list, and 
        // adding the three indices (offset by the current count, as this will be the index after the last set of vertices added)
        // to the triangles list
        private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{v1, v2, v3});
            triangles.AddRange(new[]{index,index+1,index+2});
        }
    }
}