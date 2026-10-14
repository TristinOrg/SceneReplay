import type { ColorRepresentation, Group } from 'three';

export declare const adapterDescriptor: Readonly<{
  schema: 'scenereplay.adapter/1.0';
  adapterId: 'scenereplay/three';
  engineId: 'three';
  version: '0.1.0';
  features: readonly ['stable-node-ids', 'hierarchy', 'local-transforms', 'game-metadata'];
  unsupportedFeaturePolicy: 'reject';
}>;

export interface ShapeDefinition {
  schema: 'scenereplay.shape/1.0';
  name: string;
  style?: string;
  rig?: unknown;
  root: ShapeNode;
}

export interface ShapeNode {
  id: string;
  name: string;
  type: string;
  mirrorAxis?: string;
  transform: {
    position: Vector3;
    eulerAngles: Vector3;
    scale: Vector3;
  };
  appearance?: {
    colorRole?: string;
    hasColorOverride?: boolean;
    color?: { r: number; g: number; b: number; a: number };
  };
  parameters?: Record<string, number>;
  /** Closed 2D contour used by extrude, loft, lathe, and sweep geometry. */
  profile?: Vector2[];
  /** Ordered 3D centerline used by profile sweeps. */
  path?: Vector3[];
  /** Ordered cross-section transforms used by profile lofts. */
  profileSections?: Array<{ z: number; scale: Vector2; offset: Vector2 }>;
  /** Ordered 2D contours used by profile cages. */
  profileCageSections?: Array<{ z: number; profile: Vector2[] }>;
  children?: ShapeNode[];
}

export interface Vector2 {
  x: number;
  y: number;
}

export interface Vector3 {
  x: number;
  y: number;
  z: number;
}

export interface ThreeAdapterOptions {
  /** Resolves an authored semantic palette role to a Three.js color. */
  resolveColorRole?: (role: string, style: string | undefined) => ColorRepresentation | undefined;
  /** Validated gameplay semantics to preserve alongside the compiled hierarchy. */
  gameMetadata?: ShapeGameMetadata;
}

export interface ShapeGameMetadata {
  schema: 'scenereplay.game-metadata/1.0';
  id: string;
  anchors: Array<{
    id: string;
    role: string;
    nodeId: string;
    transform: { position: Vector3; eulerAngles: Vector3; scale: Vector3 };
    tags: string[];
  }>;
  damageZones: Array<{ id: string; nodeId: string; multiplier: number; tags: string[] }>;
  colliders: Array<{
    id: string;
    nodeId: string;
    kind: 'box' | 'sphere' | 'capsule' | 'mesh';
    isTrigger: boolean;
    center: Vector3;
    size: Vector3;
    radius: number;
    height: number;
  }>;
  lods: Array<{ level: number; screenRelativeHeight: number; nodeIds: string[] }>;
  tags: string[];
}

/** Compiles a supported SceneReplay hierarchy into a native Three.js Group. */
export declare function compileShapeDefinition(
  definition: ShapeDefinition,
  options?: ThreeAdapterOptions,
): Group;

/** Validates and preserves gameplay metadata on a compiled Three.js hierarchy. */
export declare function applyGameMetadata(compiledRoot: Group, metadata: ShapeGameMetadata): Group;
