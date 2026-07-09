export interface BaselineFrameSample {
  deltaMs: number;
  drawCalls: number;
  triangles: number;
  geometries: number;
  textures: number;
  activePedestrians: number;
  activeTrafficVehicles: number;
  confirmedActiveMixers: number;
  inferredActiveMixers: number;
  collisionQueries?: number;
}

export interface BaselineEnvironment {
  browser: string;
  device: string;
  dpr: number;
  viewport: {
    width: number;
    height: number;
  };
  quality: string;
}

export interface BaselineResourceEntry {
  name: string;
  initiatorType: string;
  transferSize: number;
  encodedBodySize: number;
  decodedBodySize: number;
  duration: number;
}

export interface BaselineResourceSummary {
  transferredBytes: number;
  encodedBodyBytes: number;
  decodedBodyBytes: number;
  largestResources: BaselineResourceEntry[];
}

export interface BaselineSegmentSummary {
  name: string;
  frameCount: number;
  durationMs: number;
  averageFps: number;
  onePercentLowFps: number;
  onePercentLowMethod: string;
  p50FrameTimeMs: number;
  p95FrameTimeMs: number;
  p99FrameTimeMs: number;
  worstFrameTimeMs: number;
  drawCalls: number;
  visibleTriangles: number;
  geometries: number;
  textures: number;
  activePedestrians: number;
  activeTrafficVehicles: number;
  confirmedActiveMixers: number;
  inferredActiveMixers: number;
  collisionQueriesPerFrame: number;
}

export interface BaselineDump {
  appStartedAtMs: number;
  timeToFirstControllableFrameMs: number | null;
  environment: BaselineEnvironment;
  resources: BaselineResourceSummary;
  segments: BaselineSegmentSummary[];
  activeSegment: string | null;
  measurementLimitations: string[];
}

export interface PerformanceProbe {
  startSegment: (name: string) => void;
  endSegment: () => BaselineSegmentSummary | null;
  recordFrame: (sample: BaselineFrameSample) => void;
  recordCollisionQuery: () => void;
  markFirstControllableFrame: (nowMs?: number) => void;
  isRecording: () => boolean;
  reset: () => void;
  dump: () => BaselineDump;
}

interface PerformanceProbeOptions {
  appStartedAt?: number;
  now?: () => number;
  getEnvironment?: () => BaselineEnvironment;
  getResources?: () => BaselineResourceSummary;
  maxFramesPerSegment?: number;
}

interface SegmentAccumulator {
  name: string;
  frames: Float64Array;
  frameCount: number;
  durationMs: number;
  drawCalls: number;
  visibleTriangles: number;
  geometries: number;
  textures: number;
  activePedestrians: number;
  activeTrafficVehicles: number;
  confirmedActiveMixers: number;
  inferredActiveMixers: number;
  collisionQueries: number;
}

type BaselineWindow = Window & {
  __nexoBaseline?: PerformanceProbe;
};

const MAX_FRAMES_PER_SEGMENT = 12_000;
const ONE_PERCENT_LOW_METHOD = "p99 frame time converted to FPS";
const moduleAppStartedAt =
  typeof performance !== "undefined" ? performance.now() : 0;
function baselineEnabled(): boolean {
  const isDev =
    typeof import.meta !== "undefined" &&
    Boolean((import.meta as ImportMeta & { env?: { DEV?: boolean } }).env?.DEV);
  if (isDev) return true;
  if (typeof window === "undefined") return false;
  return new URLSearchParams(window.location.search).get("nexoBaseline") === "1";
}

export function createPerformanceProbe(options: PerformanceProbeOptions = {}): PerformanceProbe {
  const now = options.now ?? (() => performance.now());
  const getEnvironment = options.getEnvironment ?? defaultEnvironment;
  const getResources = options.getResources ?? defaultResourceSummary;
  const maxFrames = options.maxFramesPerSegment ?? MAX_FRAMES_PER_SEGMENT;
  const appStartedAt = options.appStartedAt ?? moduleAppStartedAt;

  let current: SegmentAccumulator | null = null;
  let completed: BaselineSegmentSummary[] = [];
  let firstControllableAt: number | null = null;
  let frameCollisionQueries = 0;

  const makeSegment = (name: string): SegmentAccumulator => ({
    name,
    frames: new Float64Array(maxFrames),
    frameCount: 0,
    durationMs: 0,
    drawCalls: 0,
    visibleTriangles: 0,
    geometries: 0,
    textures: 0,
    activePedestrians: 0,
    activeTrafficVehicles: 0,
    confirmedActiveMixers: 0,
    inferredActiveMixers: 0,
    collisionQueries: 0,
  });

  const summarize = (segment: SegmentAccumulator): BaselineSegmentSummary => {
    const frames = Array.from(segment.frames.slice(0, segment.frameCount)).sort((a, b) => a - b);
    const p50 = percentile(frames, 0.5);
    const p95 = percentile(frames, 0.95);
    const p99 = percentile(frames, 0.99);
    return {
      name: segment.name,
      frameCount: segment.frameCount,
      durationMs: round(segment.durationMs),
      averageFps: segment.durationMs > 0 ? round((segment.frameCount * 1000) / segment.durationMs) : 0,
      onePercentLowFps: p99 > 0 ? round(1000 / p99) : 0,
      onePercentLowMethod: ONE_PERCENT_LOW_METHOD,
      p50FrameTimeMs: round(p50),
      p95FrameTimeMs: round(p95),
      p99FrameTimeMs: round(p99),
      worstFrameTimeMs: round(frames[frames.length - 1] ?? 0),
      drawCalls: segment.drawCalls,
      visibleTriangles: segment.visibleTriangles,
      geometries: segment.geometries,
      textures: segment.textures,
      activePedestrians: segment.activePedestrians,
      activeTrafficVehicles: segment.activeTrafficVehicles,
      confirmedActiveMixers: segment.confirmedActiveMixers,
      inferredActiveMixers: segment.inferredActiveMixers,
      collisionQueriesPerFrame:
        segment.frameCount > 0 ? round(segment.collisionQueries / segment.frameCount) : 0,
    };
  };

  return {
    startSegment(name: string): void {
      if (current) completed.push(summarize(current));
      current = makeSegment(name);
      frameCollisionQueries = 0;
    },

    endSegment(): BaselineSegmentSummary | null {
      if (!current) return null;
      const summary = summarize(current);
      completed.push(summary);
      current = null;
      frameCollisionQueries = 0;
      return summary;
    },

    recordFrame(sample: BaselineFrameSample): void {
      if (!current) {
        frameCollisionQueries = 0;
        return;
      }
      if (current.frameCount >= maxFrames) {
        frameCollisionQueries = 0;
        return;
      }
      current.frames[current.frameCount++] = sample.deltaMs;
      current.durationMs += sample.deltaMs;
      current.drawCalls = Math.max(current.drawCalls, sample.drawCalls);
      current.visibleTriangles = Math.max(current.visibleTriangles, sample.triangles);
      current.geometries = Math.max(current.geometries, sample.geometries);
      current.textures = Math.max(current.textures, sample.textures);
      current.activePedestrians = Math.max(current.activePedestrians, sample.activePedestrians);
      current.activeTrafficVehicles = Math.max(current.activeTrafficVehicles, sample.activeTrafficVehicles);
      current.confirmedActiveMixers = Math.max(current.confirmedActiveMixers, sample.confirmedActiveMixers);
      current.inferredActiveMixers = Math.max(current.inferredActiveMixers, sample.inferredActiveMixers);
      current.collisionQueries += sample.collisionQueries ?? frameCollisionQueries;
      frameCollisionQueries = 0;
    },

    recordCollisionQuery(): void {
      frameCollisionQueries++;
    },

    markFirstControllableFrame(nowMs = now()): void {
      if (firstControllableAt === null) firstControllableAt = nowMs;
    },

    isRecording(): boolean {
      return current !== null;
    },

    reset(): void {
      current = null;
      completed = [];
      firstControllableAt = null;
      frameCollisionQueries = 0;
    },

    dump(): BaselineDump {
      const segments = current ? [...completed, summarize(current)] : [...completed];
      return {
        appStartedAtMs: round(appStartedAt),
        timeToFirstControllableFrameMs:
          firstControllableAt === null ? null : round(firstControllableAt - appStartedAt),
        environment: getEnvironment(),
        resources: getResources(),
        segments,
        activeSegment: current?.name ?? null,
        measurementLimitations: [
          "Active animation mixers are not directly instrumented because player and character files are frozen.",
          "Confirmed mixer count is limited to directly instrumented non-frozen systems; inferred mixer count uses measured active pedestrians plus the visible player.",
          "Browser resource timing reports actual requested resources, not every file present in public assets or dist.",
        ],
      };
    },
  };
}

export function getNexoBaseline(): PerformanceProbe | null {
  if (!baselineEnabled() || typeof window === "undefined") return null;
  const baselineWindow = window as BaselineWindow;
  baselineWindow.__nexoBaseline ??= createPerformanceProbe();
  return baselineWindow.__nexoBaseline;
}

export function recordBaselineCollisionQuery(): void {
  getNexoBaseline()?.recordCollisionQuery();
}

function percentile(sortedValues: number[], fraction: number): number {
  if (sortedValues.length === 0) return 0;
  const index = Math.min(sortedValues.length - 1, Math.max(0, Math.ceil(sortedValues.length * fraction) - 1));
  return sortedValues[index];
}

function round(value: number): number {
  return Math.round(value * 100) / 100;
}

function defaultEnvironment(): BaselineEnvironment {
  const nav = navigator as Navigator & {
    deviceMemory?: number;
    userAgentData?: {
      platform?: string;
      mobile?: boolean;
      brands?: Array<{ brand: string; version: string }>;
    };
  };
  const viewport = {
    width: window.innerWidth,
    height: window.innerHeight,
  };
  const platform = nav.userAgentData?.platform ?? nav.platform;
  const deviceParts = [
    platform,
    `${nav.hardwareConcurrency ?? "unknown"} logical CPUs`,
    nav.deviceMemory ? `${nav.deviceMemory} GB memory hint` : "memory hint unavailable",
    nav.userAgentData?.mobile ? "mobile" : "desktop",
  ];
  return {
    browser: nav.userAgent,
    device: deviceParts.filter(Boolean).join("; "),
    dpr: window.devicePixelRatio,
    viewport,
    quality: "development baseline: Canvas dpr [1, 1.5], soft shadows, default GTA scene",
  };
}

function defaultResourceSummary(): BaselineResourceSummary {
  const entries = performance
    .getEntriesByType("resource")
    .filter((entry): entry is PerformanceResourceTiming => "transferSize" in entry);
  let transferredBytes = 0;
  let encodedBodyBytes = 0;
  let decodedBodyBytes = 0;
  const resources: BaselineResourceEntry[] = entries.map((entry) => {
    transferredBytes += entry.transferSize;
    encodedBodyBytes += entry.encodedBodySize;
    decodedBodyBytes += entry.decodedBodySize;
    return {
      name: entry.name,
      initiatorType: entry.initiatorType,
      transferSize: entry.transferSize,
      encodedBodySize: entry.encodedBodySize,
      decodedBodySize: entry.decodedBodySize,
      duration: round(entry.duration),
    };
  });
  resources.sort((a, b) => {
    const aSize = Math.max(a.transferSize, a.decodedBodySize, a.encodedBodySize);
    const bSize = Math.max(b.transferSize, b.decodedBodySize, b.encodedBodySize);
    return bSize - aSize;
  });
  return {
    transferredBytes,
    encodedBodyBytes,
    decodedBodyBytes,
    largestResources: resources.slice(0, 12),
  };
}
