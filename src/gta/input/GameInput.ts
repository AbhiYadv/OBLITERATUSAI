export const GAME_INPUT_CODES = new Set([
  "KeyW",
  "KeyA",
  "KeyS",
  "KeyD",
  "ArrowUp",
  "ArrowDown",
  "ArrowLeft",
  "ArrowRight",
  "ShiftLeft",
  "ShiftRight",
  "Space",
  "KeyF",
  "KeyR",
  "KeyC",
  "KeyE",
]);

export interface NexoInputDebug {
  gameInputActive: boolean;
  windowFocused: boolean;
  canvasFocused: boolean;
  pressedCodes: string[];
  controllerMounted: boolean;
  listenerRegistrationCount: number;
}

export interface GameInputRuntime {
  acquire: () => () => void;
  activate: () => void;
  clear: () => void;
  isPressed: (code: string) => boolean;
  pressedCodes: () => string[];
  setCanvasElement: (canvas: HTMLCanvasElement | null) => void;
  setControllerMounted: (mounted: boolean) => void;
  debug: () => NexoInputDebug;
}

interface GameInputRuntimeOptions {
  windowTarget: Window;
  documentTarget: Document;
  getVisibilityState?: () => DocumentVisibilityState;
  getActiveElement?: () => Element | null;
  isWindowFocused?: () => boolean;
  isCanvasFocused?: () => boolean;
}

type RuntimeWindow = Window & {
  __nexoGameInputRuntime?: GameInputRuntime;
  __nexoInputDebug?: NexoInputDebug;
};

const DEBUG_ENABLED =
  typeof import.meta !== "undefined" &&
  Boolean((import.meta as ImportMeta & { env?: { DEV?: boolean } }).env?.DEV);
const hotModule = (import.meta as ImportMeta & { hot?: { dispose: (callback: () => void) => void } }).hot;

export function getGameInput(): GameInputRuntime {
  const runtimeWindow = window as RuntimeWindow;
  if (!runtimeWindow.__nexoGameInputRuntime) {
    runtimeWindow.__nexoGameInputRuntime = createGameInputRuntime({
      windowTarget: window,
      documentTarget: document,
    });
  }
  return runtimeWindow.__nexoGameInputRuntime;
}

export function createGameInputRuntime(options: GameInputRuntimeOptions): GameInputRuntime {
  const pressed = new Set<string>();
  let refCount = 0;
  let listenersAttached = false;
  let gameInputActive = false;
  let controllerMounted = false;
  let listenerRegistrationCount = 0;
  let canvasElement: HTMLCanvasElement | null = null;

  const visibilityState = () => options.getVisibilityState?.() ?? options.documentTarget.visibilityState;
  const activeElement = () => options.getActiveElement?.() ?? options.documentTarget.activeElement;
  const windowFocused = () => options.isWindowFocused?.() ?? options.documentTarget.hasFocus();
  const canvasFocused = () => options.isCanvasFocused?.() ?? activeElement() === canvasElement;

  const syncDebug = () => {
    const debug = runtime.debug();
    if (DEBUG_ENABLED && typeof window !== "undefined") {
      (window as RuntimeWindow).__nexoInputDebug = debug;
    }
    return debug;
  };

  const onKeyDown = (event: KeyboardEvent) => {
    if (!GAME_INPUT_CODES.has(event.code)) return;
    if (isEditableTarget(event.target)) return;
    if (!gameInputActive) return;
    if (!event.repeat) pressed.add(event.code);
    event.preventDefault();
    syncDebug();
  };

  const onKeyUp = (event: KeyboardEvent) => {
    if (!GAME_INPUT_CODES.has(event.code)) return;
    pressed.delete(event.code);
    if (!isEditableTarget(event.target) && gameInputActive) event.preventDefault();
    syncDebug();
  };

  const clear = () => {
    if (pressed.size === 0) return;
    pressed.clear();
    syncDebug();
  };

  const onBlur = () => clear();
  const onVisibilityChange = () => {
    if (visibilityState() !== "visible") clear();
  };

  const attach = () => {
    if (listenersAttached) return;
    options.windowTarget.addEventListener("keydown", onKeyDown);
    options.windowTarget.addEventListener("keyup", onKeyUp);
    options.windowTarget.addEventListener("blur", onBlur);
    options.documentTarget.addEventListener("visibilitychange", onVisibilityChange);
    listenersAttached = true;
    listenerRegistrationCount = 1;
    syncDebug();
  };

  const detach = () => {
    if (!listenersAttached) return;
    options.windowTarget.removeEventListener("keydown", onKeyDown);
    options.windowTarget.removeEventListener("keyup", onKeyUp);
    options.windowTarget.removeEventListener("blur", onBlur);
    options.documentTarget.removeEventListener("visibilitychange", onVisibilityChange);
    listenersAttached = false;
    listenerRegistrationCount = 0;
    clear();
    syncDebug();
  };

  const runtime: GameInputRuntime = {
    acquire: () => {
      refCount++;
      attach();
      let released = false;
      return () => {
        if (released) return;
        released = true;
        refCount = Math.max(0, refCount - 1);
        if (refCount === 0) detach();
      };
    },
    activate: () => {
      gameInputActive = true;
      syncDebug();
    },
    clear,
    isPressed: (code: string) => pressed.has(code),
    pressedCodes: () => [...pressed].sort(),
    setCanvasElement: (canvas: HTMLCanvasElement | null) => {
      canvasElement = canvas;
      syncDebug();
    },
    setControllerMounted: (mounted: boolean) => {
      controllerMounted = mounted;
      if (!mounted) clear();
      syncDebug();
    },
    debug: () => ({
      gameInputActive,
      windowFocused: windowFocused(),
      canvasFocused: canvasFocused(),
      pressedCodes: [...pressed].sort(),
      controllerMounted,
      listenerRegistrationCount,
    }),
  };

  syncDebug();
  return runtime;
}

function isEditableTarget(target: EventTarget | null): boolean {
  const candidate = target as { tagName?: string; closest?: (selector: string) => unknown } | null;
  if (!candidate?.tagName) return false;
  const tag = candidate.tagName.toLowerCase();
  return (
    tag === "input" ||
    tag === "textarea" ||
    tag === "select" ||
    Boolean(candidate.closest?.("[contenteditable=''],[contenteditable='true']"))
  );
}

hotModule?.dispose(() => {
  if (typeof window !== "undefined") {
    (window as RuntimeWindow).__nexoGameInputRuntime?.clear();
  }
});
