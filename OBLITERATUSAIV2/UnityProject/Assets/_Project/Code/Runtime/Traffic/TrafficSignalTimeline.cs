using ObliteratusAI.City;
using UnityEngine;

namespace ObliteratusAI.Traffic
{
    public struct SignalState
    {
        public bool NsGo;
        public bool NsYellow;
        public bool EwGo;
        public bool EwYellow;

        /// <summary>Discrete phase index 0..3, for cheap change detection.</summary>
        public int Phase;

        /// <summary>Seconds until this phase changes.</summary>
        public float PhaseRemaining;
    }

    /// <summary>
    /// One synchronized city-wide signal cycle computed purely from elapsed
    /// time, so light heads and the cars that obey them can never disagree.
    /// Ported from the legacy project's signals.ts (22 s cycle: NS green 8 s,
    /// NS yellow 3 s, EW green 8 s, EW yellow 3 s).
    /// </summary>
    public static class TrafficSignalTimeline
    {
        public const double Period = 22.0;

        /// <summary>
        /// Gives neighboring intersections one of four deterministic offsets.
        /// This creates moving pockets of traffic instead of making the whole
        /// city stop and launch at the same instant.
        /// </summary>
        public static double OffsetAt(float x, float z)
        {
            int xIndex = NearestStreetIndex(x);
            int zIndex = NearestStreetIndex(z);
            int group = (xIndex + zIndex * 2) & 3;
            return group * (Period / 4.0);
        }

        public static SignalState StateAt(double time)
        {
            double t = time % Period;
            if (t < 0.0)
            {
                t += Period;
            }

            if (t < 8.0)
            {
                return new SignalState
                {
                    NsGo = true,
                    Phase = 0,
                    PhaseRemaining = (float)(8.0 - t)
                };
            }
            if (t < 11.0)
            {
                return new SignalState
                {
                    NsYellow = true,
                    Phase = 1,
                    PhaseRemaining = (float)(11.0 - t)
                };
            }
            if (t < 19.0)
            {
                return new SignalState
                {
                    EwGo = true,
                    Phase = 2,
                    PhaseRemaining = (float)(19.0 - t)
                };
            }
            return new SignalState
            {
                EwYellow = true,
                Phase = 3,
                PhaseRemaining = (float)(Period - t)
            };
        }

        private static int NearestStreetIndex(float coordinate)
        {
            int best = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < CityLayout.Streets.Length; i++)
            {
                float distance = Mathf.Abs(coordinate - CityLayout.Streets[i]);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
