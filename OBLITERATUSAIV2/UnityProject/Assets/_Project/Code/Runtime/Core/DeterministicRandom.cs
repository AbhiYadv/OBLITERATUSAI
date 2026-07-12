using System;

namespace ObliteratusAI.Core
{
    /// <summary>
    /// Mulberry32 PRNG, bit-compatible with the legacy project's seed.ts so a
    /// given seed reproduces the same layout decisions in both codebases.
    /// Uses only uint math; never mix with UnityEngine.Random in seeded systems.
    /// </summary>
    public struct DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(uint seed)
        {
            _state = seed;
        }

        public DeterministicRandom(int seed)
        {
            _state = unchecked((uint)seed);
        }

        /// <summary>Next sample in [0, 1).</summary>
        public float NextFloat()
        {
            unchecked
            {
                _state += 0x6d2b79f5u;
                uint t = (_state ^ (_state >> 15)) * (1u | _state);
                t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (float)((t ^ (t >> 14)) * (1.0 / 4294967296.0));
            }
        }

        public float Range(float minInclusive, float maxExclusive)
        {
            return minInclusive + (maxExclusive - minInclusive) * NextFloat();
        }

        public int NextInt(int maxExclusive)
        {
            return maxExclusive <= 0
                ? 0
                : Math.Min(maxExclusive - 1, (int)(NextFloat() * maxExclusive));
        }
    }
}
