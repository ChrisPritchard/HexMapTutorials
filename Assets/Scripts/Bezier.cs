
namespace DarkDomains
{
    using UnityEngine;

    public static class Bezier
    {
        public static Vector3 GetPoint (Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var r = 1f - t;
            return r * r * a + 2f * r * t * b + t * t * c; // yeah... no idea
        }

        public static Vector3 GetDirivative(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            return 2f * ((1f - t) * (b - a) + t * (c - b)); // again... lot of 'wut', here
        }
    }
}