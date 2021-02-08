
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexGridChunk : MonoBehaviour 
    {
        HexCell[] cells;
        Canvas canvas;
        
        public HexMesh Terrain, Rivers, Roads, Water, WaterShore, Estuaries;

        public HexFeatureManager Features;

        static Color weights1 = new Color(1f, 0f, 0f);
        static Color weights2 = new Color(0f, 1f, 0f);
        static Color weights3 = new Color(0f, 0f, 1f);

        private void Awake() 
        {
            canvas = GetComponentInChildren<Canvas>();
            cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        }

        public void AddCell(int index, HexCell cell)
        {
            cells[index] = cell;
            cell.transform.SetParent(this.transform, false);
            cell.UIRect.SetParent(canvas.transform, false);
            cell.Chunk = this;
        }

        // chunks are only enabled when they need to triangulate
        public void Refresh() => enabled = true;

        // this method will only be invoked if the chunk is enabled, and will then disable itself
        private void LateUpdate() 
        {
            Triangulate(cells); 
            enabled = false;
        }

        public void ShowUI(bool visible) => canvas.gameObject.SetActive(visible);

        public void Triangulate(HexCell[] cells)
        {
            Terrain.Clear();
            Rivers.Clear();
            Roads.Clear();
            Water.Clear();
            WaterShore.Clear();
            Estuaries.Clear();
            Features.Clear();

            foreach(var cell in cells)
                TriangulateCell(cell);

            Terrain.Apply();
            Rivers.Apply();
            Roads.Apply();
            Water.Apply();
            WaterShore.Apply();
            Estuaries.Apply();
            Features.Apply();
        }

        private void TriangulateCell(HexCell cell)
        {
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                TriangulateCellDirection(d, cell);

            if(!cell.HasRiver && !cell.HasRoads && !cell.IsUnderwater)
                Features.AddFeature(cell, cell.Position);

            if(cell.IsSpecial)
                Features.AddSpecialFeature(cell, cell.Position);
        }

        // triangulates one of the six cores of a hex cell
        // and, if the conditions are met, the bridge and corner on that side
        private void TriangulateCellDirection(HexDirection direction, HexCell cell)
        {
            var e = new EdgeVertices(
                cell.Position + HexMetrics.GetFirstSolidCorner(direction),
                cell.Position + HexMetrics.GetSecondSolidCorner(direction)
            );
                
            if (!cell.IsUnderwater && cell.HasRiver)
            {
                if (cell.HasRiverThroughEdge(direction))
                {
                    e.v3.y = cell.StreamBedY;
                    if(cell.HasRiverBeginOrEnd)
                        TriangulateWithRiverBeginOrEnd(direction, cell, cell.Position, e);
                    else
                        TriangulateWithRiver(direction, cell, cell.Position, e);
                }
                else
                    TriangulateAdjacentToRiver(direction, cell, cell.Position, e);
            }
            else
            {
                TriangulateWithoutRiver(direction, cell, cell.Position, e);

                if(!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
                    Features.AddFeature(cell, (cell.Position + e.v1 + e.v5) * (1f/3));
            }

            if(direction <= HexDirection.SE)
                TriangulateConnection(direction, cell, e);
            
            if(cell.IsUnderwater)
                TriangulateWater(direction, cell, cell.Position);
        }

        private void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 centre, EdgeVertices e)
        {
            TriangulateEdgeFan(cell.Position, e, cell.Index);

            if(cell.HasRoads)
            {
                var interpolators = GetRoadInterpolators(direction, cell);
                TriangulateRoad(centre, 
                    Vector3.Lerp(centre, e.v1, interpolators.x), Vector3.Lerp(centre, e.v5, interpolators.y), 
                    e, cell.HasRoadThroughEdge(direction), cell.Index);
            }
        }

        private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 centre, EdgeVertices e)
        {
            if(cell.HasRoads)
            {
                TriangulateRoadAdjacentToRiver(direction, cell, centre, e);
            }

            if (cell.HasRiverThroughEdge(direction.Next()))
            {
                if(cell.HasRiverThroughEdge(direction.Previous())) // on a curve, so pull back the centre point
                    centre += HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.InnerToOuter * 0.5f;
                else if (cell.HasRiverThroughEdge(direction.Previous2())) // straight connection - pull to one side
                    centre += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
            else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
                centre += HexMetrics.GetSecondSolidCorner(direction) * 0.25f; // other type of straight connection

            var m = new EdgeVertices(
                Vector3.Lerp(centre, e.v1, 0.5f),
                Vector3.Lerp(centre, e.v5, 0.5f));

            TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);
            TriangulateEdgeFan(centre, m, cell.Index);

            if(!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
                Features.AddFeature(cell, (cell.Position + e.v1 + e.v5) * (1f/3));
        }

        private void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 centre, EdgeVertices e)
        {
            var m = new EdgeVertices(
                Vector3.Lerp(centre, e.v1, 0.5f),
                Vector3.Lerp(centre, e.v5, 0.5f));
            m.v3.y = e.v3.y;

            TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);
            TriangulateEdgeFan(centre, m, cell.Index);

            var reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
            centre.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            Rivers.AddTriangle(centre, m.v2, m.v4);
            if (reversed)
                Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            else
                Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
        }

        private void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 centre, EdgeVertices e)
        {
            Vector3 centreL, centreR;
            if (cell.HasRiverThroughEdge(direction.Opposite()))
            {
                centreL = centre + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
                centreR = centre + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
            }
            else if (cell.HasRiverThroughEdge(direction.Next()))
            {
                centreL = centre;
                centreR = Vector3.Lerp(centre, e.v5, 2f/3);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                centreL = Vector3.Lerp(centre, e.v1, 2f/3);
                centreR = centre;
            }
            else if (cell.HasRiverThroughEdge(direction.Next2()))
            {
                centreL = centre;
                centreR = centre + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * 0.5f * HexMetrics.InnerToOuter;
            }
            else
            {
                centreL = centre + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * 0.5f * HexMetrics.InnerToOuter;
                centreR = centre;
            }

            centre = Vector3.Lerp(centreL, centreR, 0.5f); // aligns edges

            var m = new EdgeVertices(
                Vector3.Lerp(centreL, e.v1, 0.5f),
                Vector3.Lerp(centreR, e.v5, 0.5f),
                1f/6);
            m.v3.y = centre.y = e.v3.y;

            TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);

            Terrain.AddTriangle(centreL, m.v1, m.v2);
            Terrain.AddQuad(centreL, centre, m.v2, m.v3);
            Terrain.AddQuad(centre, centreR, m.v3, m.v4);
            Terrain.AddTriangle(centreR, m.v4, m.v5);

            var indices = new Vector3(cell.Index, cell.Index, cell.Index);
            Terrain.AddTriangleCellData(indices, weights1);
            Terrain.AddQuadCellData(indices, weights1);
            Terrain.AddQuadCellData(indices, weights1);
            Terrain.AddTriangleCellData(indices, weights1);

            var reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centreL, centreR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        }

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

            var hasRiver = cell.HasRiverThroughEdge(direction);
            var hasRoad = cell.HasRoadThroughEdge(direction);

            if (hasRiver)
            {
                var startV3 = e2.v3.y;
                e2.v3.y = neighbour.StreamBedY;

                // by definition, both cells have rivers through them
                // however, if both are underwater, then we want (i want) no river bed
                // if only one is, the river should merge with the stream
                // EXCEPT if there is an elevation difference, in which case we need the stream bed to make a waterfall

                // if im a river and the neighbour is a river
                if(!cell.IsUnderwater && !neighbour.IsUnderwater)
                    TriangulateRiverQuad(
                        e1.v2, e1.v4, e2.v2, e2.v4, cell.RiverSurfaceY, neighbour.RiverSurfaceY, 0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == direction);
                // if im a river and the neighbour is beneath me
                else if(!cell.IsUnderwater && cell.Elevation > neighbour.WaterLevel)
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4, 
                        cell.RiverSurfaceY, neighbour.RiverSurfaceY,
                        neighbour.WaterLevel);
                // im underwater but the neighbour is heigher than me
                else if (cell.IsUnderwater && !neighbour.IsUnderwater && neighbour.Elevation > cell.WaterLevel)
                    TriangulateWaterfallInWater(
                            e2.v4, e2.v2, e1.v4, e1.v2, 
                            neighbour.RiverSurfaceY, cell.RiverSurfaceY,
                            cell.WaterLevel);
                else if ((cell.IsUnderwater == neighbour.IsUnderwater == true) // both underwater
                || !cell.IsUnderwater && neighbour.IsUnderwater) // river into water on same level
                    e2.v3.y = startV3; // if a river, this will smooth e1 (river side) into e2 (lake/sea bed, which is normal surface)
                    // if not a river, then e1 is already sea/lake bed, so e2 will now be approximate same level
            }

            if (HexMetrics.GetEdgeType(cell.Elevation, neighbour.Elevation) == HexEdgeType.Slope)
                TriangulateEdgeTerrace(e1, cell, e2, neighbour, hasRoad);
            else
                TriangulateEdgeStrip(e1, weights1, cell.Index, e2, weights2, neighbour.Index, hasRoad);
            
            Features.AddWall(e1, cell, e2, neighbour, hasRiver, hasRoad);

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

        private void TriangulateWaterfallInWater(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
            float y1, float y2, float waterY)
        {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            v1 = HexMetrics.Perturb(v1);
            v2 = HexMetrics.Perturb(v2);
            v3 = HexMetrics.Perturb(v3);
            v4 = HexMetrics.Perturb(v4);
            var t = (waterY - y2) / (y1 - y2);
            v3 = Vector3.Lerp(v3, v1, t);
            v4 = Vector3.Lerp(v4, v2, t);
            Rivers.AddQuadUnperterbed(v1, v2, v3, v4);
            Rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
        }

        private void TriangulateEdgeTerrace(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad)
        {
            var es = begin;
            var w1 = weights1;
            
            for(var step = 0; step <= HexMetrics.TerraceSteps; step++)
            {
                var ed = EdgeVertices.TerraceLerp(begin, end, step);
                var w2 = HexMetrics.TerraceLerp(weights1, weights2, step);
                TriangulateEdgeStrip(
                    es, w1, beginCell.Index, 
                    ed, w2, endCell.Index,
                    hasRoad);

                es = ed; w1 = w2;
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
                Terrain.AddTriangle(bottom, left, right);
                var indices = new Vector3(bottomCell.Index, leftCell.Index, rightCell.Index);
                Terrain.AddTriangleCellData(indices, weights1, weights2, weights3);
            }

            Features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
        }

        private void TriangulateCornerTerraces(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var v1 = bottom;
            var v2 = v1;
            var w1 = weights1;
            var w2 = w1;
            var indices = new Vector3(bottomCell.Index, leftCell.Index, rightCell.Index);

            for(var step = 0; step <= HexMetrics.TerraceSteps; step++)
            {
                var v3 = HexMetrics.TerraceLerp(bottom, left, step);
                var v4 = HexMetrics.TerraceLerp(bottom, right, step);
                var w3 = HexMetrics.TerraceLerp(weights1, weights2, step);
                var w4 = HexMetrics.TerraceLerp(weights1, weights3, step);

                if(step == 0)
                {
                    Terrain.AddTriangle(bottom, v3, v4);
                    Terrain.AddTriangleCellData(indices, weights1, w3, w4);
                    continue;
                }

                Terrain.AddQuad(v1, v2, v3, v4);
                Terrain.AddQuadCellData(indices, w1, w2, w3, w4);

                v1 = v3; v2 = v4; w1 = w3; w2 = w4;
            }
        }

        private void TriangulateCornerTerracesCliff(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var b = Mathf.Abs(1f / (rightCell.Elevation - bottomCell.Elevation));
            var boundary = Vector3.Lerp(HexMetrics.Perturb(bottom), HexMetrics.Perturb(right), b);
            var boundaryWeights = Color.Lerp(weights1, weights3, b);
            var indices = new Vector3(bottomCell.Index, leftCell.Index, rightCell.Index);

            TriangulateBoundaryTriangle(bottom, weights1, left, weights2, boundary, boundaryWeights, indices);
            
            if(rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
                return;
            }
            
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
        }

        private void TriangulateCornerCliffTerraces(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            var b = Mathf.Abs(1f / (leftCell.Elevation - bottomCell.Elevation));
            var boundary = Vector3.Lerp(HexMetrics.Perturb(bottom), HexMetrics.Perturb(left), b);
            var boundaryWeights = Color.Lerp(weights1, weights2, b);
            var indices = new Vector3(bottomCell.Index, leftCell.Index, rightCell.Index);

            TriangulateBoundaryTriangle(right, weights1, bottom, weights2, boundary, boundaryWeights, indices);
            
            if(rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
                return;
            }
            
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
        }

        private void TriangulateBoundaryTriangle(
            Vector3 begin, Color beginWeight, 
            Vector3 terrace, Color terraceWeight,
            Vector3 boundary, Color boundaryWeight,
            Vector3 indices)
        {
            var v1 = HexMetrics.Perturb(begin);
            var c1 = beginWeight;

            for(var step = 0; step <= HexMetrics.TerraceSteps; step++)
            {
                var v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, terrace, step));
                var c2 = HexMetrics.TerraceLerp(beginWeight, terraceWeight, step);

                Terrain.AddTriangleUnperturbed(v1, v2, boundary);
                Terrain.AddTriangleCellData(indices, c1, c2, boundaryWeight);

                v1 = v2; c1 = c2;
            }
        }

        private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
        {
            Terrain.AddTriangle(center, edge.v1, edge.v2);
            Terrain.AddTriangle(center, edge.v2, edge.v3);
            Terrain.AddTriangle(center, edge.v3, edge.v4);
            Terrain.AddTriangle(center, edge.v4, edge.v5);

            var indices = new Vector3(index, index, index);
            Terrain.AddTriangleCellData(indices, weights1);
            Terrain.AddTriangleCellData(indices, weights1);
            Terrain.AddTriangleCellData(indices, weights1);
            Terrain.AddTriangleCellData(indices, weights1);
        }

        private void TriangulateEdgeStrip(EdgeVertices e1, Color w1, float index1, EdgeVertices e2, Color w2, float index2, bool hasRoad = false)
        {
            Terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            Terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            Terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            Terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

            var indices = new Vector3(index1, index2, index1);
            Terrain.AddQuadCellData(indices, w1, w2);
            Terrain.AddQuadCellData(indices, w1, w2);
            Terrain.AddQuadCellData(indices, w1, w2);
            Terrain.AddQuadCellData(indices, w1, w2);

            if(hasRoad)
                TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4, w1, w2, indices);
        }

        private void TriangulateRiverQuad(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, 
            float y, float v,
            bool reversed) => TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);

        private void TriangulateRiverQuad(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, 
            float y1, float y2, float v,
            bool reversed)
        {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            Rivers.AddQuad(v1, v2, v3, v4);
            if (reversed)
                Rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
            else
                Rivers.AddQuadUV(0f, 1f, v, v + 0.2f); // left to right, bottom to top.
        }

        private void TriangulateRoad(
            Vector3 centre, Vector3 ml, Vector3 mr, 
            EdgeVertices e, bool hasRoadThroughEdge, float index)
        {
            if(hasRoadThroughEdge)
            {
                var indices = new Vector3(index, index, index);
                var mc = Vector3.Lerp(ml, mr, 0.5f);
                TriangulateRoadSegment(ml, mc, mr, e.v2, e.v3, e.v4, weights1, weights1, indices);
                Roads.AddTriangle(centre, ml, mc);
                Roads.AddTriangle(centre, mc, mr);
                Roads.AddTriangleUV(new Vector3(1f, 0f), new Vector3(0f, 0f), new Vector3(1f, 0f));
                Roads.AddTriangleUV(new Vector3(1f, 0f), new Vector3(1f, 0f), new Vector3(0f, 0f));
                Roads.AddTriangleCellData(indices, weights1);
                Roads.AddTriangleCellData(indices, weights1);
            }
            else
                TriangulateRoadEdge(centre, ml, mr, index);
        }

        private void TriangulateRoadSegment(
            Vector3 v1, Vector3 v2, Vector3 v3, 
            Vector3 v4, Vector3 v5, Vector3 v6,
            Color w1, Color w2, Vector3 indices)
        {
            Roads.AddQuad(v1, v2, v4, v5);
            Roads.AddQuad(v2, v3, v5, v6);
            Roads.AddQuadUV(0f, 1f, 0f, 0f);
            Roads.AddQuadUV(1f, 0f, 0f, 0f);
            Roads.AddQuadCellData(indices, w1, w2);
            Roads.AddQuadCellData(indices, w1, w2);
        }

        private void TriangulateRoadEdge(Vector3 centre, Vector3 ml, Vector3 mr, float index)
        {
            Roads.AddTriangle(centre, ml, mr);
            Roads.AddTriangleUV(new Vector3(1f, 0f), new Vector3(0f, 0f), new Vector3(0f, 0f));
            var indices = new Vector3(index, index, index);
            Roads.AddTriangleCellData(indices, weights1);
        }

        private void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 centre, EdgeVertices e)
        {
            var edgeRoad = cell.HasRoadThroughEdge(direction);
            var previousRiver = cell.HasRiverThroughEdge(direction.Previous());
            var nextRiver = cell.HasRiverThroughEdge(direction.Next());

            var interpolators = GetRoadInterpolators(direction, cell);
            var roadCentre = centre;

            if(cell.HasRiverBeginOrEnd) // pointy end of rivers
                roadCentre += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * 1f/3;
            else if(cell.IncomingRiver == cell.OutgoingRiver.Opposite()) // straight rivers
            {
                Vector3 corner;
                if(previousRiver)
                {
                    if(!edgeRoad && !cell.HasRoadThroughEdge(direction.Next()))
                        return; // isolated on one side of the river
                    corner = HexMetrics.GetSecondSolidCorner(direction);
                }
                else
                {
                    if(!edgeRoad && !cell.HasRoadThroughEdge(direction.Previous()))
                        return; // isolated on one side of the river
                    corner = HexMetrics.GetFirstSolidCorner(direction);
                }

                roadCentre += corner / 2;
                if(cell.IncomingRiver == direction.Next()
                && (cell.HasRoadThroughEdge(direction.Next2()) || 
                    cell.HasRoadThroughEdge(direction.Opposite())))
                    Features.AddBridge(roadCentre, centre - corner / 2);
                centre += corner / 4;
            }
            else if(cell.IncomingRiver == cell.OutgoingRiver.Previous()) // zigzag river orientation 1
                roadCentre -= HexMetrics.GetSecondSolidCorner(cell.IncomingRiver) * 0.2f;
            else if(cell.IncomingRiver == cell.OutgoingRiver.Next()) // zigzag river orientation 2
                roadCentre -= HexMetrics.GetFirstSolidCorner(cell.IncomingRiver) * 0.2f;
            else if (previousRiver && nextRiver) // inside of curved river
            {
                if(!edgeRoad)
                    return; // isolated road - i.e road didn't come from this edge, and doesn't connect out either.
                var offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.InnerToOuter;
                roadCentre += offset * 0.7f;
                centre += offset / 2;
            }
            else // outside of curved river
            {
                HexDirection middle;
                if(previousRiver) 
                    middle = direction.Next();
                else if(nextRiver) 
                    middle = direction.Previous();
                else 
                    middle = direction;
                if (!cell.HasRoadThroughEdge(middle) && !cell.HasRoadThroughEdge(middle.Previous()) && !cell.HasRoadThroughEdge(middle.Next()))
                    return; // prune orphaned rivers on the inside of a curve
                
                var offset = HexMetrics.GetSolidEdgeMiddle(middle);
                roadCentre += offset / 4;
                if(direction == middle && cell.HasRoadThroughEdge(direction.Opposite()))
                    Features.AddBridge(roadCentre, centre - offset * (HexMetrics.InnerToOuter * 0.7f));
            }

            var ml = Vector3.Lerp(roadCentre, e.v1, interpolators.x);
            var mr = Vector3.Lerp(roadCentre, e.v5, interpolators.y);

            TriangulateRoad(roadCentre, ml, mr, e, edgeRoad, cell.Index);

            if(previousRiver)
                TriangulateRoadEdge(roadCentre, centre, ml, cell.Index);
            if(nextRiver)
                TriangulateRoadEdge(roadCentre, mr, centre, cell.Index);
        }

        // the center of a hex is filled by the road when a road passes through this edge, or when there is a road on each side (so inner turn)
        // for other sides (road on only one side, or no roads on either side and this edge, the center is filled half as much, which lines up with the roads' straight bits)
        private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
        {
            Vector2 interpolators;
            if(cell.HasRoadThroughEdge(direction))
                interpolators.x = interpolators.y = 0.5f;
            else
            {
                interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
                interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
            }
            return interpolators;
        }

        private void TriangulateWater(HexDirection direction, HexCell cell, Vector3 centre)
        {
            centre.y = cell.WaterSurfaceY;
            var neighbour = cell.GetNeighbour(direction);

            if(neighbour != null && !neighbour.IsUnderwater)
                TriangulateWaterShore(direction, cell, neighbour, centre);
            else
                TriangulateOpenWater(direction, cell, neighbour, centre);
        }

        private void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbour, Vector3 centre)
        {
            var e1 = new EdgeVertices(
                centre + HexMetrics.GetFirstWaterCorner(direction),
                centre + HexMetrics.GetSecondWaterCorner(direction)
            );

            Water.AddTriangle(centre, e1.v1, e1.v2);
            Water.AddTriangle(centre, e1.v2, e1.v3);
            Water.AddTriangle(centre, e1.v3, e1.v4);
            Water.AddTriangle(centre, e1.v4, e1.v5);

            var indices = new Vector3(cell.Index, cell.Index, cell.Index);
            Water.AddTriangleCellData(indices, weights1);
            Water.AddTriangleCellData(indices, weights1);
            Water.AddTriangleCellData(indices, weights1);
            Water.AddTriangleCellData(indices, weights1);

            var centre2 = neighbour.Position;
            centre2.y = centre.y;
            var e2 = new EdgeVertices(
                centre2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
                centre2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
            ); // rather than calculating from current centre, work backwards from neighbour centre to find edge

            if(cell.HasRiverThroughEdge(direction))
                TriangulateEstuary(e1, e2, cell.IncomingRiver == direction);
            else
            {
                WaterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
                WaterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
                WaterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
                WaterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
                WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
                WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
                WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
                WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
            }

            var nextNeighbour = cell.GetNeighbour(direction.Next());
            if (nextNeighbour == null)
                return;

            var v3 = nextNeighbour.Position + 
                (nextNeighbour.IsUnderwater ? 
                    HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                    HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.y = centre.y;
            WaterShore.AddTriangle(e1.v5, e2.v5, v3);
            WaterShore.AddTriangleUV(
                new Vector2(0f, 0f), 
                new Vector2(0f, 1f), 
                new Vector2(0f, nextNeighbour.IsUnderwater ? 0f : 1f));
        }

        private void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver)
        {
            WaterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
            WaterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
            WaterShore.AddTriangleUV(
                new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
            );
            WaterShore.AddTriangleUV(
                new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
            );

            Estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
            Estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
            Estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

            Estuaries.AddQuadUV(
                new Vector2(0f, 1f), new Vector2(0f, 0f), 
                new Vector2(1f, 1f), new Vector2(0f, 0f));
            Estuaries.AddTriangleUV(
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            Estuaries.AddQuadUV(
                new Vector2(0f, 0f), new Vector2(0f, 0f), 
                new Vector2(1f, 1f), new Vector2(0f, 1f));

            if(incomingRiver)
            {
                Estuaries.AddQuadUV2(
                    new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f), 
                    new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
                Estuaries.AddTriangleUV2(
                    new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
                Estuaries.AddQuadUV2(
                    new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), 
                    new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
            }
            else
            {
                Estuaries.AddQuadUV2(
                    new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                    new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
                Estuaries.AddTriangleUV2(
                    new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
                Estuaries.AddQuadUV2(
                    new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                    new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
            }
        }

        private void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbour, Vector3 centre)
        {
            var c1 = centre + HexMetrics.GetFirstWaterCorner(direction);
            var c2 = centre + HexMetrics.GetSecondWaterCorner(direction);
            Water.AddTriangle(centre, c1, c2);
            var indices = new Vector3(cell.Index, cell.Index, cell.Index);
            Water.AddTriangleCellData(indices, weights1);

            if(neighbour == null || direction > HexDirection.SE)
                return;
                
            var bridge = HexMetrics.GetWaterBridge(direction);
            var e1 = c1 + bridge;
            var e2 = c2 + bridge;
            
            Water.AddQuad(c1, c2, e1, e2);
            indices.y = neighbour.Index;
            Water.AddQuadCellData(indices, weights1, weights2);

            if(direction > HexDirection.E)
                return;

            var nextNeighbour = cell.GetNeighbour(direction.Next());
            if(nextNeighbour == null || !nextNeighbour.IsUnderwater)
                return;

            Water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
            indices.z = nextNeighbour.Index;
            Water.AddTriangleCellData(indices, weights1, weights2, weights3);
        }
    }
}