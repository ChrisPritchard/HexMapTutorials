
namespace DarkDomains
{
    using System;

    [Serializable]
    public struct HexCoordinates
    {
        public int X { get; private set; }
        public int Y => -X - Z;
        public int Z { get; private set; }

        public HexCoordinates(int x, int z)
        {
            X = x;
            Z = z;
        }

        public static HexCoordinates FromOffsetCoordinates(int x, int z) => new HexCoordinates(x-z/2, z);

        public override string ToString() => ToString(", ");

        public string ToString(string sep) => X + sep + Y + sep + Z;
    }
}