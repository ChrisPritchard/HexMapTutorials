
namespace DarkDomains
{
    using UnityEngine;
    
    public enum HexDirection { NE, E, SE, SW, W, NW }
    
    public enum HexEdgeType { Flat, Slope, Cliff }

    public static class HexMetrics
    {
        public const float OuterRadius = 10f; // distance from centre to any point. also distance between points

        public const float InnerRadius = OuterRadius * 0.866025404f; // distance from centre to any edge

        public const float SolidFactor = 0.8f; // unblended core percent of a tile

        public const float BlendFactor = 1f - SolidFactor; // blended with neighbours percent of a tile

        public const float ElevationStep = 3f; // how much the elevation value of a cell changes its height

        public const int TerracesPerSlope = 2;

        public const int TerraceSteps = TerracesPerSlope * 2 + 1; // this is the side and top of each terrace, plus the final side

        public const float HorizontalTerraceStepSize = 1f / TerraceSteps;

        public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

        public static float CellPerturbStrength = 0f;//4f;

        public static float ElevationPerturbStrength = 0f;//1.5f;

        public static float NoiseScale = 0.003f;

        public static Texture2D NoiseSource;

        public const int ChunkSizeX = 5, ChunkSizeZ = 5; // this can be tweaked, but note that the full map will always be a multiple of theses

        public const float StreamBedElevationOffset = -1f;

        // hex points, pointy-top, with half above and half below 0 on the Z access
        // coords are in XYZ, but Z is as Y in this, with Y always 0, in order to align 
        // with the 3D plain in Unity. coords are clockwise from top.
        private static readonly Vector3[] corners = new Vector3[]
        {
            new Vector3(0f,             0f,     OuterRadius), // top
            new Vector3(InnerRadius,    0f,     OuterRadius/2), // top-right
            new Vector3(InnerRadius,    0f,     -OuterRadius/2), // bottom-right
            new Vector3(0f,             0f,     -OuterRadius), // bottom
            new Vector3(-InnerRadius,   0f,     -OuterRadius/2), // bottom-left
            new Vector3(-InnerRadius,   0f,     OuterRadius/2), // top-left
            new Vector3(0f,             0f,     OuterRadius), // top repeated, so modulus isn't required in some places
        };

        public static HexDirection Opposite(this HexDirection direction) => (HexDirection)(((int)direction + 3) % 6);

        public static HexDirection Previous(this HexDirection direction) => direction == HexDirection.NE ? HexDirection.NW : direction - 1;

        public static HexDirection Next(this HexDirection direction) => direction == HexDirection.NW ? HexDirection.NE : direction + 1;

        public static Vector3 GetFirstCorner(HexDirection direction) => corners[(int)direction];

        public static Vector3 GetSecondCorner(HexDirection direction) => corners[(int)direction + 1];

        public static Vector3 GetFirstSolidCorner(HexDirection direction) => corners[(int)direction] * SolidFactor;

        public static Vector3 GetSecondSolidCorner(HexDirection direction) => corners[(int)direction + 1] * SolidFactor;

        public static Vector3 GetBridge(HexDirection direction) => (GetFirstCorner(direction) + GetSecondCorner(direction)) * BlendFactor;

        // returns the point between a and b for the given terrace step
        // the lerp function is: for t = 0, a; for t = 1, b; for t between 0 and 1, a + (b - a) * t, so t times the distance between the two
        // here, horizontal is easy as each step, whether sloped or flat, is the same distance
        // vertical is harder, slightly, as every second step its 0
        public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
        {
            var h = step * HorizontalTerraceStepSize;
            a.x += (b.x - a.x) * h;
            a.z += (b.z - a.z) * h;
            var v = (step + 1) / 2 * VerticalTerraceStepSize;
            a.y += (b.y - a.y) * v;
            return a;
        }

        public static Color TerraceLerp(Color a, Color b, int step)
        {
            var h = step * HorizontalTerraceStepSize;
            return Color.Lerp(a, b, h);
        }

        public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
        {
            if (elevation1 == elevation2) return HexEdgeType.Flat;
            if (Mathf.Abs(elevation1 - elevation2) == 1) return HexEdgeType.Slope;
            return HexEdgeType.Cliff;
        }

        public static Vector4 SampleNoise(Vector3 position) => 
            NoiseSource.GetPixelBilinear(position.x * NoiseScale, position.z * NoiseScale);
    }
}