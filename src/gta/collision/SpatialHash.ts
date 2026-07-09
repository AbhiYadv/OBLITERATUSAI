import type { CollisionAabb } from "./CollisionTypes";

export class SpatialHash {
  private readonly cells = new Map<string, Set<string>>();
  private readonly objectCells = new Map<string, string[]>();

  constructor(private readonly cellSize = 12) {}

  insert(id: string, aabb: CollisionAabb): void {
    this.remove(id);
    const keys = this.keysFor(aabb);
    this.objectCells.set(id, keys);
    for (const key of keys) {
      let bucket = this.cells.get(key);
      if (!bucket) {
        bucket = new Set();
        this.cells.set(key, bucket);
      }
      bucket.add(id);
    }
  }

  remove(id: string): void {
    const keys = this.objectCells.get(id);
    if (!keys) return;
    for (const key of keys) {
      const bucket = this.cells.get(key);
      if (!bucket) continue;
      bucket.delete(id);
      if (bucket.size === 0) this.cells.delete(key);
    }
    this.objectCells.delete(id);
  }

  query(aabb: CollisionAabb): string[] {
    const out = new Set<string>();
    for (const key of this.keysFor(aabb)) {
      const bucket = this.cells.get(key);
      if (!bucket) continue;
      for (const id of bucket) out.add(id);
    }
    return [...out];
  }

  clear(): void {
    this.cells.clear();
    this.objectCells.clear();
  }

  private keysFor(aabb: CollisionAabb): string[] {
    const minX = Math.floor(aabb.minX / this.cellSize);
    const maxX = Math.floor(aabb.maxX / this.cellSize);
    const minZ = Math.floor(aabb.minZ / this.cellSize);
    const maxZ = Math.floor(aabb.maxZ / this.cellSize);
    const keys: string[] = [];
    for (let x = minX; x <= maxX; x++) {
      for (let z = minZ; z <= maxZ; z++) keys.push(`${x}:${z}`);
    }
    return keys;
  }
}
