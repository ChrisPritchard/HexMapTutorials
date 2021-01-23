
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
            AddTriangleColour(cell.Colour);

            if(direction <= HexDirection.SE)
                TriangulateConnection(direction, cell, v1, v2);
        }

        // adds bridges and corner triangles
        private void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
        {
            var neighbour = cell.GetNeighbour(direction);
            if (neighbour == null)
                return; // dont add for edge hexes

            var bridge = HexMetrics.GetBridge(direction);
            var v3 = v1 + bridge;
            var v4 = v2 + bridge;
            v3.y = v4.y = neighbour.Elevation * HexMetrics.ElevationStep;

            if (HexMetrics.GetEdgeType(cell.Elevation, neighbour.Elevation) == HexEdgeType.Slope)
                TriangulateEdgeTerrace(v1, v2, cell, v3, v4, neighbour);
            else
            {
                AddQuad(v1, v2, v3, v4);
                AddQuadColour(cell.Colour, neighbour.Colour);
            }

            if(direction > HexDirection.E)
                return;

            var nextDirection = direction.Next();
            var nextNeighbour = cell.GetNeighbour(nextDirection);
            if (nextNeighbour == null)
                return;

            var v5 = v2 + HexMetrics.GetBridge(nextDirection);
            v5.y = nextNeighbour.Elevation * HexMetrics.ElevationStep;
            
            var minElevation = Mathf.Min(cell.Elevation, neighbour.Elevation, nextNeighbour.Elevation);
            if (minElevation == cell.Elevation)
                TriangulateCorner(v2, cell, v4, neighbour, v5, nextNeighbour);
            else if (minElevation == neighbour.Elevation)
                TriangulateCorner(v4, neighbour, v5, nextNeighbour, v2, cell);
            else
                TriangulateCorner(v5, nextNeighbour, v2, cell, v4, neighbour);
        }

        private void TriangulateEdgeTerrace(
            Vector3 beginLeft, Vector3 beginRight, HexCell beginCell, 
            Vector3 endLeft, Vector3 endRight, HexCell endCell)
        {
            var v1 = beginLeft;
            var v2 = beginRight;
            var c1 = beginCell.Colour;
            for(var step = 0; step <= HexMetrics.TerraceSteps; step++)
            {
                var v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, step);
                var v4 = HexMetrics.TerraceLerp(beginRight, endRight, step);
                var c2 = HexMetrics.TerraceLerp(beginCell.Colour, endCell.Colour, step);
                AddQuad(v1, v2, v3, v4);
                AddQuadColour(c1, c2);
                v1 = v3; v2 = v4; c1 = c2;
            }
        }

        private void TriangulateCorner(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var leftEdge = bottomCell.GetEdgeType(leftCell);
            var rightEdge = bottomCell.GetEdgeType(rightCell);

            if (leftEdge == HexEdgeType.Slope && rightEdge == HexEdgeType.Slope)
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            else if (leftEdge == HexEdgeType.Slope && rightEdge == HexEdgeType.Flat)
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            else if (leftEdge == HexEdgeType.Flat && rightEdge == HexEdgeType.Slope)
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            else
            {
                AddTriangle(bottom, left, right);
                AddTriangleColour(bottomCell.Colour, leftCell.Colour, rightCell.Colour);
            }
        }

        private void TriangulateCornerTerraces(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var v1 = bottom;
            var v2 = v1;
            var c1 = bottomCell.Colour;
            var c2 = c1;

            for(var step = 0; step <= HexMetrics.TerraceSteps; step++)
            {
                var v3 = HexMetrics.TerraceLerp(bottom, left, step);
                var v4 = HexMetrics.TerraceLerp(bottom, right, step);
                var c3 = HexMetrics.TerraceLerp(bottomCell.Colour, leftCell.Colour, step);
                var c4 = HexMetrics.TerraceLerp(bottomCell.Colour, rightCell.Colour, step);

                if(step == 0)
                {
                    AddTriangle(bottom, v3, v4);
                    AddTriangleColour(bottomCell.Colour, c3, c4);
                    continue;
                }

                AddQuad(v1, v2, v3, v4);
                AddQuadColour(c1, c2, c3, c4);
                v1 = v3; v2 = v4; c1 = c3; c2 = c4;
            }
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

        // one colour for triangle
        private void AddTriangleColour(Color c1) => colours.AddRange(new[]{c1, c1, c1});

        // adds colours for each vertex
        private void AddTriangleColour(Color c1, Color c2, Color c3) => colours.AddRange(new[]{c1, c2, c3});

        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{v1, v2, v3, v4});
            triangles.AddRange(new[]{index, index+2, index+1});
            triangles.AddRange(new[]{index+1, index+2, index+3});
        }

        // for when each corner is a different colour
        private void AddQuadColour(Color c1, Color c2, Color c3, Color c4) => colours.AddRange(new[]{c1, c2, c3, c4});

        // for when opposite sides of the quad are different colours
        private void AddQuadColour(Color c1, Color c2) => colours.AddRange(new[]{c1, c1, c2, c2});
    }
}