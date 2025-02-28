using UnityEngine;

namespace Utilities
{
    public static class MathUtilities
    {
        public static float Square(float x) => x * x;

        public static float TriangleArea(Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = b - a;
            var ac = c - a;
            return 0.5f * Vector3.Cross(ab, ac).magnitude;
        }

        public static float NormalizedAngleDegrees(float angle)
        {
            while (angle < -180f) angle += 360f;
            while (angle > 180f) angle -= 360f;
            return angle;
        }
    }
}