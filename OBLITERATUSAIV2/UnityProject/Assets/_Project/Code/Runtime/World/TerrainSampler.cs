using ObliteratusAI.Core;
using UnityEngine;

namespace ObliteratusAI.World
{
    /// <summary>
    /// Analytic heightfield for the terrain surrounding the downtown grid.
    /// Ported from the legacy village terrain.ts and re-centered on the Unity
    /// world: the city rectangle sits flat at y=0, the world is compressed
    /// from 2 km to 1 km, and the river hugs the city's west edge where the
    /// old WesternWater strip was. Pure function of (seed, x, z).
    /// </summary>
    public static class TerrainSampler
    {
        public const float WorldSize = 1024f;
        public const float HalfWorld = WorldSize * 0.5f;
        public const float WaterLevel = -2.5f;

        /// <summary>Half-extent of the flat city plateau (the 310 m foundation).</summary>
        public const float CityEdge = 155f;
        public const float CityBlendDistance = 45f;

        // Zone centers, compressed from the legacy village layout.
        private static readonly Vector2 HillZone = new Vector2(330f, -330f);
        private static readonly Vector2 MeadowZone = new Vector2(60f, 380f);
        private static readonly Vector2 ForestNorth = new Vector2(-60f, -370f);
        private static readonly Vector2 ForestSouthwest = new Vector2(-370f, 300f);

        // River runs north to south just west of the city plateau.
        private static readonly Vector2[] River =
        {
            new Vector2(-195f, -512f),
            new Vector2(-205f, -350f),
            new Vector2(-215f, -180f),
            new Vector2(-200f, 0f),
            new Vector2(-195f, 180f),
            new Vector2(-210f, 350f),
            new Vector2(-200f, 512f)
        };

        private static uint _seed = 20260702;

        public static void SetSeed(int seed)
        {
            _seed = unchecked((uint)seed);
        }

        public struct TerrainSample
        {
            public float Height;
            public float HillMask;
            public float MeadowMask;
            public float ForestMask;
            public float CityMask;
            public float RiverDist;
        }

        public static float HeightAt(float x, float z)
        {
            return Sample(x, z).Height;
        }

        public static TerrainSample Sample(float x, float z)
        {
            float hillMask = RadialMask(x, z, HillZone, 180f, 340f);
            float meadowMask = RadialMask(x, z, MeadowZone, 200f, 360f);
            float forestMask = Mathf.Max(
                RadialMask(x, z, ForestNorth, 150f, 240f),
                RadialMask(x, z, ForestSouthwest, 140f, 220f));
            float cityMask = CityMaskAt(x, z);
            float riverDist = DistToPolyline(River, x, z);

            // Rolling macro relief around the city's ground level.
            float h = -1f + Noise.Fbm(_seed + 11u, x, z, 4, 1f / 400f) * 20f;
            // Meadow stays gentle.
            h = Noise.Mix(h, -1.5f + Noise.Fbm(_seed + 11u, x, z, 3, 1f / 400f) * 5f, meadowMask * 0.7f);
            // Hill zone in the northeast rises to ~45 m.
            if (hillMask > 0.001f)
            {
                float hillNoise = Noise.Fbm(_seed + 23u, x, z, 3, 1f / 150f) * 0.5f + 0.55f;
                h += hillMask * hillNoise * 45f;
            }
            // Fine detail bumps, suppressed near the city.
            h += Noise.Fbm(_seed + 37u, x, z, 2, 1f / 55f) * 1.5f * (1f - cityMask);
            // Flatten the downtown plateau.
            if (cityMask > 0.001f)
            {
                h = Noise.Mix(h, 0f, cityMask);
            }
            // Soft floor keeps open land above water; only the river dips under.
            float floor = WaterLevel + 1.4f;
            if (h < floor)
            {
                h = floor - (floor - h) * 0.06f;
            }
            // Gentle rim at the world edge to signal the boundary.
            float edge = Noise.Smoothstep(430f, 500f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)));
            h += edge * edge * 30f;

            // Carve the river channel below water level.
            float riverHalfWidth = 12f + Noise.Fbm(_seed + 53u, x, z, 2, 1f / 140f) * 4f;
            float bank = 1f - Noise.Smoothstep(riverHalfWidth, riverHalfWidth + 34f, riverDist);
            if (bank > 0.001f)
            {
                float bed = WaterLevel - 3.6f + Noise.Fbm(_seed + 61u, x, z, 2, 1f / 40f) * 0.5f;
                h = Noise.Mix(h, Mathf.Min(h, bed), bank);
            }

            return new TerrainSample
            {
                Height = h,
                HillMask = hillMask,
                MeadowMask = meadowMask,
                ForestMask = forestMask,
                CityMask = cityMask,
                RiverDist = riverDist
            };
        }

        /// <summary>Terrain color from the sample; slope is rise over run.</summary>
        public static Color ColorAt(in TerrainSample s, float x, float z, float slope)
        {
            float tint = Noise.Fbm(_seed + 71u, x, z, 2, 1f / 90f) * 0.06f;
            float r = 0.396f;
            float g = 0.596f;
            float b = 0.286f;
            Lerp3(ref r, ref g, ref b, 0.576f, 0.686f, 0.333f, s.MeadowMask * 0.85f);
            Lerp3(ref r, ref g, ref b, 0.282f, 0.435f, 0.216f, s.ForestMask * 0.8f);
            // Rock on steep slopes and high peaks.
            Lerp3(ref r, ref g, ref b, 0.553f, 0.553f, 0.565f, Noise.Smoothstep(0.75f, 1.15f, slope) * 0.85f);
            Lerp3(ref r, ref g, ref b, 0.553f, 0.553f, 0.565f, Noise.Smoothstep(35f, 55f, s.Height) * 0.6f);
            // Sand along the water margin.
            Lerp3(ref r, ref g, ref b, 0.76f, 0.698f, 0.502f,
                (1f - Noise.Smoothstep(WaterLevel - 0.6f, WaterLevel + 1.4f, s.Height)) * 0.9f);
            // City apron reads as concrete under the foundation slab.
            Lerp3(ref r, ref g, ref b, 0.44f, 0.44f, 0.45f, s.CityMask * 0.95f);
            return new Color(
                Mathf.Max(0f, r + tint),
                Mathf.Max(0f, g + tint),
                Mathf.Max(0f, b + tint));
        }

        /// <summary>1 inside the city rectangle, smooth falloff outside it.</summary>
        public static float CityMaskAt(float x, float z)
        {
            float dx = Mathf.Max(Mathf.Max(-CityEdge - x, x - CityEdge), 0f);
            float dz = Mathf.Max(Mathf.Max(-CityEdge - z, z - CityEdge), 0f);
            return 1f - Noise.Smoothstep(0f, CityBlendDistance, Mathf.Sqrt(dx * dx + dz * dz));
        }

        private static float RadialMask(float x, float z, Vector2 center, float inner, float outer)
        {
            float dx = x - center.x;
            float dz = z - center.y;
            return 1f - Noise.Smoothstep(inner, outer, Mathf.Sqrt(dx * dx + dz * dz));
        }

        private static float DistToPolyline(Vector2[] points, float x, float z)
        {
            float best = float.PositiveInfinity;
            for (int i = 0; i < points.Length - 1; i++)
            {
                float d = DistSqToSegment(x, z, points[i], points[i + 1]);
                if (d < best)
                {
                    best = d;
                }
            }
            return Mathf.Sqrt(best);
        }

        private static float DistSqToSegment(float px, float pz, Vector2 a, Vector2 b)
        {
            float abx = b.x - a.x;
            float abz = b.y - a.y;
            float apx = px - a.x;
            float apz = pz - a.y;
            float len = abx * abx + abz * abz;
            float t = len > 0f ? Mathf.Clamp01((apx * abx + apz * abz) / len) : 0f;
            float dx = apx - abx * t;
            float dz = apz - abz * t;
            return dx * dx + dz * dz;
        }

        private static void Lerp3(
            ref float r, ref float g, ref float b,
            float cr, float cg, float cb, float t)
        {
            r = Noise.Mix(r, cr, t);
            g = Noise.Mix(g, cg, t);
            b = Noise.Mix(b, cb, t);
        }
    }
}
