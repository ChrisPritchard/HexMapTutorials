
namespace DarkDomains
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    
    [Serializable]
    public class HexFeatureCollection
    {

        public Transform[] Prefabs;

        // choice is 0 <= N < 1
        public Transform Pick(float choice) => Prefabs[(int)(choice * Prefabs.Length)];
    }

    public class HexFeatureManager : MonoBehaviour 
    {
        public HexMesh Walls;

        public Transform WallTowerPrefab, BridgePrefab;

        public Transform[] SpecialFeatures;
        
        public HexFeatureCollection[] UrbanPrefabs, FarmPrefabs, ForestPrefabs;

        Transform container;

        public void Clear() 
        { 
            if(container)
                Destroy(container.gameObject);
            container = new GameObject("Features Container").transform;
            container.SetParent(this.transform, false);

            Walls.Clear();
        }

        public void Apply() 
        { 
            Walls.Apply();
        }

        public void AddFeature (HexCell cell, Vector3 position) 
        { 
            if(cell.IsSpecial)
                return;
            var hash = HexMetrics.SampleHashGrid(position);

            var prefab = PickPrefab(cell, hash);
            if(!prefab)
                return;

            var instance = Instantiate(prefab);
            position.y += instance.localScale.y /2;
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.E, 0f);
            instance.SetParent(container, false);
        }

        private Transform PickPrefab(HexCell cell, HexHash hash)
        {
            var options = new Dictionary<float, Transform>
            {
                [hash.A] = PickPrefab(UrbanPrefabs, cell.UrbanLevel, hash.A, hash.D),
                [hash.B] = PickPrefab(FarmPrefabs, cell.FarmLevel, hash.B, hash.D),
                [hash.C] = PickPrefab(ForestPrefabs, cell.ForestLevel, hash.C, hash.D)
            };
            
            var odds = new[] {hash.A, hash.B, hash.C};
            Array.Sort(odds);
            foreach(var o in odds)
                if(options[o])
                    return options[o];
            return null;
        }

        private Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
        {
            if(level > 0)
            {
                var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
                for (var i = 0; i < thresholds.Length; i++)
                    if(hash < thresholds[i])
                        return collection[i].Pick(choice);
            }
            return null;
        }

        public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, bool hasRiver, bool hasRoad)
        { 
            if(nearCell.Walled != farCell.Walled
            && !nearCell.IsUnderwater && !farCell.IsUnderwater
            && nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
            {
                AddWallSegment(near.v1, far.v1, near.v2, far.v2);
                if (hasRiver || hasRoad)
                {
                    AddWallCap(near.v2, far.v2);
                    AddWallCap(far.v4, near.v4);
                }
                else
                {
                    AddWallSegment(near.v2, far.v2, near.v3, far.v3);
                    AddWallSegment(near.v3, far.v3, near.v4, far.v4);
                }
                AddWallSegment(near.v4, far.v4, near.v5, far.v5);

                foreach(var neighbour in nearCell.Neighbours)
                    if(neighbour == null) // edge of map
                    {
                        AddWallCap(far.v1, near.v1);
                        AddWallCap(near.v5, far.v5);
                        return;
                    }
            }
        }

        public void AddWall(Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3)
        { 
            var state = (cell1.Walled, cell2.Walled, cell3.Walled);
            if (state == (true, true, false) || state == (false, false, true))
                AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
            else if (state == (false, true, false) || state == (true, false, true))
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            else if (state == (true, false, false) || state == (false, true, true))
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
        }

        private void AddWallSegment(Vector3 pivot, HexCell pivotCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
        {
            if(pivotCell.IsUnderwater)
                return;

            var leftWall = !leftCell.IsUnderwater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
            var rightWall = !rightCell.IsUnderwater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

            if(leftWall && rightWall)
            {
                var hasWall = false;
                if(leftCell.Elevation == rightCell.Elevation)
                {
                    var hash = HexMetrics.SampleHashGrid((pivot + left + right) / 3);
                    hasWall = hash.E < HexMetrics.WallTowerThreashold;
                }
                AddWallSegment(pivot, left, pivot, right, hasWall);
            }
            if(leftWall && !rightWall)
                if (leftCell.Elevation < rightCell.Elevation)
                    AddWallWedge(pivot, left, right);
                else
                    AddWallCap(pivot, left);
            if (rightWall && !leftWall)
                if (rightCell.Elevation < leftCell.Elevation)
                    AddWallWedge(right, pivot, left);
                else
                    AddWallCap(right, pivot);

        }

        private void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight, bool addTower = false)
        {
            nearLeft = HexMetrics.Perturb(nearLeft);
            farLeft = HexMetrics.Perturb(farLeft);
            nearRight = HexMetrics.Perturb(nearRight);
            farRight = HexMetrics.Perturb(farRight);

            var left = HexMetrics.WallLerp(nearLeft, farLeft);
            var right = HexMetrics.WallLerp(nearRight, farRight);

            var leftTop = left.y + HexMetrics.WallHeight;
            var rightTop = right.y + HexMetrics.WallHeight;

            var leftThickness = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
            var rightThickness = HexMetrics.WallThicknessOffset(nearRight, farRight);

            Vector3 v1, v2, v3, v4;
            v1 = v3 = left - leftThickness;
            v2 = v4 = right - rightThickness;
            v3.y = leftTop;
            v4.y = rightTop;
            Walls.AddQuadUnperterbed(v1, v2, v3, v4);

            Vector3 t1 = v3, t2 = v4; // first two top vertices

            v1 = v3 = left + leftThickness;
            v2 = v4 = right + rightThickness;
            v3.y = leftTop;
            v4.y = rightTop;
            Walls.AddQuadUnperterbed(v2, v1, v4, v3);

            Walls.AddQuadUnperterbed(t1, t2, v3, v4); // top

            if(!addTower)
                return;

            var towerInstance = Instantiate(WallTowerPrefab);
            towerInstance.transform.localPosition = (left + right) / 2;

            var rightDirection = right - left;
            rightDirection.y = 0;
            towerInstance.transform.right = rightDirection;

            towerInstance.SetParent(container, false);
        }

        private void AddWallCap(Vector3 near, Vector3 far)
        {
            near = HexMetrics.Perturb(near);
            far = HexMetrics.Perturb(far);

            var centre = HexMetrics.WallLerp(near, far);
            var thickness = HexMetrics.WallThicknessOffset(near, far);

            Vector3 v1, v2, v3, v4;
            v1 = v3 = centre - thickness;
            v2 = v4 = centre + thickness;
            v3.y = v4.y = centre.y + HexMetrics.WallHeight;

            Walls.AddQuadUnperterbed(v1, v2, v3, v4);
        }

        private void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
        {
            near = HexMetrics.Perturb(near);
            far = HexMetrics.Perturb(far);
            point = HexMetrics.Perturb(point);

            var centre = HexMetrics.WallLerp(near, far);
            var thickness = HexMetrics.WallThicknessOffset(near, far);

            Vector3 v1, v2, v3, v4;
            var pointTop = point;
            point.y = centre.y;

            v1 = v3 = centre - thickness;
            v2 = v4 = centre + thickness;
            v3.y = v4.y = pointTop.y = centre.y + HexMetrics.WallHeight;

            Walls.AddQuadUnperterbed(v1, point, v3, pointTop);
            Walls.AddQuadUnperterbed(point, v2, pointTop, v4);
            Walls.AddTriangleUnperturbed(pointTop, v3, v4);
        }

        public void AddBridge(Vector3 roadCentre1, Vector3 roadCentre2)
        {
            roadCentre1 = HexMetrics.Perturb(roadCentre1);
            roadCentre2 = HexMetrics.Perturb(roadCentre2);

            var instance = Instantiate(BridgePrefab);
            instance.localPosition = (roadCentre1 + roadCentre2) / 2;
            instance.forward = roadCentre1 - roadCentre2; // rotate appropriatly

            var length = Vector3.Distance(roadCentre1, roadCentre2);
            instance.localScale = new Vector3(1f, 1f, length * (1f / HexMetrics.BridgeDesignLength));

            instance.SetParent(container, false);
        }

        public void AddSpecialFeature(HexCell cell, Vector3 position)
        {
            var instance = Instantiate(SpecialFeatures[(int)cell.SpecialFeatureIndex-1]);
            instance.localPosition = HexMetrics.Perturb(position);
            var hash = HexMetrics.SampleHashGrid(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.E, 0f);
            instance.SetParent(container, false);
        }
    }
}