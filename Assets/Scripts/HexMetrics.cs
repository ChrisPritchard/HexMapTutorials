
namespace DarkDomains
{
    using UnityEngine;
    public static class HexMetrics
    {
        public const float OuterRadius = 10f; // distance from centre to any point. also distance between points
        public const float InnerRadius = OuterRadius * 0.866025404f; // distance from centre to any edge

        // hex points, pointy-top, with half above and half below 0 on the Z access
        // coords are in XYZ, but Z is as Y in this, with Y always 0, in order to align 
        // with the 3D plain in Unity. coords are clockwise from top.
        private static readonly Vector3[] corners = new Vector3[]
        {
            new Vector3(0f,0f,OuterRadius), // top
            new Vector3(InnerRadius,0f,OuterRadius/2), // top-right
            new Vector3(InnerRadius,0f,-OuterRadius/2), // bottom-right
            new Vector3(0f,0f,-OuterRadius), // bottom
            new Vector3(-InnerRadius,0f,-OuterRadius/2), // bottom-left
            new Vector3(-InnerRadius,0f,OuterRadius/2), // top-left
            new Vector3(0f,0f,OuterRadius), // top repeated, so modulus isn't required in some places
        };

        public static Vector3 GetFirstCorner(HexDirection direction) => corners[(int)direction];

        public static Vector3 GetSecondCorner(HexDirection direction) => corners[(int)direction + 1];
    }
}