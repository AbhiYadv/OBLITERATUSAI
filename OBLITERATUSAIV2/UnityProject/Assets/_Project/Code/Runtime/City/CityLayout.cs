using UnityEngine;

namespace ObliteratusAI.City
{
    /// <summary>
    /// Canonical downtown grid shared by the editor city builder and the
    /// runtime traffic/pedestrian systems, so the two can never disagree.
    /// Ported from the legacy project's cityPlan.ts, compressed to the
    /// 300x300 m prototype footprint.
    /// </summary>
    public static class CityLayout
    {
        public static readonly float[] Streets = { -150f, -75f, 0f, 75f, 150f };

        public const float RoadWidth = 11f;
        public const float SidewalkHeight = 0.15f;
        public const float LaneOffset = 2.75f;
        public const float RoadY = 0.05f;
        public const float SlabTop = SidewalkHeight;
        public const float StopLineSetback = RoadWidth * 0.5f + 1.6f;

        public struct BlockRect
        {
            public float MinX;
            public float MaxX;
            public float MinZ;
            public float MaxZ;

            public float CenterX => (MinX + MaxX) * 0.5f;
            public float CenterZ => (MinZ + MaxZ) * 0.5f;
            public float Width => MaxX - MinX;
            public float Depth => MaxZ - MinZ;
        }

        public static readonly BlockRect[] Blocks = BuildBlocks();

        private static BlockRect[] BuildBlocks()
        {
            int side = Streets.Length - 1;
            BlockRect[] blocks = new BlockRect[side * side];
            int index = 0;
            for (int x = 0; x < side; x++)
            {
                for (int z = 0; z < side; z++)
                {
                    blocks[index++] = new BlockRect
                    {
                        MinX = Streets[x] + RoadWidth * 0.5f,
                        MaxX = Streets[x + 1] - RoadWidth * 0.5f,
                        MinZ = Streets[z] + RoadWidth * 0.5f,
                        MaxZ = Streets[z + 1] - RoadWidth * 0.5f
                    };
                }
            }
            return blocks;
        }

        /// <summary>
        /// Distance to the stop line of the next signalized cross-street along
        /// the current axis of travel. False mid-corner (vehicles never stop
        /// inside an intersection) or when no line is ahead.
        /// </summary>
        public static bool TryGetStopLineDistance(
            float x,
            float z,
            float yaw,
            out bool northSouthAxis,
            out float distance,
            out float intersectionX,
            out float intersectionZ)
        {
            float fx = Mathf.Sin(yaw);
            float fz = Mathf.Cos(yaw);
            if (Mathf.Abs(fz) > 0.92f)
            {
                northSouthAxis = true;
            }
            else if (Mathf.Abs(fx) > 0.92f)
            {
                northSouthAxis = false;
            }
            else
            {
                northSouthAxis = false;
                distance = float.PositiveInfinity;
                intersectionX = 0f;
                intersectionZ = 0f;
                return false;
            }

            float coord = northSouthAxis ? z : x;
            float dir = Mathf.Sign(northSouthAxis ? fz : fx);
            float best = float.PositiveInfinity;
            float bestLine = 0f;
            foreach (float line in Streets)
            {
                float stop = line - dir * StopLineSetback;
                float d = (stop - coord) * dir;
                if (d > -1.5f && d < best)
                {
                    best = d;
                    bestLine = line;
                }
            }

            distance = best;
            intersectionX = northSouthAxis ? NearestStreet(x) : bestLine;
            intersectionZ = northSouthAxis ? bestLine : NearestStreet(z);
            return !float.IsPositiveInfinity(best);
        }

        private static float NearestStreet(float coordinate)
        {
            float best = Streets[0];
            float bestDistance = Mathf.Abs(coordinate - best);
            for (int i = 1; i < Streets.Length; i++)
            {
                float candidateDistance = Mathf.Abs(coordinate - Streets[i]);
                if (candidateDistance < bestDistance)
                {
                    best = Streets[i];
                    bestDistance = candidateDistance;
                }
            }
            return best;
        }
    }
}
