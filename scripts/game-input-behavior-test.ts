import { createGameInputRuntime } from "../src/gta/input/GameInput.ts";

type Listener = (event: FakeEvent) => void;

class FakeEventTarget {
  private readonly listeners = new Map<string, Set<Listener>>();

  addEventListener(type: string, listener: Listener): void {
    let set = this.listeners.get(type);
    if (!set) {
      set = new Set();
      this.listeners.set(type, set);
    }
    set.add(listener);
  }

  removeEventListener(type: string, listener: Listener): void {
    this.listeners.get(type)?.delete(listener);
  }

  dispatch(type: string, event: FakeEvent): void {
    for (const listener of this.listeners.get(type) ?? []) listener(event);
  }

  count(type: string): number {
    return this.listeners.get(type)?.size ?? 0;
  }
}

class FakeEvent {
  defaultPrevented = false;

  constructor(
    readonly code: string,
    readonly target: FakeElement,
    readonly repeat = false,
  ) {}

  preventDefault(): void {
    this.defaultPrevented = true;
  }
}

class FakeElement {
  constructor(
    readonly tagName: string,
    private readonly editable = false,
  ) {}

  closest(selector: string): FakeElement | null {
    if (selector === "[contenteditable=''],[contenteditable='true']" && this.editable) return this;
    return null;
  }
}

function expect(condition: boolean, label: string): void {
  if (!condition) throw new Error(label);
}

const fakeWindow = new FakeEventTarget();
const fakeDocument = new FakeEventTarget();
const body = new FakeElement("body");
const input = new FakeElement("input");

let hidden = false;
const runtime = createGameInputRuntime({
  windowTarget: fakeWindow as unknown as Window,
  documentTarget: fakeDocument as unknown as Document,
  getVisibilityState: () => (hidden ? "hidden" : "visible"),
  getActiveElement: () => body as unknown as Element,
  isWindowFocused: () => true,
  isCanvasFocused: () => false,
});

const releaseA = runtime.acquire();
const releaseB = runtime.acquire();
expect(fakeWindow.count("keydown") === 1, "acquire should not duplicate keydown listeners");
expect(fakeWindow.count("keyup") === 1, "acquire should not duplicate keyup listeners");
expect(runtime.debug().listenerRegistrationCount === 1, "debug registration count should reflect one active listener set");

runtime.activate();
const moveDown = new FakeEvent("KeyW", body);
fakeWindow.dispatch("keydown", moveDown);
expect(runtime.isPressed("KeyW"), "KeyW should be tracked as pressed");
expect(moveDown.defaultPrevented, "active movement key should prevent page defaults");

const moveUp = new FakeEvent("KeyW", body);
fakeWindow.dispatch("keyup", moveUp);
expect(!runtime.isPressed("KeyW"), "keyup should clear KeyW");

const arrowDown = new FakeEvent("ArrowUp", body);
fakeWindow.dispatch("keydown", arrowDown);
expect(runtime.isPressed("ArrowUp"), "ArrowUp should be tracked as pressed");

fakeWindow.dispatch("blur", new FakeEvent("", body));
expect(runtime.pressedCodes().length === 0, "window blur should clear held keys");

const editableDown = new FakeEvent("KeyA", input);
fakeWindow.dispatch("keydown", editableDown);
expect(!runtime.isPressed("KeyA"), "editable field input should not move the player");
expect(!editableDown.defaultPrevented, "editable field input should not be cancelled");

fakeWindow.dispatch("keydown", new FakeEvent("Space", body));
hidden = true;
fakeDocument.dispatch("visibilitychange", new FakeEvent("", body));
expect(runtime.pressedCodes().length === 0, "hidden document should clear held keys");

releaseA();
expect(fakeWindow.count("keydown") === 1, "first release should keep shared listeners active");
releaseB();
expect(fakeWindow.count("keydown") === 0, "final release should remove keydown listener");
expect(fakeWindow.count("keyup") === 0, "final release should remove keyup listener");

console.log("game input behavior checks passed");
