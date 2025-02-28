using UnityEngine;

namespace Utilities
{
    // Utility functions for calculating Catmull-Rom splines
    public static class CatmullRom
    {
        public static void CalculateChain(Vector3[] controlPoints, uint pointsPerSegment, float alpha, ref Vector3[] output)
        {
            var arrayLength = pointsPerSegment * (controlPoints.Length - 3);
            if (arrayLength != output.Length)
            {
                output = new Vector3[arrayLength];
            }

            for (var i = 0; i < controlPoints.Length - 3; ++i)
            {
                var newSegment = SingleSegment(controlPoints[i], controlPoints[i + 1], controlPoints[i + 2],
                    controlPoints[i + 3], pointsPerSegment, alpha);

                for (var j = 0; j < pointsPerSegment; ++j)
                {
                    output[i * pointsPerSegment + j] = newSegment[j];
                }
            }
        }
        
        static Vector3[] SingleSegment(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, uint numberOfPoints, float alpha)
        {
            var points = new Vector3[numberOfPoints];
            
            var t0 = 0f;
            var t1 = GetT(t0, p0, p1, alpha);
            var t2 = GetT(t1, p1, p2, alpha);
            var t3 = GetT(t2, p2, p3, alpha);

            var t = t1;
            for (var i = 0; i < numberOfPoints; ++i)
            {
                var a1 = (t1-t)/(t1-t0)*p0 + (t-t0)/(t1-t0)*p1;
                var a2 = (t2-t)/(t2-t1)*p1 + (t-t1)/(t2-t1)*p2;
                var a3 = (t3-t)/(t3-t2)*p2 + (t-t2)/(t3-t2)*p3;
		    
                var b1 = (t2-t)/(t2-t0)*a1 + (t-t0)/(t2-t0)*a2;
                var b2 = (t3-t)/(t3-t1)*a2 + (t-t1)/(t3-t1)*a3;
		    
                var c = (t2-t)/(t2-t1)*b1 + (t-t1)/(t2-t1)*b2;

                points[i] = c;

                t += (t2 - t1) / numberOfPoints;
            }

            return points;
        }

        static float GetT(float prevT, Vector2 p0, Vector2 p1, float alpha)
        {
            var a = MathUtilities.Square(p1.x - p0.x) + MathUtilities.Square(p1.y - p0.y);
            var segT = Mathf.Pow(a, alpha * 0.5f);
            return segT + prevT;
        }

    }
}