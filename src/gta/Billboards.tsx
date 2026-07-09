/**
 * LED billboards on the downtown towers — the Nexo-city identity layer
 * from the concept art: marketplace, API, cloud, auth branding rendered as
 * glowing screen quads mounted proud of the facades. Quads are merged per
 * design (8 draw calls, unlit materials read as emissive LED).
 */
import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { mulberry32 } from "../village/seed";
import { SLAB_TOP, type CityPlan } from "./cityPlan";

const TEX_W = 512;
const TEX_H = 288;

interface BoardDesign {
  draw: (ctx: CanvasRenderingContext2D, rand: () => number) => void;
}

function title(ctx: CanvasRenderingContext2D, text: string, color: string, y = TEX_H / 2, size = 56) {
  ctx.font = `bold ${size}px 'Helvetica Neue', Arial, sans-serif`;
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillStyle = color;
  ctx.fillText(text, TEX_W / 2, y, TEX_W - 50);
}

const DESIGNS: BoardDesign[] = [
  {
    // NEXO MARKETPLACE | MCP TOOLKITS — white/blue with pink block.
    draw: (ctx) => {
      ctx.fillStyle = "#10233f";
      ctx.fillRect(0, 0, TEX_W, TEX_H);
      ctx.fillStyle = "#f2f5f8";
      ctx.fillRect(0, 0, TEX_W * 0.68, TEX_H);
      ctx.fillStyle = "#e0348c";
      ctx.fillRect(TEX_W * 0.68, 0, TEX_W * 0.32, TEX_H);
      ctx.font = "bold 44px 'Helvetica Neue', Arial, sans-serif";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillStyle = "#12325c";
      ctx.fillText("NEXO", TEX_W * 0.34, TEX_H * 0.32);
      ctx.fillText("MARKETPLACE", TEX_W * 0.34, TEX_H * 0.55);
      ctx.font = "bold 30px Arial";
      ctx.fillStyle = "#5b6c80";
      ctx.fillText("MCP", TEX_W * 0.34, TEX_H * 0.8);
      ctx.fillStyle = "#ffffff";
      ctx.font = "bold 38px Arial";
      ctx.save();
      ctx.translate(TEX_W * 0.84, TEX_H / 2);
      ctx.fillText("TOOL", 0, -24);
      ctx.fillText("KITS", 0, 24);
      ctx.restore();
    },
  },
  {
    // API INTEGRATIONS — navy with cyan glow.
    draw: (ctx, rand) => {
      const g = ctx.createLinearGradient(0, 0, 0, TEX_H);
      g.addColorStop(0, "#0a1e38");
      g.addColorStop(1, "#0e2c52");
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, TEX_W, TEX_H);
      ctx.strokeStyle = "rgba(64,200,255,0.35)";
      ctx.lineWidth = 2;
      for (let i = 0; i < 7; i++) {
        ctx.beginPath();
        ctx.moveTo(rand() * TEX_W, TEX_H);
        ctx.lineTo(rand() * TEX_W, rand() * TEX_H * 0.5);
        ctx.stroke();
      }
      title(ctx, "API", "#63e0ff", TEX_H * 0.36, 84);
      title(ctx, "INTEGRATIONS", "#e8f6ff", TEX_H * 0.72, 46);
    },
  },
  {
    // CLOUD COMPUTE STREAMS.
    draw: (ctx) => {
      const g = ctx.createLinearGradient(0, 0, TEX_W, TEX_H);
      g.addColorStop(0, "#1450a0");
      g.addColorStop(1, "#2a7ad4");
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, TEX_W, TEX_H);
      ctx.fillStyle = "rgba(255,255,255,0.85)";
      ctx.beginPath();
      ctx.arc(TEX_W * 0.5, TEX_H * 0.3, 34, 0, Math.PI * 2);
      ctx.arc(TEX_W * 0.42, TEX_H * 0.34, 26, 0, Math.PI * 2);
      ctx.arc(TEX_W * 0.58, TEX_H * 0.34, 26, 0, Math.PI * 2);
      ctx.fill();
      title(ctx, "CLOUD COMPUTE", "#ffffff", TEX_H * 0.62, 44);
      title(ctx, "STREAMS", "#cfe6ff", TEX_H * 0.84, 40);
    },
  },
  {
    // SECURE AUTHENTICATION HUB.
    draw: (ctx) => {
      ctx.fillStyle = "#131a33";
      ctx.fillRect(0, 0, TEX_W, TEX_H);
      ctx.strokeStyle = "#57c8ff";
      ctx.lineWidth = 7;
      ctx.beginPath();
      ctx.arc(TEX_W / 2, TEX_H / 2, TEX_H * 0.42, 0, Math.PI * 2);
      ctx.stroke();
      title(ctx, "SECURE", "#e8f4ff", TEX_H * 0.34, 42);
      title(ctx, "AUTHENTICATION", "#57c8ff", TEX_H * 0.54, 38);
      title(ctx, "HUB", "#e8f4ff", TEX_H * 0.74, 42);
    },
  },
  {
    // PostgreSQL Data Services.
    draw: (ctx) => {
      ctx.fillStyle = "#eef2f5";
      ctx.fillRect(0, 0, TEX_W, TEX_H);
      ctx.fillStyle = "#2f5f8f";
      ctx.fillRect(0, 0, TEX_W, 14);
      ctx.fillRect(0, TEX_H - 14, TEX_W, 14);
      title(ctx, "PostgreSQL", "#27486b", TEX_H * 0.4, 60);
      title(ctx, "Data Services", "#4a80b4", TEX_H * 0.7, 40);
    },
  },
  {
    // docker Logistics.
    draw: (ctx) => {
      ctx.fillStyle = "#0d3a5f";
      ctx.fillRect(0, 0, TEX_W, TEX_H);
      ctx.fillStyle = "#5ad1f0";
      for (let i = 0; i < 3; i++) ctx.fillRect(TEX_W * 0.42 + i * 30, TEX_H * 0.2, 24, 18);
      for (let i = 0; i < 2; i++) ctx.fillRect(TEX_W * 0.455 + i * 30, TEX_H * 0.2 - 24, 24, 18);
      title(ctx, "docker", "#ffffff", TEX_H * 0.55, 64);
      title(ctx, "Logistics", "#9fdcf2", TEX_H * 0.8, 38);
    },
  },
  {
    // AI RESEARCH CAMPUS — teal with node graph.
    draw: (ctx, rand) => {
      const g = ctx.createLinearGradient(0, 0, TEX_W, 0);
      g.addColorStop(0, "#0d3d3a");
      g.addColorStop(1, "#14615c");
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, TEX_W, TEX_H);
      const nodes: Array<[number, number]> = [];
      for (let i = 0; i < 9; i++) nodes.push([rand() * TEX_W, rand() * TEX_H * 0.45]);
      ctx.strokeStyle = "rgba(120,255,220,0.3)";
      for (let i = 1; i < nodes.length; i++) {
        ctx.beginPath();
        ctx.moveTo(nodes[i - 1][0], nodes[i - 1][1]);
        ctx.lineTo(nodes[i][0], nodes[i][1]);
        ctx.stroke();
      }
      ctx.fillStyle = "rgba(120,255,220,0.6)";
      for (const [x, y] of nodes) {
        ctx.beginPath();
        ctx.arc(x, y, 5, 0, Math.PI * 2);
        ctx.fill();
      }
      title(ctx, "AI RESEARCH", "#eafff8", TEX_H * 0.62, 46);
      title(ctx, "CAMPUS", "#7dffdc", TEX_H * 0.84, 40);
    },
  },
  {
    // ANALYTICS SPIRE — dark with bar chart.
    draw: (ctx, rand) => {
      ctx.fillStyle = "#101820";
      ctx.fillRect(0, 0, TEX_W, TEX_H);
      for (let i = 0; i < 10; i++) {
        const h = 30 + rand() * 90;
        ctx.fillStyle = i % 3 === 0 ? "#57c8ff" : "#2b7fb8";
        ctx.fillRect(40 + i * 44, TEX_H * 0.55 - h, 26, h);
      }
      title(ctx, "ANALYTICS SPIRE", "#d9f2ff", TEX_H * 0.78, 44);
    },
  },
];

function makeBoardTexture(design: BoardDesign, rand: () => number): THREE.CanvasTexture {
  const canvas = document.createElement("canvas");
  canvas.width = TEX_W;
  canvas.height = TEX_H;
  const ctx = canvas.getContext("2d")!;
  design.draw(ctx, rand);
  // LED scanlines + bezel.
  ctx.fillStyle = "rgba(0,0,0,0.1)";
  for (let y = 0; y < TEX_H; y += 4) ctx.fillRect(0, y, TEX_W, 1);
  ctx.strokeStyle = "#0b0e12";
  ctx.lineWidth = 10;
  ctx.strokeRect(0, 0, TEX_W, TEX_H);
  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 4;
  return tex;
}

interface BoardQuad {
  design: number;
  cx: number;
  cy: number;
  cz: number;
  nx: number;
  nz: number;
  w: number;
  h: number;
}

export function Billboards({ plan, seed }: { plan: CityPlan; seed: number }) {
  const built = useMemo(() => {
    const rand = mulberry32(seed ^ 0x1edb0a);
    const towers = plan.buildings
      .filter((b) => !b.construction && b.h > 40)
      .sort((a, b) => b.h - a.h)
      .slice(0, 16);

    const quads: BoardQuad[] = [];
    const faceDirs: Array<[number, number]> = [
      [1, 0],
      [-1, 0],
      [0, 1],
      [0, -1],
    ];
    towers.forEach((b, ti) => {
      const boards = ti < 3 ? 3 : ti < 8 ? 2 : 1;
      const faceStart = Math.floor(rand() * 4);
      for (let k = 0; k < boards; k++) {
        const [nx, nz] = faceDirs[(faceStart + k) % 4];
        const faceW = nx !== 0 ? b.d : b.w;
        const w = Math.min(faceW * 0.78, 15);
        const h = w * (TEX_H / TEX_W);
        const levels = [0.68, 0.42, 0.18];
        const cy = SLAB_TOP + Math.max(7 + h / 2, Math.min(b.h * levels[k % 3], b.h - h / 2 - 2));
        quads.push({
          design: Math.floor(rand() * DESIGNS.length),
          cx: b.x + nx * (b.w / 2 + 0.18),
          cy,
          cz: b.z + nz * (b.d / 2 + 0.18),
          nx,
          nz,
          w,
          h,
        });
      }
    });

    // Merge quads per design.
    const groups = DESIGNS.map(() => ({
      positions: [] as number[],
      uvs: [] as number[],
      indices: [] as number[],
    }));
    const up = new THREE.Vector3(0, 1, 0);
    const n = new THREE.Vector3();
    const right = new THREE.Vector3();
    for (const q of quads) {
      const g = groups[q.design];
      n.set(q.nx, 0, q.nz);
      right.copy(up).cross(n); // spans the face laterally
      const base = g.positions.length / 3;
      const hw = q.w / 2;
      const hh = q.h / 2;
      g.positions.push(
        q.cx - right.x * hw, q.cy - hh, q.cz - right.z * hw,
        q.cx + right.x * hw, q.cy - hh, q.cz + right.z * hw,
        q.cx + right.x * hw, q.cy + hh, q.cz + right.z * hw,
        q.cx - right.x * hw, q.cy + hh, q.cz - right.z * hw,
      );
      g.uvs.push(0, 0, 1, 0, 1, 1, 0, 1);
      g.indices.push(base, base + 1, base + 2, base, base + 2, base + 3);
    }

    return groups
      .map((g, i) => {
        if (g.indices.length === 0) return null;
        const geo = new THREE.BufferGeometry();
        geo.setAttribute("position", new THREE.Float32BufferAttribute(g.positions, 3));
        geo.setAttribute("uv", new THREE.Float32BufferAttribute(g.uvs, 2));
        geo.setIndex(g.indices);
        geo.computeBoundingSphere();
        const tex = makeBoardTexture(DESIGNS[i], rand);
        const mat = new THREE.MeshBasicMaterial({ map: tex, side: THREE.DoubleSide });
        return { geo, mat, tex };
      })
      .filter((x): x is NonNullable<typeof x> => x !== null);
  }, [plan, seed]);

  useEffect(() => {
    return () => {
      for (const m of built) {
        m.geo.dispose();
        m.mat.dispose();
        m.tex.dispose();
      }
    };
  }, [built]);

  return (
    <group>
      {built.map((m, i) => (
        <mesh key={i} geometry={m.geo} material={m.mat} />
      ))}
    </group>
  );
}
