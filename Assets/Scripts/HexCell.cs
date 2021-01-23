
namespace DarkDomains
{
    using UnityEngine;

    public class HexCell : MonoBehaviour
    {
        public HexGridChunk Chunk;
        public HexCoordinates Coordinates;
        public RectTransform UIRect;

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
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationStep;
                transform.localPosition = position;

                var uiPosition = UIRect.localPosition;
                uiPosition.z = -position.y;
                UIRect.localPosition = uiPosition;

                Refresh();
            }
        }

        public Vector3 Position => transform.localPosition;

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
    }
}