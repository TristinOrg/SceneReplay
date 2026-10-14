import type { ColorRepresentation, Group } from 'three';

export declare const adapterDescriptor: Readonly<{
  schema: 'scenereplay.adapter/1.0';
  adapterId: 'scenereplay/three';
  engineId: 'three';
  version: '0.1.0';
  features: readonly ['stable-node-ids', 'hierarchy', 'local-transforms'];
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
  children?: ShapeNode[];
}

export interface Vector3 {
  x: number;
  y: number;
  z: number;
}

export interface ThreeAdapterOptions {
  /** Resolves an authored semantic palette role to a Three.js color. */
  resolveColorRole?: (role: string, style: string | undefined) => ColorRepresentation | undefined;
}

/** Compiles a supported SceneReplay hierarchy into a native Three.js Group. */
export declare function compileShapeDefinition(
  definition: ShapeDefinition,
  options?: ThreeAdapterOptions,
): Group;
