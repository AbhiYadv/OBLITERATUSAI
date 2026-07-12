using NUnit.Framework;
using ObliteratusAI.City;
using ObliteratusAI.Core;
using ObliteratusAI.Traffic;
using ObliteratusAI.World;
using UnityEngine;

namespace ObliteratusAI.Tests
{
    public sealed class DeterministicSimulationTests
    {
        [Test]
        public void DeterministicRandom_MatchesLegacyGoldenSequence()
        {
            DeterministicRandom random = new DeterministicRandom(20260702);
            float[] expected =
            {
                0.06816277f,
                0.51109314f,
                0.68290985f,
                0.25650975f,
                0.69267488f
            };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(random.NextFloat(), Is.EqualTo(expected[i]).Within(0.000001f));
            }
        }

        [Test]
        public void SignalTimeline_ReportsPhaseAndRemainingTimeAtBoundaries()
        {
            AssertSignal(0.0, phase: 0, remaining: 8f, nsGo: true);
            AssertSignal(7.5, phase: 0, remaining: 0.5f, nsGo: true);
            AssertSignal(8.0, phase: 1, remaining: 3f, nsYellow: true);
            AssertSignal(11.0, phase: 2, remaining: 8f, ewGo: true);
            AssertSignal(19.0, phase: 3, remaining: 3f, ewYellow: true);
            AssertSignal(-1.0, phase: 3, remaining: 1f, ewYellow: true);
        }

        [Test]
        public void CityLayout_ReturnsNextNorthboundAndEastboundStopLines()
        {
            bool northbound = CityLayout.TryGetStopLineDistance(
                CityLayout.LaneOffset,
                -20f,
                0f,
                out bool northSouthAxis,
                out float northDistance,
                out float northIntersectionX,
                out float northIntersectionZ);

            Assert.That(northbound, Is.True);
            Assert.That(northSouthAxis, Is.True);
            Assert.That(northDistance, Is.EqualTo(12.9f).Within(0.001f));
            Assert.That(northIntersectionX, Is.EqualTo(0f).Within(0.001f));
            Assert.That(northIntersectionZ, Is.EqualTo(0f).Within(0.001f));

            bool eastbound = CityLayout.TryGetStopLineDistance(
                -20f,
                CityLayout.LaneOffset,
                Mathf.PI * 0.5f,
                out northSouthAxis,
                out float eastDistance,
                out float eastIntersectionX,
                out float eastIntersectionZ);

            Assert.That(eastbound, Is.True);
            Assert.That(northSouthAxis, Is.False);
            Assert.That(eastDistance, Is.EqualTo(12.9f).Within(0.001f));
            Assert.That(eastIntersectionX, Is.EqualTo(0f).Within(0.001f));
            Assert.That(eastIntersectionZ, Is.EqualTo(0f).Within(0.001f));

            Assert.That(
                CityLayout.TryGetStopLineDistance(
                    0f,
                    0f,
                    Mathf.PI * 0.25f,
                    out _,
                    out _,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void TrafficRouteBuilder_RepeatsSeededLoopsAndClosesSamples()
        {
            DeterministicRandom firstRandom = new DeterministicRandom(4421);
            DeterministicRandom secondRandom = new DeterministicRandom(4421);
            TrafficRoute[] first = TrafficRouteBuilder.BuildLoops(12, ref firstRandom);
            TrafficRoute[] second = TrafficRouteBuilder.BuildLoops(12, ref secondRandom);

            Assert.That(first, Has.Length.EqualTo(12));
            Assert.That(second, Has.Length.EqualTo(first.Length));
            for (int i = 0; i < first.Length; i++)
            {
                Assert.That(first[i].TotalLength, Is.GreaterThan(100f));
                Assert.That(first[i].TotalLength, Is.EqualTo(second[i].TotalLength));
                CollectionAssert.AreEqual(first[i].PointsXZ, second[i].PointsXZ);

                int startPointer = 0;
                int endPointer = 0;
                first[i].Sample(0f, ref startPointer, out float startX, out float startZ);
                first[i].Sample(
                    first[i].TotalLength,
                    ref endPointer,
                    out float endX,
                    out float endZ);
                Assert.That(endX, Is.EqualTo(startX).Within(0.0001f));
                Assert.That(endZ, Is.EqualTo(startZ).Within(0.0001f));
            }
        }

        [Test]
        public void Noise_IsDeterministicBoundedAndMatchesLatticeHash()
        {
            const uint seed = 77u;
            float hash = Noise.Hash2(seed, 3, -4);
            float latticeNoise = Noise.ValueNoise2(seed, 3f, -4f);
            Assert.That(latticeNoise, Is.EqualTo(hash * 2f - 1f).Within(0.000001f));

            float first = Noise.Fbm(seed, 18.25f, -91.5f, 5, 1f / 120f);
            float second = Noise.Fbm(seed, 18.25f, -91.5f, 5, 1f / 120f);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.InRange(-1f, 1f));
        }

        [Test]
        public void TerrainSampler_CarvesRiverBelowWaterAndKeepsCityDry()
        {
            TerrainSampler.SetSeed(20260702);
            TerrainSampler.TerrainSample river = TerrainSampler.Sample(-200f, 0f);
            TerrainSampler.TerrainSample city = TerrainSampler.Sample(0f, 0f);

            Assert.That(river.RiverDist, Is.LessThan(1f));
            Assert.That(river.Height, Is.LessThan(TerrainSampler.WaterLevel - 1f));
            Assert.That(city.Height, Is.EqualTo(0f).Within(0.001f));
            Assert.That(city.Height, Is.GreaterThan(TerrainSampler.WaterLevel));
        }

        private static void AssertSignal(
            double time,
            int phase,
            float remaining,
            bool nsGo = false,
            bool nsYellow = false,
            bool ewGo = false,
            bool ewYellow = false)
        {
            SignalState state = TrafficSignalTimeline.StateAt(time);
            Assert.That(state.Phase, Is.EqualTo(phase));
            Assert.That(state.PhaseRemaining, Is.EqualTo(remaining).Within(0.001f));
            Assert.That(state.NsGo, Is.EqualTo(nsGo));
            Assert.That(state.NsYellow, Is.EqualTo(nsYellow));
            Assert.That(state.EwGo, Is.EqualTo(ewGo));
            Assert.That(state.EwYellow, Is.EqualTo(ewYellow));
        }
    }
}
