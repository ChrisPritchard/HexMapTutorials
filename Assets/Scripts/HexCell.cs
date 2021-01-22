
namespace DarkDomains
{
    using UnityEngine;

    public enum HexDirection { NE, E, SE, SW, W, NW }

    public static class HexDirectionExtensions
    {
        public static HexDirection Opposite(this HexDirection direction) => (HexDirection)(((int)direction + 3) % 6);
    }

    public class HexCell : MonoBehaviour
    {
        public HexCoordinates Coordinates;

        public Color Colour;

        [SerializeField]
        public HexCell[] Neighbours;

        public HexCell GetNeighbour(HexDirection direction) => Neighbours[(int)direction] ?? this;

        public void SetNeighbour(HexDirection direction, HexCell cell)
        {
            Neighbours[(int)direction] = cell;
            cell.Neighbours[(int)direction.Opposite()] = this;
        }
    }
}