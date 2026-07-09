/**
 * Seeded canvas-generated facade textures so buildings read as real
 * architecture instead of flat-colored boxes. Each facade texture tile is
 * a 4x4 grid of 3 m window cells (walls repeat it via UV scale); the
 * storefront texture is one 6 m ground-floor bay repeated horizontally.
 */
import * as THREE from "three";
import { mulberry32 } from "../village/seed";

const TILE = 512;
const CELLS = 4;

export const WINDOW_CELL_M = 3; // world metres per window cell
export const FACADE_TILE_M = WINDOW_CELL_M * CELLS; // metres per texture tile
export const STOREFRONT_TILE_M = 6;
export const STOREFRONT_H = 4; // storefront strip height in metres

function finishTexture(canvas: HTMLCanvasElement): THREE.CanvasTexture {
  const tex = new THREE.CanvasTexture(canvas);
  tex.wrapS = THREE.RepeatWrapping;
  tex.wrapT = THREE.RepeatWrapping;
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 8;
  return tex;
}

function speckle(
  ctx: CanvasRenderingContext2D,
  rand: () => number,
  count: number,
  alpha: number,
): void {
  for (let i = 0; i < count; i++) {
    ctx.fillStyle = `rgba(${rand() < 0.5 ? "0,0,0" : "255,255,255"},${alpha})`;
    ctx.fillRect(rand() * TILE, rand() * TILE, 2, 2);
  }
}

/** Punched/ribbon window facade over a wall color. */
function drawWallFacade(
  rand: () => number,
  wall: string,
  wallShade: string,
  ribbon: boolean,
): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = TILE;
  canvas.height = TILE;
  const ctx = canvas.getContext("2d")!;
  const cell = TILE / CELLS;

  ctx.fillStyle = wall;
  ctx.fillRect(0, 0, TILE, TILE);
  speckle(ctx, rand, 500, 0.05);

  // Floor slab shadow line at each cell row.
  ctx.fillStyle = wallShade;
  for (let j = 0; j < CELLS; j++) {
    ctx.fillRect(0, j * cell, TILE, 3);
  }

  for (let j = 0; j < CELLS; j++) {
    for (let i = 0; i < CELLS; i++) {
      const x = i * cell;
      const y = j * cell;
      const wx = ribbon ? x + cell * 0.06 : x + cell * 0.2;
      const ww = ribbon ? cell * 0.88 : cell * 0.6;
      const wy = y + cell * 0.22;
      const wh = cell * 0.52;
      const lit = rand() < 0.16;
      ctx.fillStyle = lit ? "#e8c37a" : rand() < 0.5 ? "#2c3947" : "#33424f";
      ctx.fillRect(wx, wy, ww, wh);
      // Deep top + side reveals sell the window inset.
      ctx.fillStyle = "rgba(0,0,0,0.5)";
      ctx.fillRect(wx, wy, ww, 7);
      ctx.fillRect(wx, wy, 5, wh);
      ctx.fillStyle = "rgba(255,255,255,0.2)";
      ctx.fillRect(wx, wy + wh, ww, 3);
      ctx.fillStyle = "rgba(20,24,28,0.5)";
      ctx.fillRect(wx + ww / 2 - 2, wy, 3, wh);
    }
  }
  return canvas;
}

/** Brick facade with mortar courses and punched windows. */
function drawBrickFacade(rand: () => number): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = TILE;
  canvas.height = TILE;
  const ctx = canvas.getContext("2d")!;
  const cell = TILE / CELLS;

  ctx.fillStyle = "#8a4a38";
  ctx.fillRect(0, 0, TILE, TILE);
  for (let y = 0; y < TILE; y += 6) {
    ctx.fillStyle = "rgba(60,28,22,0.35)";
    ctx.fillRect(0, y, TILE, 1);
    const offset = (y / 6) % 2 === 0 ? 0 : 8;
    for (let x = offset; x < TILE; x += 16) {
      ctx.fillRect(x, y, 1, 6);
    }
  }
  speckle(ctx, rand, 700, 0.06);

  for (let j = 0; j < CELLS; j++) {
    for (let i = 0; i < CELLS; i++) {
      const x = i * cell;
      const y = j * cell;
      const wx = x + cell * 0.22;
      const ww = cell * 0.56;
      const wy = y + cell * 0.2;
      const wh = cell * 0.56;
      ctx.fillStyle = "#b9a58c";
      ctx.fillRect(wx - 3, wy - 4, ww + 6, 4);
      ctx.fillRect(wx - 3, wy + wh, ww + 6, 3);
      const lit = rand() < 0.14;
      ctx.fillStyle = lit ? "#dfb46e" : "#26313c";
      ctx.fillRect(wx, wy, ww, wh);
      ctx.fillStyle = "rgba(0,0,0,0.5)";
      ctx.fillRect(wx, wy, ww, 7);
      ctx.fillRect(wx, wy, 5, wh);
      ctx.fillStyle = "rgba(240,235,225,0.85)";
      ctx.fillRect(wx + ww / 2 - 1, wy, 2, wh);
      ctx.fillRect(wx, wy + wh / 2 - 1, ww, 2);
    }
  }
  return canvas;
}

/** Blue-glass curtain wall with sky gradient reflections. */
function drawGlassFacade(rand: () => number): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = TILE;
  canvas.height = TILE;
  const ctx = canvas.getContext("2d")!;
  const cell = TILE / CELLS;

  const grad = ctx.createLinearGradient(0, 0, TILE * 0.3, TILE);
  grad.addColorStop(0, "#7fa8c4");
  grad.addColorStop(0.5, "#4a6f8e");
  grad.addColorStop(1, "#324d66");
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, TILE, TILE);

  for (let j = 0; j < CELLS; j++) {
    for (let i = 0; i < CELLS; i++) {
      const r = rand();
      if (r < 0.22) {
        ctx.fillStyle = r < 0.05 ? "rgba(232,195,122,0.75)" : "rgba(20,32,44,0.35)";
        ctx.fillRect(i * cell + 2, j * cell + 2, cell - 4, cell - 4);
      }
    }
  }
  ctx.fillStyle = "rgba(18,24,30,0.8)";
  for (let k = 0; k <= CELLS; k++) {
    ctx.fillRect(k * cell - 1, 0, 3, TILE);
    ctx.fillRect(0, k * cell - 1, TILE, 3);
  }
  ctx.fillStyle = "rgba(255,255,255,0.07)";
  ctx.beginPath();
  ctx.moveTo(TILE * 0.1, 0);
  ctx.lineTo(TILE * 0.35, 0);
  ctx.lineTo(0, TILE * 0.7);
  ctx.lineTo(0, TILE * 0.45);
  ctx.fill();
  return canvas;
}

/** Nexo-world storefront brands, straight from the concept art. */
export const STORE_BRANDS = [
  { name: "NEXO MARKETPLACE · MCP TOOLS", bg: "#14352a", accent: "#3bd98a" },
  { name: "PostgreSQL Data Services", bg: "#1c3c5c", accent: "#9cc9ff" },
  { name: "docker Logistics", bg: "#0d3a5f", accent: "#5ad1f0" },
  { name: "SECURE AUTHENTICATION HUB", bg: "#292344", accent: "#b39dff" },
] as const;

/** Ground-floor storefront bay: brand signband, glass, door. */
function drawStorefront(
  rand: () => number,
  brand: (typeof STORE_BRANDS)[number],
): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = TILE;
  canvas.height = TILE / 2;
  const ctx = canvas.getContext("2d")!;
  const H = TILE / 2;

  ctx.fillStyle = "#3b3f45";
  ctx.fillRect(0, 0, TILE, H);
  // Signband with the brand name.
  ctx.fillStyle = brand.bg;
  ctx.fillRect(4, 4, TILE - 8, H * 0.22);
  ctx.fillStyle = brand.accent;
  ctx.fillRect(4, H * 0.22, TILE - 8, 4);
  ctx.font = "bold 34px 'Helvetica Neue', Arial, sans-serif";
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillStyle = "#f2f6f9";
  ctx.fillText(brand.name, TILE / 2, H * 0.13, TILE - 40);
  // Glass, mullions, interior glow.
  const glassTop = H * 0.3;
  ctx.fillStyle = "#16222c";
  ctx.fillRect(8, glassTop, TILE - 16, H - glassTop - 6);
  ctx.fillStyle = "rgba(140,180,205,0.22)";
  ctx.fillRect(8, glassTop, TILE - 16, (H - glassTop) * 0.4);
  ctx.fillStyle = brand.accent + "33";
  ctx.fillRect(24, glassTop + 14, TILE * 0.28, (H - glassTop) * 0.42);
  ctx.fillStyle = "#3b3f45";
  for (let x = 8 + 84; x < TILE - 16; x += 88) {
    ctx.fillRect(x, glassTop, 8, H - glassTop - 6);
  }
  const doorX = TILE * (0.35 + rand() * 0.3);
  ctx.fillStyle = "#12181e";
  ctx.fillRect(doorX, glassTop + 6, 58, H - glassTop - 12);
  ctx.fillStyle = "#c9b06a";
  ctx.fillRect(doorX + 46, glassTop + (H - glassTop) * 0.55, 5, 15);
  return canvas;
}

export interface FacadeTextures {
  facades: THREE.CanvasTexture[]; // index matches BuildingSpec.variant
  storefronts: THREE.CanvasTexture[]; // index matches STORE_BRANDS
  dispose: () => void;
}

export function makeFacadeTextures(seed: number): FacadeTextures {
  const rand = mulberry32(seed ^ 0xfacade);
  const facades = [
    finishTexture(drawWallFacade(rand, "#b3a48c", "rgba(70,62,48,0.35)", false)),
    finishTexture(drawWallFacade(rand, "#8f9295", "rgba(45,48,52,0.4)", true)),
    finishTexture(drawBrickFacade(rand)),
    finishTexture(drawGlassFacade(rand)),
  ];
  const storefronts = STORE_BRANDS.map((brand) => finishTexture(drawStorefront(rand, brand)));
  return {
    facades,
    storefronts,
    dispose: () => {
      for (const t of facades) t.dispose();
      for (const t of storefronts) t.dispose();
    },
  };
}
