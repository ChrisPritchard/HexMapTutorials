
namespace DarkDomains
{
    using UnityEngine;

    public class HexCell : MonoBehaviour
    {
        public HexGridChunk Chunk;
        public HexCoordinates Coordinates;
        public RectTransform UIRect;

        bool hasIncomingRiver, hasOutgoingRiver;
        HexDirection incomingRiver, outgoingRiver;

        [SerializeField]
        bool[] roads;

        bool walled;

        public Vector3 Position => transform.localPosition;

        float terrainTypeIndex;
        public float TerrainTypeIndex
        {
            get => terrainTypeIndex;
            set
            {
                if (terrainTypeIndex == value)
                    return;

                terrainTypeIndex = value;
                Refresh();
            }
        }

        int elevation = int.MinValue;
        public int Elevation
        {
            get => elevation;
            set
            {
                if(value == elevation)
                    return;

                elevation = value;

                var position = transform.localPosition;
                position.y = elevation * HexMetrics.ElevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
                transform.localPosition = position;

                var uiPosition = UIRect.localPosition;
                uiPosition.z = -position.y;
                UIRect.localPosition = uiPosition;

                ValidateRivers();

                for(var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                    if(HasRoadThroughEdge(direction) && GetElevationDifference(direction) > HexMetrics.MaxRoadSlope)
                        SetRoad((int)direction, false);

                Refresh();
            }
        }

        private int waterLevel;
        public int WaterLevel
        {
            get => waterLevel;
            set
            {
                if (waterLevel == value)
                    return;
                waterLevel = value;
                ValidateRivers();
                Refresh();
            }
        }

        public bool IsUnderwater => waterLevel > elevation;

        int urbanLevel, farmLevel, forestLevel;

        public int UrbanLevel
        {
            get => urbanLevel;
            set
            {
                if (urbanLevel == value)
                    return;

                urbanLevel = value;
                Refresh();
            }
        }

        public int FarmLevel
        {
            get => farmLevel;
            set
            {
                if (farmLevel == value)
                    return;

                farmLevel = value;
                Refresh();
            }
        }

        public int ForestLevel
        {
            get => forestLevel;
            set
            {
                if (forestLevel == value)
                    return;

                forestLevel = value;
                Refresh();
            }
        }
        public bool Walled
        {
            get => walled;
            set
            {
                if (walled == value)
                    return;

                walled = value;
                Refresh();
            }
        }

        public float StreamBedY => (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;

        public float RiverSurfaceY => (elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;

        public float WaterSurfaceY => (waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;

        public bool HasIncomingRiver => hasIncomingRiver;
        public bool HasOutgoingRiver => hasOutgoingRiver;
        public HexDirection IncomingRiver => incomingRiver;
        public HexDirection OutgoingRiver => outgoingRiver;
        public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;
        public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;
        public HexDirection RiverBeginOrEndDirection => hasIncomingRiver ? incomingRiver : outgoingRiver;

        public bool HasRoads
        {
            get
            {
                if (IsUnderwater)
                    return false;
                foreach(var road in roads)
                    if (road) return true;
                return false;
            }
        }

        public bool HasRoadThroughEdge(HexDirection direction)
        {
            if (IsUnderwater || (GetNeighbour(direction) != null && GetNeighbour(direction).IsUnderwater))
                return false;
            return roads[(int)direction];
        }

        public void RemoveRoad()
        {
            for(var i = 0; i < Neighbours.Length; i++) 
            {
                if(!roads[i])
                    continue;
                SetRoad(i, false);
            }
        }

        public void AddRoad(HexDirection direction)
        {
            if(!roads[(int)direction] && !HasRiverThroughEdge(direction) 
                && GetElevationDifference(direction) <= HexMetrics.MaxRoadSlope)
                SetRoad((int)direction, true);
        }

        private void SetRoad(int index, bool state)
        {
            var neighbour = Neighbours[index];
            if(!neighbour) 
                return;
            roads[index] = state;
            neighbour.roads[(int)((HexDirection)index).Opposite()] = state;
            neighbour.RefreshSelfOnly();
            RefreshSelfOnly();
        }

        public int GetElevationDifference(HexDirection direction) => Mathf.Abs(elevation - GetNeighbour(direction).elevation);

        [SerializeField]
        public HexCell[] Neighbours;

        public HexCell GetNeighbour(HexDirection direction) => Neighbours[(int)direction];

        public void SetNeighbour(HexDirection direction, HexCell cell)
        {
            Neighbours[(int)direction] = cell;
            cell.Neighbours[(int)direction.Opposite()] = this;
        }

        public HexEdgeType GetEdgeType(HexCell other) => HexMetrics.GetEdgeType(elevation, other.elevation);

        public void Refresh()
        {
            if(!Chunk) 
                return;

            Chunk.Refresh();
            foreach(var neighbour in Neighbours)
                if(neighbour != null && neighbour.Chunk != Chunk)
                    neighbour.Chunk.Refresh();
        }

        public void RefreshSelfOnly() => Chunk?.Refresh();

        public bool HasRiverThroughEdge(HexDirection direction) =>
            (hasIncomingRiver && incomingRiver == direction) || (hasOutgoingRiver && outgoingRiver == direction);

        public void RemoveOutgoingRiver()
        {
            if (!hasOutgoingRiver)
                return;
            hasOutgoingRiver = false;
            RefreshSelfOnly();

            var neighbour = GetNeighbour(outgoingRiver);
            neighbour.hasIncomingRiver = false;
            neighbour.RefreshSelfOnly();
        }

        public void RemoveIncomingRiver()
        {
            if (!hasIncomingRiver)
                return;
            hasIncomingRiver = false;
            RefreshSelfOnly();

            var neighbour = GetNeighbour(incomingRiver);
            neighbour.hasOutgoingRiver = false;
            neighbour.RefreshSelfOnly();
        }

        public void RemoveRiver()
        {
            RemoveOutgoingRiver();
            RemoveIncomingRiver();
        }

        public void SetOutgoingRiver(HexDirection direction)
        {
            if (hasOutgoingRiver && outgoingRiver == direction)
                return;

            var neighbour = GetNeighbour(direction);
            if(!IsValidRiverDestination(neighbour))
                return;

            RemoveOutgoingRiver(); // clear existing, if it exists
            if (hasIncomingRiver && incomingRiver == direction)
                RemoveIncomingRiver();
            
            outgoingRiver = direction;
            hasOutgoingRiver = true;

            neighbour.RemoveIncomingRiver();
            neighbour.hasIncomingRiver = true;
            neighbour.incomingRiver = direction.Opposite();

            SetRoad((int)direction, false); // this will also refresh this cell
        }

        private bool IsValidRiverDestination(HexCell neighbour) =>
            neighbour && (elevation >= neighbour.elevation || waterLevel == neighbour.elevation);

        private void ValidateRivers()
        {
            if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbour(outgoingRiver)))
                RemoveOutgoingRiver();
            if (hasIncomingRiver && !GetNeighbour(incomingRiver).IsValidRiverDestination(this))
                RemoveIncomingRiver();
        }
    }
}