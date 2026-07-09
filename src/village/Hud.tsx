/**
 * DOM performance HUD + controls help, rendered outside the Canvas.
 * Reads the shared villageStats object on an interval (adapted from the
 * LAAS HUD pattern; F3 toggles the detail panel).
 */
import { useEffect, useRef, useState } from "react";
import { villageStats } from "./stats";

const PANEL_STYLE: React.CSSProperties = {
  position: "absolute",
  top: 10,
  left: 10,
  padding: "8px 12px",
  background: "rgba(12, 18, 24, 0.72)",
  color: "#d8e4ee",
  fontFamily: "ui-monospace, Menlo, monospace",
  fontSize: 12,
  lineHeight: 1.55,
  borderRadius: 6,
  pointerEvents: "none",
  whiteSpace: "pre",
  userSelect: "none",
};

const HELP_STYLE: React.CSSProperties = {
  ...PANEL_STYLE,
  top: "auto",
  bottom: 10,
  fontSize: 11,
  color: "#aebdc9",
};

const CROSSHAIR_STYLE: React.CSSProperties = {
  position: "absolute",
  top: "50%",
  left: "50%",
  width: 4,
  height: 4,
  marginLeft: -2,
  marginTop: -2,
  borderRadius: "50%",
  background: "rgba(255,255,255,0.75)",
  pointerEvents: "none",
};

export function VillageHud() {
  const panelRef = useRef<HTMLDivElement>(null);
  const [showDetail, setShowDetail] = useState(true);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.code === "F3") {
        e.preventDefault();
        setShowDetail((v) => !v);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  useEffect(() => {
    const id = window.setInterval(() => {
      const el = panelRef.current;
      if (!el) return;
      const s = villageStats;
      const lines = [
        `${s.fps.toFixed(0)} fps  ${s.frameMs.toFixed(1)} ms`,
      ];
      if (showDetail) {
        lines.push(
          `mode   ${s.mode}${s.mode === "walk" ? (s.grounded ? " (ground)" : " (air)") : ""}`,
          `pos    ${s.x.toFixed(1)}, ${s.y.toFixed(1)}, ${s.z.toFixed(1)}`,
          `tris   ${(s.triangles / 1000).toFixed(0)}k   draws ${s.drawCalls}`,
          `chunks ${s.chunksBuilt}/256   pending ${s.chunksPending}`,
          `geoms  ${s.geometries}   seed ${s.seed}`,
        );
      }
      el.textContent = lines.join("\n");
    }, 250);
    return () => window.clearInterval(id);
  }, [showDetail]);

  return (
    <>
      <div ref={panelRef} style={PANEL_STYLE} />
      <div style={HELP_STYLE}>
        {"click  lock mouse    WASD  move    Shift  sprint    Space  jump\n"}
        {"F  fly mode (Space/C up/down)    R  reset to village    F3  HUD detail"}
      </div>
      <div style={CROSSHAIR_STYLE} />
    </>
  );
}
