
namespace DarkDomains
{
    using UnityEngine;
    using System.Collections.Generic;
    
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class HexMesh : MonoBehaviour 
    {
        Mesh hexMesh;

        // these are static as they are temporary buffers, cleared then used for only a given triangulation
        static List<Vector3> vertices = new List<Vector3>();
        static List<int> triangles = new List<int>();
        static List<Color> colours = new List<Color>();

        private void Awake() 
        {
            hexMesh = new Mesh { name = "Hex Mesh" };
            GetComponent<MeshFilter>().mesh = hexMesh;
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

        // triangulates one of the six cores of a hex cell
        // and, if the conditions are met, the bridge and corner on that side
        private void Triangulate(HexDirection direction, HexCell cell)
        {
            var e = new EdgeVertices(
                cell.Position + HexMetrics.GetFirstSolidCorner(direction),
                cell.Position + HexMetrics.GetSecondSolidCorner(direction)
            );

            if (cell.HasRiverThroughEdge(direction))
                e.v3.y = cell.StreamBedY;
                
            if (cell.HasRiver)
                TriangulateWithRiver(direction, cell, e);
            else
                TriangulateEdgeFan(cell.Position, e, cell.Colour);

            if(direction <= HexDirection.SE)
                TriangulateConnection(direction, cell, e);
        }

        private void TriangulateWithRiver(HexDirection direction, HexCell cell, EdgeVertices e1)
        {

        }

        // adds bridges and corner triangles
        private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
        {
            var neighbour = cell.GetNeighbour(direction);
            if (neighbour == null)
                return; // dont add for edge hexes

            var bridge = HexMetrics.GetBridge(direction);
            bridge.y = neighbour.Position.y - cell.Position.y;
            var e2 = new EdgeVertices(
                e1.v1 + bridge,
                e1.v5 + bridge
            );

            if (cell.HasRiverThroughEdge(direction))
                e2.v3.y = neighbour.StreamBedY;

            if (HexMetrics.GetEdgeType(cell.Elevation, neighbour.Elevation) == HexEdgeType.Slope)
                TriangulateEdgeTerrace(e1, cell, e2, neighbour);
            else
                TriangulateEdgeStrip(e1, cell.Colour, e2, neighbour.Colour);

            if(direction > HexDirection.E)
                return;
            var nextDirection = direction.Next();
            var nextNeighbour = cell.GetNeighbour(nextDirection);
            if (nextNeighbour == null)
                return;

            var v5 = e1.v5 + HexMetrics.GetBridge(nextDirection);
            v5.y = nextNeighbour.Position.y;
            
            var minElevation = Mathf.Min(cell.Elevation, neighbour.Elevation, nextNeighbour.Elevation);
            if (minElevation == cell.Elevation)
                TriangulateCorner(e1.v5, cell, e2.v5, neighbour, v5, nextNeighbour);
            else if (minElevation == neighbour.Elevation)
                TriangulateCorner(e2.v5, neighbour, v5, nextNeighbour, e1.v5, cell);
            else
                TriangulateCorner(v5, nextNeighbour, e1.v5, cell, e2.v5, neighbour);
        }

        private void TriangulateEdgeTerrace(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell)
        {
            var es = begin;
            var c1 = beginCell.Colour;
            
            for(var step = 0; step <= HexMetrics.TerraceSteps; step++)
            {
                var ed = EdgeVertices.TerraceLerp(begin, end, step);
                var c2 = HexMetrics.TerraceLerp(beginCell.Colour, endCell.Colour, step);
                TriangulateEdgeStrip(es, c1, ed, c2);
                es = ed; c1 = c2;
            }
        }

        private void TriangulateCorner(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var leftEdge = bottomCell.GetEdgeType(leftCell);
            var rightEdge = bottomCell.GetEdgeType(rightCell);

            if (leftEdge == HexEdgeType.Slope)
            {
                if (rightEdge == HexEdgeType.Slope) // SSF: slope-slope-flat
                    TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                else if (rightEdge == HexEdgeType.Flat) // SFS: slope-flat-slope
                    TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
                else
                    TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            } 
            else if (rightEdge == HexEdgeType.Slope)
            {
                if (leftEdge == HexEdgeType.Flat) // FSS: flat-slope-slope
                    TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                else // must be a cliff, as left as slope has already been covered
                    TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) // both sides are cliffs, top is slope
            {
                if (leftCell.Elevation < rightCell.Elevation)
                    TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell); // CCSR: cliff, cliff, slope to right
                else
                    TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell); // CCSL: cliff, cliff, slope to left
            }
            else // no terraces anywhere, so a simple triangle will do
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

        private void TriangulateCornerTerracesCliff(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var b = Mathf.Abs(1f / (rightCell.Elevation - bottomCell.Elevation));
            var boundary = Vector3.Lerp(Perturb(bottom), Perturb(right), b);
            var boundaryColour = Color.Lerp(bottomCell.Colour, rightCell.Colour, b);

            TriangulteBoundaryTriangle(bottom, bottomCell, left, leftCell, boundary, boundaryColour);
            TriangulateTop(left, leftCell, right, rightCell, boundary, boundaryColour);
        }

        private void TriangulateCornerCliffTerraces(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var b = Mathf.Abs(1f / (leftCell.Elevation - bottomCell.Elevation));
            var boundary = Vector3.Lerp(Perturb(bottom), Perturb(left), b);
            var boundaryColour = Color.Lerp(bottomCell.Colour, leftCell.Colour, b);

            TriangulteBoundaryTriangle(right, rightCell, bottom, bottomCell, boundary, boundaryColour);
            TriangulateTop(left, leftCell, right, rightCell, boundary, boundaryColour);
        }

        private void TriangulateTop(
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell,
            Vector3 boundary, Color boundaryColour)
        {
            if(rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
            {
                TriangulteBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColour);
                return;
            }
            
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColour(leftCell.Colour, rightCell.Colour, boundaryColour);
        }

        private void TriangulteBoundaryTriangle(
            Vector3 bottom, HexCell bottomCell, 
            Vector3 terrace, HexCell terraceCell,
            Vector3 boundary, Color boundaryColour)
        {
            var v1 = Perturb(bottom);
            var c1 = bottomCell.Colour;

            for(var step = 0; step <= HexMetrics.TerraceSteps; step++)
            {
                var v2 = Perturb(HexMetrics.TerraceLerp(bottom, terrace, step));
                var c2 = HexMetrics.TerraceLerp(bottomCell.Colour, terraceCell.Colour, step);

                AddTriangleUnperturbed(v1, v2, boundary);
                AddTriangleColour(c1, c2, boundaryColour);

                v1 = v2; c1 = c2;
            }
        }

        private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
        {
            AddTriangle(center, edge.v1, edge.v2);
            AddTriangleColour(color);
            AddTriangle(center, edge.v2, edge.v3);
            AddTriangleColour(color);
            AddTriangle(center, edge.v3, edge.v4);
            AddTriangleColour(color);
            AddTriangle(center, edge.v4, edge.v5);
            AddTriangleColour(color);
        }

        private void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
        {
            AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            AddQuadColour(c1, c2);
            AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            AddQuadColour(c1, c2);
            AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            AddQuadColour(c1, c2);
            AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            AddQuadColour(c1, c2);
        }

        // adds a new triangle, both adding the vertices to the vertices list, and 
        // adding the three indices (offset by the current count, as this will be the index after the last set of vertices added)
        // to the triangles list
        private void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{v1, v2, v3});
            triangles.AddRange(new[]{index,index+1,index+2});
        }

        private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) =>
            AddTriangleUnperturbed(Perturb(v1), Perturb(v2), Perturb(v3));

        // one colour for triangle
        private void AddTriangleColour(Color c1) => colours.AddRange(new[]{c1, c1, c1});

        // adds colours for each vertex
        private void AddTriangleColour(Color c1, Color c2, Color c3) => colours.AddRange(new[]{c1, c2, c3});

        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var index = vertices.Count;
            vertices.AddRange(new[]{Perturb(v1), Perturb(v2), Perturb(v3), Perturb(v4)});
            triangles.AddRange(new[]{index, index+2, index+1});
            triangles.AddRange(new[]{index+1, index+2, index+3});
        }

        // for when each corner is a different colour
        private void AddQuadColour(Color c1, Color c2, Color c3, Color c4) => colours.AddRange(new[]{c1, c2, c3, c4});

        // for when opposite sides of the quad are different colours
        private void AddQuadColour(Color c1, Color c2) => colours.AddRange(new[]{c1, c1, c2, c2});

        // a key insight with perturb is that the same position will always be perturbed the same amount, due to the fixed noise texture
        // as a result, even though vertices for one triangle are isolated, other triangles will line up as their vertices have the same initial position
        private Vector3 Perturb(Vector3 position)
        {
            var sample = HexMetrics.SampleNoise(position);
            position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
            position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
            // we dont perturb y so that surfaces (hex tops, terrace tops) are flat
            return position;
        }
    }
}