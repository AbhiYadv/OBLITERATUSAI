import { Suspense, useEffect, useState } from "react";
import { Canvas } from "@react-three/fiber";
import { CityWorld } from "./world/CityWorld";
import { VillageWorld } from "./village/VillageWorld";
import { GtaWorld } from "./gta/GtaWorld";
import { VillageHud } from "./village/Hud";
import { getGameInput } from "./gta/input/GameInput";
import { getNexoBaseline } from "./gta/debug/PerformanceProbe";

// Default scene is the GTA open world. `?scene=village` keeps the OW-01
// village foundation; `?scene=city` keeps the original city-block prototype.
const scene = new URLSearchParams(window.location.search).get("scene") ?? "gta";

const FOCUS_HINT_STYLE: React.CSSProperties = {
  position: "absolute",
  left: "50%",
  bottom: 72,
  transform: "translateX(-50%)",
  padding: "7px 10px",
  borderRadius: 6,
  background: "rgba(12, 18, 24, 0.74)",
  color: "#d8e4ee",
  fontFamily: "ui-monospace, Menlo, monospace",
  fontSize: 12,
  pointerEvents: "none",
  userSelect: "none",
};

export function App() {
  const [gameCanvas, setGameCanvas] = useState<HTMLCanvasElement | null>(null);
  const [inputActivated, setInputActivated] = useState(false);

  useEffect(() => {
    if (!gameCanvas) return;
    const input = getGameInput();
    gameCanvas.tabIndex = 0;
    gameCanvas.style.outline = "none";
    input.setCanvasElement(gameCanvas);

    const activate = () => {
      gameCanvas.focus({ preventScroll: true });
      input.activate();
      getNexoBaseline()?.markFirstControllableFrame();
      setInputActivated(true);
    };

    gameCanvas.addEventListener("pointerdown", activate);
    gameCanvas.addEventListener("focus", activate);
    return () => {
      gameCanvas.removeEventListener("pointerdown", activate);
      gameCanvas.removeEventListener("focus", activate);
      input.setCanvasElement(null);
      input.clear();
    };
  }, [gameCanvas]);

  if (scene === "city") {
    return (
      <Canvas
        shadows
        dpr={[1, 1.75]}
        camera={{ position: [4, 7.5, 34], fov: 55, near: 0.1, far: 800 }}
        onCreated={({ camera, gl }) => {
          setGameCanvas(gl.domElement);
          camera.lookAt(8, 9, -18);
        }}
      >
        <Suspense fallback={null}>
          <CityWorld />
        </Suspense>
      </Canvas>
    );
  }
  return (
    <div style={{ position: "relative", width: "100%", height: "100%" }}>
      <Canvas
        shadows="soft"
        dpr={[1, 1.5]}
        camera={{ position: [0, 20, 26], fov: 70, near: 0.3, far: 3000 }}
        onCreated={({ gl }) => {
          setGameCanvas(gl.domElement);
        }}
      >
        <Suspense fallback={null}>
          {scene === "village" ? <VillageWorld /> : <GtaWorld />}
        </Suspense>
      </Canvas>
      {!inputActivated && scene !== "city" ? <div style={FOCUS_HINT_STYLE}>Click the world to enable controls</div> : null}
      <VillageHud />
    </div>
  );
}
