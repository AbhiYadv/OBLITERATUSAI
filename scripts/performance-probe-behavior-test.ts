import { createPerformanceProbe } from "../src/gta/debug/PerformanceProbe.ts";

function expect(condition: boolean, label: string): void {
  if (!condition) throw new Error(label);
}

const probe = createPerformanceProbe({
  appStartedAt: 10,
  now: () => 100,
  getEnvironment: () => ({
    browser: "test-browser",
    device: "test-device",
    dpr: 2,
    viewport: { width: 800, height: 600 },
    quality: "development-baseline",
  }),
  getResources: () => ({
    transferredBytes: 1234,
    decodedBodyBytes: 4567,
    encodedBodyBytes: 2345,
    largestResources: [
      {
        name: "http://localhost/assets/test.glb",
        initiatorType: "fetch",
        transferSize: 1000,
        encodedBodySize: 900,
        decodedBodySize: 1800,
        duration: 12,
      },
    ],
  }),
});

probe.markFirstControllableFrame(42);
probe.startSegment("spawn");
probe.recordFrame({
  deltaMs: 10,
  drawCalls: 100,
  triangles: 500000,
  geometries: 200,
  textures: 20,
  activePedestrians: 7,
  activeTrafficVehicles: 11,
  confirmedActiveMixers: 7,
  inferredActiveMixers: 8,
  collisionQueries: 3,
});
probe.recordFrame({
  deltaMs: 20,
  drawCalls: 120,
  triangles: 550000,
  geometries: 220,
  textures: 22,
  activePedestrians: 9,
  activeTrafficVehicles: 13,
  confirmedActiveMixers: 9,
  inferredActiveMixers: 10,
  collisionQueries: 5,
});
probe.endSegment();

const dump = probe.dump();
const segment = dump.segments[0];

expect(dump.timeToFirstControllableFrameMs === 32, "time to controllable should use appStartedAt delta");
expect(segment.frameCount === 2, "segment should count recorded frames");
expect(segment.durationMs === 30, "segment duration should sum frame times");
expect(segment.averageFps > 66 && segment.averageFps < 67, "average FPS should use frame count over duration");
expect(segment.onePercentLowFps === 50, "1% low FPS should convert p99 frame time to FPS");
expect(segment.p50FrameTimeMs === 10, "p50 should use sorted frame samples");
expect(segment.p99FrameTimeMs === 20, "p99 should use sorted frame samples");
expect(segment.worstFrameTimeMs === 20, "worst frame should track max sample");
expect(segment.collisionQueriesPerFrame === 4, "collision queries should average over frames");
expect(segment.activePedestrians === 9, "active pedestrians should report measured high-water mark");
expect(segment.activeTrafficVehicles === 13, "active traffic should report measured high-water mark");
expect(dump.resources.transferredBytes === 1234, "resource transfer bytes should come from browser resources");

console.log("performance probe behavior checks passed");
