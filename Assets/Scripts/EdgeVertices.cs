
namespace HexMapTutorials
{
    using UnityEngine;

    public struct EdgeVertices
    {
        // typically v2, v4 are the edges of a road/river, with v3 being the river bed/middle of the road
        public Vector3 v1, v2, v3, v4, v5;

        public EdgeVertices(Vector3 corner1, Vector3 corner2)
        {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, 1f/4);
            v3 = Vector3.Lerp(corner1, corner2, 2f/4);
            v4 = Vector3.Lerp(corner1, corner2, 3f/4);
            v5 = corner2;
        }

        public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
        {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, outerStep);
            v3 = Vector3.Lerp(corner1, corner2, 0.5f);
            v4 = Vector3.Lerp(corner1, corner2, 1 - outerStep);
            v5 = corner2;
        }

        public static EdgeVertices TerraceLerp(EdgeVertices e1, EdgeVertices e2, int step)
        {
            EdgeVertices result;
            result.v1 = HexMetrics.TerraceLerp(e1.v1, e2.v1, step);
            result.v2 = HexMetrics.TerraceLerp(e1.v2, e2.v2, step);
            result.v3 = HexMetrics.TerraceLerp(e1.v3, e2.v3, step);
            result.v4 = HexMetrics.TerraceLerp(e1.v4, e2.v4, step);
            result.v5 = HexMetrics.TerraceLerp(e1.v5, e2.v5, step);
            return result;
        }
    }
}