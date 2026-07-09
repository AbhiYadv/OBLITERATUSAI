import { useMemo } from "react";
import * as THREE from "three";

/**
 * Background skyline: cheap textured towers for depth behind the hero block.
 * Hero buildings live in src/world/buildings/ and swap to GLB assets via the
 * manifest; the skyline stays procedural by design.
 */

function useFacadeTexture(body: string, win: string, lit: string, seed: number) {
  return useMemo(() => {
    const canvas = document.createElement("canvas");
    canvas.width = 128;
    canvas.height = 256;
    const ctx = canvas.getContext("2d")!;
    ctx.fillStyle = body;
    ctx.fillRect(0, 0, 128, 256);
    let n = seed;
    const rand = () => {
      n = (n * 1103515245 + 12345) % 2147483648;
      return n / 2147483648;
    };
    for (let y = 8; y < 244; y += 18) {
      for (let x = 8; x < 116; x += 16) {
        ctx.fillStyle = rand() < 0.16 ? lit : win;
        ctx.fillRect(x, y, 9, 10);
      }
    }
    const texture = new THREE.CanvasTexture(canvas);
    texture.colorSpace = THREE.SRGBColorSpace;
    texture.wrapS = THREE.RepeatWrapping;
    texture.wrapT = THREE.RepeatWrapping;
    return texture;
  }, [body, win, lit, seed]);
}

function SkylineTower({
  x,
  z,
  w,
  d,
  h,
  body,
  seed,
}: {
  x: number;
  z: number;
  w: number;
  d: number;
  h: number;
  body: string;
  seed: number;
}) {
  const texture = useFacadeTexture(body, "#232c34", "#e7c185", seed);
  texture.repeat.set(Math.max(1, Math.round(w / 12)), Math.max(1, Math.round(h / 24)));
  return (
    <mesh position={[x, h / 2, z]} castShadow>
      <boxGeometry args={[w, h, d]} />
      <meshStandardMaterial map={texture} color="#ffffff" roughness={0.85} />
    </mesh>
  );
}

export function Skyline() {
  return (
    <group>
      <SkylineTower x={-58} z={-50} w={14} d={14} h={26} body="#7d838c" seed={3} />
      <SkylineTower x={-38} z={-54} w={12} d={12} h={34} body="#6a7078" seed={11} />
      <SkylineTower x={-18} z={-48} w={16} d={14} h={22} body="#8b8e94" seed={7} />
      <SkylineTower x={2} z={-56} w={14} d={12} h={38} body="#757a83" seed={19} />
      <SkylineTower x={22} z={-50} w={12} d={12} h={28} body="#82868d" seed={5} />
      <SkylineTower x={42} z={-56} w={16} d={14} h={42} body="#6e737b" seed={13} />
      <SkylineTower x={64} z={-48} w={14} d={12} h={24} body="#878b91" seed={17} />
      {/* fillers framing the block so street edges are not empty */}
      <SkylineTower x={-54} z={24} w={18} d={14} h={14} body="#8f8c86" seed={23} />
      <SkylineTower x={-74} z={30} w={16} d={16} h={20} body="#7a7f88" seed={29} />
      <SkylineTower x={66} z={28} w={14} d={12} h={16} body="#84888e" seed={31} />
    </group>
  );
}
