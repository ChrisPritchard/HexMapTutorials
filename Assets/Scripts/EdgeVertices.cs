
namespace DarkDomains
{
    using UnityEngine;

    public struct EdgeVertices
    {
        public Vector3 v1, v2, v3, v4;

        public EdgeVertices(Vector3 corner1, Vector3 corner2)
        {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, 1f/3);
            v3 = Vector3.Lerp(corner1, corner2, 2f/3);
            v4 = corner2;
        }

        public static EdgeVertices TerraceLerp(EdgeVertices e1, EdgeVertices e2, int step)
        {
            EdgeVertices result;
            result.v1 = HexMetrics.TerraceLerp(e1.v1, e2.v1, step);
            result.v2 = HexMetrics.TerraceLerp(e1.v2, e2.v2, step);
            result.v3 = HexMetrics.TerraceLerp(e1.v3, e2.v3, step);
            result.v4 = HexMetrics.TerraceLerp(e1.v4, e2.v4, step);
            return result;
        }
    }
}