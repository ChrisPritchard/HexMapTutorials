
namespace DarkDomains
{
    using System;
    using UnityEngine;

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

        public override string ToString() => "(" + ToString(", ") + ")";

        public string ToString(string sep) => X + sep + Y + sep + Z;

        public static HexCoordinates FromOffsetCoordinates(int x, int z) => new HexCoordinates(x-z/2, z);
        public static HexCoordinates FromPosition(Vector3 position)
        {
            var x = position.x / (HexMetrics.InnerRadius * 2f);
            var y = -x;
            var offset = position.z / (HexMetrics.OuterRadius * 3f);
            x -= offset;
            y -= offset;

            var iX = Mathf.RoundToInt(x);
            var iY = Mathf.RoundToInt(y);
            var iZ = Mathf.RoundToInt(-x - y);

            if (iX + iY + iZ != 0)
            {
                var dX = Mathf.Abs(x - iX);
                var dY = Mathf.Abs(y - iY);
                var dZ = Mathf.Abs(-x -y - iZ);

                if (dX > dY && dX > dZ)
                    iX = -iY - iZ;
                else if (dZ > dY)
                    iZ = -iX - iY;
            }

            return new HexCoordinates(iX, iZ);
        }
    }
}