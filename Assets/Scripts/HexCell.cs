
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

        Color colour; // defaults to transparent black (if a colour is transparent is it any real colour??)
        public Color Colour 
        {
            get => colour;
            set
            {
                if (colour == value)
                    return;

                colour = value;
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

                if (hasOutgoingRiver && elevation < GetNeighbour(outgoingRiver).elevation)
                    RemoveOutgoingRiver();
                if (hasIncomingRiver && elevation > GetNeighbour(incomingRiver).elevation)
                    RemoveIncomingRiver();

                for(var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                    if(HasRoadThroughEdge(direction) && GetElevationDifference(direction) > HexMetrics.MaxRoadSlope)
                        SetRoad((int)direction, false);

                Refresh();
            }
        }

        public Vector3 Position => transform.localPosition;

        public float StreamBedY => (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;

        public float RiverSurfaceY => (elevation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep;

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
                foreach(var road in roads)
                    if (road) return true;
                return false;
            }
        }

        public bool HasRoadThroughEdge(HexDirection direction) => roads[(int)direction];

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
            if(!neighbour || neighbour.elevation > elevation)
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
    }
}