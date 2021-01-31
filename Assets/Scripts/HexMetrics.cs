
namespace DarkDomains
{
    using UnityEngine;
    
    public enum HexDirection { NE, E, SE, SW, W, NW }
    
    public enum HexEdgeType { Flat, Slope, Cliff }

    public struct HexHash
    {
        public float A, B, C, D, E;
        
        private static float rv() => Random.value * 0.999f; // precludes 1.0 from occuring

        public static HexHash Create() => 
            new HexHash { A = rv(), B = rv(), C = rv(), D = rv(), E = rv() };
    }

    public static class HexMetrics
    {
        public const float OuterToInner = 0.866025404f;
        public const float InnerToOuter = 1f / OuterToInner;

        public const float OuterRadius = 10f; // distance from centre to any point. also distance between points

        public const float InnerRadius = OuterRadius * OuterToInner; // distance from centre to any edge

        public const float SolidFactor = 0.8f; // unblended core percent of a tile

        public const float BlendFactor = 1f - SolidFactor; // blended with neighbours percent of a tile

        public const float ElevationStep = 3f; // how much the elevation value of a cell changes its height

        public const int TerracesPerSlope = 2;

        public const int TerraceSteps = TerracesPerSlope * 2 + 1; // this is the side and top of each terrace, plus the final side

        public const float HorizontalTerraceStepSize = 1f / TerraceSteps;

        public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

        public static float CellPerturbStrength = 4f;

        public static float ElevationPerturbStrength = 1.5f;

        public static float NoiseScale = 0.003f;

        public static Texture2D NoiseSource;

        public const int ChunkSizeX = 5, ChunkSizeZ = 5; // this can be tweaked, but note that the full map will always be a multiple of theses

        public const float StreamBedElevationOffset = -1.75f;

        public const float WaterElevationOffset = -0.5f;

        public const float WaterFactor = 0.6f;

        public const float WaterBlendFactor = 1f - WaterFactor;

        public const int MaxRoadSlope = 1;

        public const int HashGridSize = 256;

        public const float WallHeight = 4f;
        public const float WallWidth = 0.75f;
        public const float WallElevationOffset = VerticalTerraceStepSize;
        public const float WallYOffset = -1f; // buries towers and walls, so they merge with the terrain
        public const float WallTowerThreashold = 0.5f;

        static HexHash[] hashGrid;

        // at each density from low to high, the chances of a feature prefab size from high to low
        static float[][] featureThresholds = 
        {
            new float[] {0.0f, 0.0f, 0.4f},
            new float[] {0.0f, 0.4f, 0.6f},
            new float[] {0.4f, 0.6f, 0.8f}
        };

        // hex points, pointy-top, with half above and half below 0 on the Z access
        // coords are in XYZ, but Z is as Y in this, with Y always 0, in order to align 
        // with the 3D plain in Unity. coords are clockwise from top.
        static readonly Vector3[] corners = new Vector3[]
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

        public static HexDirection Previous2(this HexDirection direction) => direction.Previous().Previous();

        public static HexDirection Next(this HexDirection direction) => direction == HexDirection.NW ? HexDirection.NE : direction + 1;

        public static HexDirection Next2(this HexDirection direction) => direction.Next().Next();

        public static Vector3 GetFirstCorner(HexDirection direction) => corners[(int)direction];

        public static Vector3 GetSecondCorner(HexDirection direction) => corners[(int)direction + 1];

        public static Vector3 GetFirstSolidCorner(HexDirection direction) => corners[(int)direction] * SolidFactor;

        public static Vector3 GetSecondSolidCorner(HexDirection direction) => corners[(int)direction + 1] * SolidFactor;

        public static Vector3 GetSolidEdgeMiddle(HexDirection direction) => (GetFirstCorner(direction) + GetSecondCorner(direction)) * 0.5f * SolidFactor;

        public static Vector3 GetBridge(HexDirection direction) => (GetFirstCorner(direction) + GetSecondCorner(direction)) * BlendFactor;

        public static Vector3 GetFirstWaterCorner(HexDirection direction) => corners[(int)direction] * WaterFactor;

        public static Vector3 GetSecondWaterCorner(HexDirection direction) => corners[(int)direction + 1] * WaterFactor;

        public static Vector3 GetWaterBridge(HexDirection direction) => (GetFirstCorner(direction) + GetSecondCorner(direction)) * WaterBlendFactor;

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

        // a key insight with perturb is that the same position will always be perturbed the same amount, due to the fixed noise texture
        // as a result, even though vertices for one triangle are isolated, other triangles will line up as their vertices have the same initial position
        public static Vector3 Perturb(Vector3 position)
        {
            var sample = SampleNoise(position);
            position.x += (sample.x * 2f - 1f) * CellPerturbStrength;
            position.z += (sample.z * 2f - 1f) * CellPerturbStrength;
            // we dont perturb y so that surfaces (hex tops, terrace tops) are flat
            return position;
        }

        public static void InitialiseHashGrid(int seed)
        {
            var state = Random.state;
            Random.InitState(seed);

            hashGrid = new HexHash[HashGridSize * HashGridSize];
            for(var i = 0; i < hashGrid.Length; i++)
                hashGrid[i] = HexHash.Create();

            Random.state = state; // restore original state unaltered by explicit seed
        }

        public static HexHash SampleHashGrid(Vector3 position)
        {
            var x = (int)position.x % HashGridSize;
            if (x < 0) x += HashGridSize;
            var z = (int)position.z % HashGridSize;
            if (z < 0) z += HashGridSize;
            return hashGrid[z * HashGridSize + x];
        }    

        public static float[] GetFeatureThresholds(int level) => featureThresholds[level];

        public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far) =>
            new Vector3(far.x - near.x, 0f, far.z - near.z).normalized * (WallWidth * 0.5f);

        public static Vector3 WallLerp(Vector3 near, Vector3 far)
        {
            near.x += (far.x - near.x) * 0.5f;
            near.z += (far.z - near.z) * 0.5f;
            var v = near.y < far.y ? WallElevationOffset : 1f - WallElevationOffset;
            near.y += (far.y - near.y) * v + WallYOffset;
            return near;
        }
    }
}