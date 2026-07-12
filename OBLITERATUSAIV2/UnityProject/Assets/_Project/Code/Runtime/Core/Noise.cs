using UnityEngine;

namespace ObliteratusAI.Core
{
    /// <summary>
    /// Deterministic hashing and value noise, bit-compatible with the legacy
    /// project's seed.ts so seeded terrain reads the same in both codebases.
    /// </summary>
    public static class Noise
    {
        /// <summary>32-bit integer avalanche hash.</summary>
        public static uint HashInt(uint x)
        {
            unchecked
            {
                x = (x ^ (x >> 16)) * 0x45d9f3bu;
                x = (x ^ (x >> 16)) * 0x45d9f3bu;
                return x ^ (x >> 16);
            }
        }

        /// <summary>Hash a 2D integer lattice point to [0, 1).</summary>
        public static float Hash2(uint seed, int ix, int iz)
        {
            unchecked
            {
                uint h = seed ^ ((uint)ix * 0x27d4eb2du) ^ ((uint)iz * 0x165667b1u);
                return (float)(HashInt(h) * (1.0 / 4294967296.0));
            }
        }

        /// <summary>Smooth 2D value noise in [-1, 1].</summary>
        public static float ValueNoise2(uint seed, float x, float z)
        {
            int ix = Mathf.FloorToInt(x);
            int iz = Mathf.FloorToInt(z);
            float fx = Fade(x - ix);
            float fz = Fade(z - iz);
            float a = Hash2(seed, ix, iz);
            float b = Hash2(seed, ix + 1, iz);
            float c = Hash2(seed, ix, iz + 1);
            float d = Hash2(seed, ix + 1, iz + 1);
            float v = a + (b - a) * fx + (c - a) * fz + (a - b - c + d) * fx * fz;
            return v * 2f - 1f;
        }

        /// <summary>Fractal Brownian motion, roughly [-1, 1].</summary>
        public static float Fbm(uint seed, float x, float z, int octaves, float frequency)
        {
            float sum = 0f;
            float amp = 0.5f;
            float norm = 0f;
            float f = frequency;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise2(unchecked(seed + (uint)(i * 1013)), x * f, z * f) * amp;
                norm += amp;
                amp *= 0.5f;
                f *= 2.02f;
            }
            return sum / norm;
        }

        public static float Smoothstep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        public static float Mix(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private static float Fade(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
