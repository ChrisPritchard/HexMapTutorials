
namespace DarkDomains
{
    using UnityEngine;

    public class HexCell : MonoBehaviour
    {
        public HexCoordinates Coordinates;

        public Color Colour;

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