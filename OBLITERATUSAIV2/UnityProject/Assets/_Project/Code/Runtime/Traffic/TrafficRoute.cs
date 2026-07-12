using UnityEngine;

namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// A closed traffic loop as densely resampled x,z points with cumulative
    /// lengths. Sampling keeps a per-vehicle monotonic segment pointer so each
    /// lookup is O(1). Ported from the legacy Traffic.tsx sampleRoute.
    /// </summary>
    public sealed class TrafficRoute
    {
        public readonly float[] PointsXZ;
        public readonly float[] CumulativeLength;
        public readonly float TotalLength;

        public TrafficRoute(float[] pointsXZ)
        {
            PointsXZ = pointsXZ;
            int count = pointsXZ.Length / 2;
            CumulativeLength = new float[count];
            float total = 0f;
            for (int i = 1; i < count; i++)
            {
                total += SegmentLength(i - 1, i);
                CumulativeLength[i] = total;
            }
            // The closing segment back to the first point.
            total += Mathf.Sqrt(
                Square(pointsXZ[0] - pointsXZ[(count - 1) * 2])
                + Square(pointsXZ[1] - pointsXZ[(count - 1) * 2 + 1]));
            TotalLength = total;
        }

        /// <summary>Position and yaw (radians) at a distance along the loop.</summary>
        public float Sample(float dist, ref int pointer, out float x, out float z)
        {
            int count = PointsXZ.Length / 2;
            float d = dist % TotalLength;
            if (d < 0f)
            {
                d += TotalLength;
            }
            if (CumulativeLength[pointer] > d)
            {
                pointer = 0;
            }
            while (pointer < count - 1 && CumulativeLength[pointer + 1] < d)
            {
                pointer++;
            }

            int i = pointer;
            int j = (i + 1) % count;
            float segStart = CumulativeLength[i];
            float segLen = (j == 0 ? TotalLength : CumulativeLength[j]) - segStart;
            float t = segLen > 0f ? (d - segStart) / segLen : 0f;
            float x0 = PointsXZ[i * 2];
            float z0 = PointsXZ[i * 2 + 1];
            float x1 = PointsXZ[j * 2];
            float z1 = PointsXZ[j * 2 + 1];
            x = x0 + (x1 - x0) * t;
            z = z0 + (z1 - z0) * t;
            return Mathf.Atan2(x1 - x0, z1 - z0);
        }

        private float SegmentLength(int i, int j)
        {
            return Mathf.Sqrt(
                Square(PointsXZ[j * 2] - PointsXZ[i * 2])
                + Square(PointsXZ[j * 2 + 1] - PointsXZ[i * 2 + 1]));
        }

        private static float Square(float v)
        {
            return v * v;
        }
    }
}
