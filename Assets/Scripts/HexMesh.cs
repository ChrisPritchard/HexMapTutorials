
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

            var v1 = centre + HexMetrics.GetFirstSolidCorner(direction);
            var v2 = centre + HexMetrics.GetSecondSolidCorner(direction);

            AddTriangle(centre, v1, v2);
            AddTriangleColour(cell.Colour, cell.Colour, cell.Colour);

            var v3 = centre + HexMetrics.GetFirstCorner(direction);
            var v4 = centre + HexMetrics.GetSecondCorner(direction);

            AddQuad(v1, v2, v3, v4);

            var prevNeighbour = cell.GetNeighbour(direction.Previous()) ?? cell;
            var neighbour = cell.GetNeighbour(direction) ?? cell;
            var nextNeighbour = cell.GetNeighbour(direction.Next()) ?? cell;

            var blendColour1 = (cell.Colour + neighbour.Colour + prevNeighbour.Colour) / 3f;
            var blendColour2 = (cell.Colour + neighbour.Colour + nextNeighbour.Colour) / 3f;

            AddQuadColour(cell.Colour, cell.Colour, blendColour1, blendColour2);
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

        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{v1, v2, v3, v4});
            triangles.AddRange(new[]{index, index+2, index+1});
            triangles.AddRange(new[]{index+1, index+2, index+3});
        }

        private void AddQuadColour(Color c1, Color c2, Color c3, Color c4) => colours.AddRange(new[]{c1, c2, c3, c4});
    }
}