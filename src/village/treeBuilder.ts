/**
 * Believable procedural trees (real-world design lock): a tapered trunk,
 * several primary branches angling up and out, and multiple overlapping
 * irregular leaf clusters — never a single sphere or cone canopy.
 *
 * Each variant is returned as two merged geometries (bark, foliage) so a
 * whole forest renders as a handful of InstancedMesh draws. Base tree is
 * ~7.5 m tall, real scale; callers scale 0.8-1.4 for natural variation.
 */
import * as THREE from "three";
import { mergeGeometries } from "three/examples/jsm/utils/BufferGeometryUtils.js";
import { mulberry32 } from "./seed";

const UP = new THREE.Vector3(0, 1, 0);

/** A tapered cylinder spanning A->B, radii at each end. */
function limb(
  ax: number, ay: number, az: number,
  bx: number, by: number, bz: number,
  rBottom: number, rTop: number,
  radial = 6,
): THREE.BufferGeometry {
  const a = new THREE.Vector3(ax, ay, az);
  const b = new THREE.Vector3(bx, by, bz);
  const dir = new THREE.Vector3().subVectors(b, a);
  const len = dir.length();
  const geo = new THREE.CylinderGeometry(rTop, rBottom, len, radial, 1);
  geo.translate(0, len / 2, 0); // base at origin
  const q = new THREE.Quaternion().setFromUnitVectors(UP, dir.normalize());
  geo.applyQuaternion(q);
  geo.translate(a.x, a.y, a.z);
  return geo;
}

/** An irregular leaf cluster: low-poly icosahedron pushed around by noise so
 * it reads as a clump of foliage, not a ball. Vertex colours darken toward
 * the underside. */
function cluster(
  cx: number, cy: number, cz: number,
  radius: number,
  rand: () => number,
): THREE.BufferGeometry {
  const geo = new THREE.IcosahedronGeometry(radius, 1);
  const pos = geo.getAttribute("position") as THREE.BufferAttribute;
  const colors: number[] = [];
  const base = new THREE.Color("#4a7a34");
  const dark = new THREE.Color("#2f5522");
  const v = new THREE.Vector3();
  for (let i = 0; i < pos.count; i++) {
    v.fromBufferAttribute(pos, i);
    // Push each vertex out/in irregularly, and squash vertically a touch.
    const j = 0.62 + rand() * 0.5;
    v.multiplyScalar(j);
    v.y *= 0.82;
    pos.setXYZ(i, v.x, v.y, v.z);
    const shade = THREE.MathUtils.clamp((v.y / radius) * 0.5 + 0.5, 0, 1);
    const c = dark.clone().lerp(base, shade);
    colors.push(c.r, c.g, c.b);
  }
  geo.setAttribute("color", new THREE.Float32BufferAttribute(colors, 3));
  geo.deleteAttribute("uv");
  geo.computeVertexNormals();
  geo.translate(cx, cy, cz);
  return geo;
}

interface TreeGeo {
  bark: THREE.BufferGeometry;
  foliage: THREE.BufferGeometry;
}

/** Build one tree. `broadleaf` chooses a rounded crown vs a taller conifer. */
function buildTree(rand: () => number, broadleaf: boolean): TreeGeo {
  const barkParts: THREE.BufferGeometry[] = [];
  const foliageParts: THREE.BufferGeometry[] = [];

  const trunkH = broadleaf ? 2.4 + rand() * 0.8 : 3.0 + rand() * 0.9;
  const rBase = broadleaf ? 0.26 : 0.22;
  const rTop = rBase * 0.55;
  // Slight trunk lean for naturalness.
  const leanX = (rand() - 0.5) * 0.4;
  const leanZ = (rand() - 0.5) * 0.4;
  barkParts.push(limb(0, 0, 0, leanX, trunkH, leanZ, rBase, rTop, 7));
  const topX = leanX;
  const topZ = leanZ;

  if (broadleaf) {
    // 4-6 primary branches fanning from the upper trunk.
    const n = 4 + Math.floor(rand() * 3);
    const tips: Array<[number, number, number, number]> = [];
    for (let i = 0; i < n; i++) {
      const ang = (i / n) * Math.PI * 2 + rand() * 0.6;
      const startY = trunkH * (0.6 + rand() * 0.3);
      const spread = 1.4 + rand() * 1.1;
      const rise = 1.6 + rand() * 1.3;
      const bx = topX + Math.cos(ang) * spread;
      const bz = topZ + Math.sin(ang) * spread;
      const by = startY + rise;
      barkParts.push(limb(topX, startY, topZ, bx, by, bz, rTop * 0.8, rTop * 0.4, 5));
      tips.push([bx, by, bz, 1.0 + rand() * 0.4]);
    }
    // Foliage: a clump at each branch tip plus a big crown centre.
    const crownY = trunkH + 1.9 + rand() * 0.6;
    foliageParts.push(cluster(topX, crownY, topZ, 1.7 + rand() * 0.4, rand));
    for (const [bx, by, bz, cr] of tips) {
      foliageParts.push(cluster(bx, by + 0.3, bz, 1.1 * cr, rand));
      if (rand() < 0.6) {
        foliageParts.push(
          cluster(bx * 0.6 + topX * 0.4, by + 0.9, bz * 0.6 + topZ * 0.4, 1.0 * cr, rand),
        );
      }
    }
  } else {
    // Conifer: whorls of short upswept branches, stacked clusters forming a
    // tapered but irregular column of foliage.
    const layers = 4 + Math.floor(rand() * 2);
    for (let l = 0; l < layers; l++) {
      const t = l / (layers - 1);
      const y = trunkH * 0.5 + t * (trunkH * 1.9);
      const r = (1.5 - t) * (1.1 + rand() * 0.3);
      const perLayer = 3 + Math.floor(rand() * 2);
      for (let i = 0; i < perLayer; i++) {
        const ang = (i / perLayer) * Math.PI * 2 + l * 0.7;
        const cx = topX + Math.cos(ang) * r * 0.5;
        const cz = topZ + Math.sin(ang) * r * 0.5;
        foliageParts.push(cluster(cx, y, cz, Math.max(0.5, r * 0.9), rand));
      }
    }
  }

  const bark = mergeGeometries(barkParts, false)!;
  bark.computeVertexNormals();
  for (const g of barkParts) g.dispose();
  const foliage = mergeGeometries(foliageParts, false)!;
  for (const g of foliageParts) g.dispose();
  return { bark, foliage };
}

export interface TreeVariant {
  bark: THREE.BufferGeometry;
  foliage: THREE.BufferGeometry;
}

/** A set of distinct tree geometries for instancing. Mix of broadleaf and
 * conifer so a forest has believable species variation. */
export function buildTreeVariants(count: number, seed: number): TreeVariant[] {
  const rand = mulberry32(seed ^ 0x7ee5);
  const out: TreeVariant[] = [];
  for (let i = 0; i < count; i++) {
    out.push(buildTree(rand, i % 3 !== 0)); // ~1 in 3 is a conifer
  }
  return out;
}

export const BARK_MATERIAL = () =>
  new THREE.MeshStandardMaterial({ color: "#6a4a30", roughness: 0.95 });

export const FOLIAGE_MATERIAL = () =>
  new THREE.MeshStandardMaterial({
    vertexColors: true,
    roughness: 0.85,
    flatShading: true,
  });
