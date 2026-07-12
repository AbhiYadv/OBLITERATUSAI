using System.Collections.Generic;
using ObliteratusAI.City;
using ObliteratusAI.Core;
using UnityEngine;

namespace ObliteratusAI.Traffic
{
    /// <summary>
    /// Builds seeded rectangular loop routes around random super-blocks of the
    /// street grid, offset to the right-hand lane with bezier-rounded corners.
    /// Ported from the legacy cityPlan.ts makeRoute/buildCityPlan.
    /// </summary>
    public static class TrafficRouteBuilder
    {
        private const float CornerRadius = 7f;
        private const float ResampleStep = 3f;
        private const int CornerSamples = 6;

        public static TrafficRoute[] BuildLoops(int count, ref DeterministicRandom rand)
        {
            float[] streets = CityLayout.Streets;
            TrafficRoute[] routes = new TrafficRoute[count];
            for (int r = 0; r < count; r++)
            {
                int i0 = (int)(rand.NextFloat() * (streets.Length - 1));
                int i1 = Mathf.Min(streets.Length - 1, i0 + 1 + (int)(rand.NextFloat() * 2f));
                int j0 = (int)(rand.NextFloat() * (streets.Length - 2));
                int j1 = Mathf.Min(streets.Length - 1, j0 + 1 + (int)(rand.NextFloat() * 3f));
                float x0 = streets[i0];
                float x1 = streets[i1];
                float z0 = streets[j0];
                float z1 = streets[j1];
                Vector2[] corners =
                {
                    new Vector2(x0, z0),
                    new Vector2(x1, z0),
                    new Vector2(x1, z1),
                    new Vector2(x0, z1)
                };
                if (r % 2 == 1)
                {
                    System.Array.Reverse(corners);
                }
                routes[r] = MakeLoop(corners);
            }
            return routes;
        }

        private static TrafficRoute MakeLoop(Vector2[] corners)
        {
            int n = corners.Length;

            // Offset each edge to the right-hand lane.
            Vector2[] offsetPoints = new Vector2[n * 2];
            for (int i = 0; i < n; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % n];
                Vector2 edge = b - a;
                float len = edge.magnitude;
                if (len < 1e-4f)
                {
                    len = 1f;
                }
                // Right of travel in the xz plane: (dz, -dx) / len.
                Vector2 offset = new Vector2(edge.y, -edge.x) / len * CityLayout.LaneOffset;
                offsetPoints[i * 2] = a + offset;
                offsetPoints[i * 2 + 1] = b + offset;
            }

            Vector2[] loop = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 prevEnd = offsetPoints[((i + n - 1) % n) * 2 + 1];
                Vector2 currStart = offsetPoints[i * 2];
                loop[i] = (prevEnd + currStart) * 0.5f;
            }

            // Cut each corner with a quadratic bezier, resample straights.
            List<float> dense = new List<float>(n * 24);
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = loop[(i + n - 1) % n];
                Vector2 p1 = loop[i];
                Vector2 p2 = loop[(i + 1) % n];
                Vector2 inV = p1 - p0;
                Vector2 outV = p2 - p1;
                float inLen = inV.magnitude;
                float outLen = outV.magnitude;
                if (inLen < 1e-4f)
                {
                    inLen = 1f;
                }
                if (outLen < 1e-4f)
                {
                    outLen = 1f;
                }
                float rIn = Mathf.Min(CornerRadius, inLen * 0.4f);
                float rOut = Mathf.Min(CornerRadius, outLen * 0.4f);
                Vector2 a = p1 - inV / inLen * rIn;
                Vector2 c = p1 + outV / outLen * rOut;
                Vector2 prevC = i == 0
                    ? a
                    : new Vector2(dense[dense.Count - 2], dense[dense.Count - 1]);
                float straightLen = (a - prevC).magnitude;
                int straightSteps = Mathf.Max(1, Mathf.RoundToInt(straightLen / ResampleStep));
                for (int k = 1; k <= straightSteps; k++)
                {
                    Vector2 p = prevC + (a - prevC) * (k / (float)straightSteps);
                    dense.Add(p.x);
                    dense.Add(p.y);
                }
                for (int k = 1; k <= CornerSamples; k++)
                {
                    float u = k / (float)CornerSamples;
                    float iu = 1f - u;
                    dense.Add(iu * iu * a.x + 2f * iu * u * p1.x + u * u * c.x);
                    dense.Add(iu * iu * a.y + 2f * iu * u * p1.y + u * u * c.y);
                }
            }

            return new TrafficRoute(dense.ToArray());
        }
    }
}
