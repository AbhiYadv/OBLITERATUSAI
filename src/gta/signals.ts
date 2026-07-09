/**
 * Shared traffic-signal timing: one synchronized city-wide cycle, computed
 * purely from elapsed time so the light heads and the cars that obey them
 * can never disagree.
 */
export const SIGNAL_PERIOD = 22;

export interface SignalState {
  /** North-south approaches may proceed. */
  nsGo: boolean;
  nsYellow: boolean;
  ewGo: boolean;
  ewYellow: boolean;
  /** Discrete phase index 0..3, for cheap change detection. */
  phase: number;
}

export function signalAt(time: number): SignalState {
  const t = time % SIGNAL_PERIOD;
  if (t < 8) return { nsGo: true, nsYellow: false, ewGo: false, ewYellow: false, phase: 0 };
  if (t < 11) return { nsGo: false, nsYellow: true, ewGo: false, ewYellow: false, phase: 1 };
  if (t < 19) return { nsGo: false, nsYellow: false, ewGo: true, ewYellow: false, phase: 2 };
  return { nsGo: false, nsYellow: false, ewGo: false, ewYellow: true, phase: 3 };
}
