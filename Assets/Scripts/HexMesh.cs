
namespace DarkDomains
{
    using UnityEngine;
    using System.Collections.Generic;
    
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class HexMesh : MonoBehaviour 
    {
        Mesh hexMesh;
        List<Vector3> vertices;
        List<int> triangles;
        List<Color> colours;

        private void Awake() 
        {
            hexMesh = new Mesh { name = "Hex Mesh" };
            GetComponent<MeshFilter>().mesh = hexMesh;

            vertices = new List<Vector3>();
            triangles = new List<int>();
            colours = new List<Color>();
        }

        public void Triangulate(HexCell[] cells)
        {
            hexMesh.Clear();
            vertices.Clear();
            triangles.Clear();
            colours.Clear();

            foreach(var cell in cells)
                Triangulate(cell);

            hexMesh.vertices = vertices.ToArray();
            hexMesh.triangles = triangles.ToArray();
            hexMesh.colors = colours.ToArray();

            hexMesh.RecalculateNormals();

            GetComponent<MeshCollider>().sharedMesh = hexMesh;
        }

        private void Triangulate(HexCell cell)
        {
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                Triangulate(d, cell);
        }

        private void Triangulate(HexDirection direction, HexCell cell)
        {
            var centre = cell.transform.localPosition;
            AddTriangle(
                centre, 
                centre + HexMetrics.GetFirstCorner(direction), 
                centre + HexMetrics.GetSecondCorner(direction));
            var neighbour = cell.GetNeighbour(direction) ?? cell;
            var blendColour = (cell.Colour + neighbour.Colour) / 2f;
            AddTriangleColour(cell.Colour, blendColour, blendColour);
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

        // adds colours for each vertex
        private void AddTriangleColour(Color c1, Color c2, Color c3) => colours.AddRange(new[]{c1, c2, c3});
    }
}