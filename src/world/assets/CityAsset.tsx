import { Component, Suspense, useMemo, type ReactNode } from "react";
import * as THREE from "three";
import { useGLTF } from "@react-three/drei";
import { getAsset, type CityAssetDefinition } from "./AssetManifest";

class AssetBoundary extends Component<
  { fallback: ReactNode; children: ReactNode },
  { failed: boolean }
> {
  state = { failed: false };
  static getDerivedStateFromError() {
    return { failed: true };
  }
  render() {
    return this.state.failed ? this.props.fallback : this.props.children;
  }
}

function GltfAsset({ def }: { def: CityAssetDefinition }) {
  const gltf = useGLTF(def.path);
  const object = useMemo(() => {
    const clone = gltf.scene.clone(true);
    clone.traverse((node) => {
      node.castShadow = true;
      node.receiveShadow = true;
    });
    let scale = def.scale;
    const box = new THREE.Box3().setFromObject(clone);
    const size = box.getSize(new THREE.Vector3());
    if (def.normalizeHeight && size.y > 0) {
      scale = def.normalizeHeight / size.y;
    }
    clone.scale.setScalar(scale);
    // rest the model on y=0, centered on its definition point
    box.setFromObject(clone);
    const center = box.getCenter(new THREE.Vector3());
    clone.position.x -= center.x;
    clone.position.z -= center.z;
    clone.position.y -= box.min.y;
    return clone;
  }, [gltf.scene, def]);
  return (
    <group position={def.position} rotation={def.rotation}>
      <primitive object={object} />
    </group>
  );
}

interface CityAssetProps {
  assetId: string;
  /**
   * Procedural stand-in rendered while loading, on load failure, or when the
   * manifest marks the asset missing. Fallbacks position themselves in world
   * space; the GLB path applies the manifest transform instead.
   */
  fallback: ReactNode;
}

export function CityAsset({ assetId, fallback }: CityAssetProps) {
  const def = getAsset(assetId);
  if (def.status !== "installed") {
    return <>{fallback}</>;
  }
  return (
    <AssetBoundary fallback={fallback}>
      <Suspense fallback={fallback}>
        <GltfAsset def={def} />
      </Suspense>
    </AssetBoundary>
  );
}
