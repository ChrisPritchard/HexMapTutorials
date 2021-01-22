
namespace DarkDomains
{
    using UnityEngine;

    public class HexCell : MonoBehaviour
    {
        public HexCoordinates Coordinates;

        public Color Colour;

        public RectTransform UIRect;

        int elevation;
        public int Elevation
        {
            get => elevation;
            set
            {
                elevation = value;

                var position = transform.localPosition;
                position.y = elevation * HexMetrics.ElevationStep;
                transform.localPosition = position;

                position = UIRect.localPosition;
                position.z = elevation * -HexMetrics.ElevationStep;
                UIRect.localPosition = position;
            }
        }

        [SerializeField]
        public HexCell[] Neighbours;

        public HexCell GetNeighbour(HexDirection direction) => Neighbours[(int)direction];

        public void SetNeighbour(HexDirection direction, HexCell cell)
        {
            Neighbours[(int)direction] = cell;
            cell.Neighbours[(int)direction.Opposite()] = this;
        }
    }
}