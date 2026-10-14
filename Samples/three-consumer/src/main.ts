import {
  AmbientLight,
  AxesHelper,
  Box3,
  Color,
  DirectionalLight,
  GridHelper,
  Group,
  MathUtils,
  Mesh,
  PerspectiveCamera,
  SRGBColorSpace,
  Scene,
  Sphere,
  Vector3,
  WebGLRenderer,
} from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import {
  applyShapePatch,
  compileShapeDefinition,
  getRigTarget,
  type ShapeDefinition,
  type ShapeGameMetadata,
} from '@scenereplay/three-adapter';
import './style.css';

const scene = new Scene();
scene.background = new Color('#20242b');
scene.add(new GridHelper(5, 10), new AxesHelper(1));
scene.add(new AmbientLight(0xffffff, 1.5));
const keyLight = new DirectionalLight(0xffffff, 3);
keyLight.position.set(3, 5, 4);
scene.add(keyLight);
type ExampleId = 'character' | 'weapon' | 'building' | 'profiles' | 'rigged' | 'workbench' | 'town';
type PaletteStyle = {
  id: string;
  palette: { entries: Array<{ role: string; color: { r: number; g: number; b: number } }> };
};
type LoadedExample = {
  definition: ShapeDefinition;
  metadata?: ShapeGameMetadata;
  style?: PaletteStyle;
};
const examples: Record<ExampleId, () => Promise<LoadedExample>> = {
  character: async () => {
    const [definition, metadata] = await Promise.all([import('../scene.json'), import('../game-metadata.json')]);
    return { definition: definition.default as ShapeDefinition, metadata: metadata.default as ShapeGameMetadata };
  },
  weapon: async () => {
    const [definition, metadata] = await Promise.all([import('../examples/weapon.json'), import('../examples/weapon.game-metadata.json')]);
    return { definition: definition.default as ShapeDefinition, metadata: metadata.default as ShapeGameMetadata };
  },
  building: async () => {
    const [definition, metadata] = await Promise.all([import('../examples/building.json'), import('../examples/building.game-metadata.json')]);
    return { definition: definition.default as ShapeDefinition, metadata: metadata.default as ShapeGameMetadata };
  },
  profiles: async () => ({ definition: (await import('../examples/profiles.json')).default as ShapeDefinition }),
  rigged: async () => ({ definition: (await import('../examples/rigged-character.json')).default as ShapeDefinition }),
  workbench: async () => {
    const [definition, style] = await Promise.all([import('../examples/inventor-workbench.json'), import('../examples/inventor-workbench.style.json')]);
    return { definition: definition.default as ShapeDefinition, style: style.default as PaletteStyle };
  },
  town: async () => {
    const [definition, style] = await Promise.all([import('../examples/japanese-town.json'), import('../examples/japanese-town.style.json')]);
    return { definition: definition.default as ShapeDefinition, style: style.default as PaletteStyle };
  },
};

const camera = new PerspectiveCamera(45, window.innerWidth / window.innerHeight, 0.1, 100);
camera.position.set(3, 2.5, 4);
camera.lookAt(0, 0.8, 0);

const renderer = new WebGLRenderer({ antialias: true });
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.outputColorSpace = SRGBColorSpace;
document.body.appendChild(renderer.domElement);
const controls = new OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;
controls.update();
// The shared Low Poly presets place their readable facade on negative Z.
const cameraDirection = new Vector3(1, 0.72, -1).normalize();

function fitView(): void {
  if (!currentModel) return;
  currentModel.updateMatrixWorld(true);
  const bounds = new Box3().setFromObject(currentModel);
  const sphere = bounds.getBoundingSphere(new Sphere());
  const radius = Math.max(sphere.radius, 0.5);
  const verticalFov = MathUtils.degToRad(camera.fov);
  const horizontalFov = 2 * Math.atan(Math.tan(verticalFov / 2) * camera.aspect);
  const limitingFov = Math.min(verticalFov, horizontalFov);
  const distance = (radius / Math.sin(limitingFov / 2)) * 1.25;

  controls.target.copy(sphere.center);
  camera.position.copy(sphere.center).addScaledVector(cameraDirection, distance);
  camera.near = Math.max(radius / 1000, 0.01);
  camera.far = Math.max(distance + radius * 5, 100);
  camera.updateProjectionMatrix();
  controls.minDistance = radius * 0.55;
  controls.maxDistance = distance * 5;
  controls.update();
}

let currentModel: Group | undefined;
let armRaised = false;
let loadSequence = 0;
async function showExample(exampleId: ExampleId): Promise<void> {
  const sequence = ++loadSequence;
  document.querySelector('#status')!.textContent = 'Loading SceneReplay definition…';
  try {
    const example = await examples[exampleId]();
    if (sequence !== loadSequence) return;
    const nextModel = compileShapeDefinition(example.definition, {
      gameMetadata: example.metadata,
      resolveColorRole: (role, styleId) => {
        if (!example.style || example.style.id !== styleId) return undefined;
        const entry = example.style.palette.entries.find(candidate => candidate.role === role);
        return entry ? new Color(entry.color.r, entry.color.g, entry.color.b) : undefined;
      },
    });
    if (sequence !== loadSequence) {
      disposeModel(nextModel);
      return;
    }
    if (currentModel) {
      scene.remove(currentModel);
      disposeModel(currentModel);
    }
    currentModel = nextModel;
    armRaised = false;
    document.querySelector<HTMLButtonElement>('#patch-toggle')!.hidden = exampleId !== 'character';
    document.querySelector<HTMLButtonElement>('#rig-target')!.hidden = exampleId !== 'rigged';
    document.querySelector<HTMLButtonElement>('#patch-toggle')!.textContent = 'Raise arm with semantic patch';
    scene.add(currentModel);
    document.querySelector('#status')!.textContent =
      `${example.definition.name} · ${countAuthoredNodes(example.definition)} authored nodes · ${countMirrors(example.definition)} mirrored instances`;
    fitView();
  } catch (error) {
    if (sequence === loadSequence) {
      document.querySelector('#status')!.textContent = `Could not load ${exampleId}: ${error instanceof Error ? error.message : String(error)}`;
      console.error(error);
    }
  }
}

function disposeModel(model: Group): void {
  model.traverse(object => {
    if (object instanceof Mesh) {
      object.geometry.dispose();
      const materials = Array.isArray(object.material) ? object.material : [object.material];
      materials.forEach(material => material.dispose());
    }
  });
}

function countAuthoredNodes(definition: ShapeDefinition): number {
  let count = 0;
  const visit = (node: ShapeDefinition['root']) => {
    count += 1;
    node.children?.forEach(visit);
  };
  visit(definition.root);
  return count;
}

function countMirrors(definition: ShapeDefinition): number {
  let count = 0;
  const visit = (node: ShapeDefinition['root']) => {
    if (node.mirrorAxis && node.mirrorAxis !== 'none') count += 1;
    node.children?.forEach(visit);
  };
  visit(definition.root);
  return count;
}

document.querySelector<HTMLButtonElement>('#fit-view')!.addEventListener('click', fitView);

document.querySelector<HTMLButtonElement>('#save-png')!.addEventListener('click', () => {
  renderer.render(scene, camera);
  renderer.domElement.toBlob(blob => {
    if (!blob) throw new Error('The current Three.js frame could not be saved as PNG.');
    const downloadUrl = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = `scenereplay-${picker.value}.png`;
    link.click();
    URL.revokeObjectURL(downloadUrl);
  }, 'image/png');
});

document.querySelector<HTMLButtonElement>('#rig-target')!.addEventListener('click', () => {
  if (!currentModel) return;
  const target = getRigTarget(currentModel, 'humanoid/head');
  if (!target) throw new Error('The semantic rig fixture has no humanoid/head target.');
  document.querySelector('#status')!.textContent =
    `humanoid/head → ${target.name} (${target.userData.sceneReplay.id}) · rotation limits remain attached as metadata`;
});

document.querySelector<HTMLButtonElement>('#patch-toggle')!.addEventListener('click', () => {
  if (!currentModel) return;
  const arm = findNodeById(currentModel, 'sample.arm');
  const nextX = armRaised ? 0.55 : 0.9;
  applyShapePatch(currentModel, {
    schema: 'scenereplay.patch/1.0',
    operations: [{
      kind: 'updateNode',
      nodeId: 'sample.arm',
      parentId: '',
      siblingIndex: -1,
      update: {
        transform: {
          position: { x: nextX, y: 0.2, z: 0 },
          eulerAngles: { x: 0, y: 0, z: -65 },
          scale: { x: 0.22, y: 0.42, z: 0.22 },
        },
      },
    }],
  });
  if (findNodeById(currentModel, 'sample.arm') !== arm) throw new Error('Semantic patch replaced a compatible Three.js object.');
  armRaised = !armRaised;
  document.querySelector<HTMLButtonElement>('#patch-toggle')!.textContent =
    armRaised ? 'Restore arm with semantic patch' : 'Raise arm with semantic patch';
});

function findNodeById(root: Group, id: string): Mesh | undefined {
  let match: Mesh | undefined;
  root.traverse(object => {
    if (object instanceof Mesh && object.userData.sceneReplay?.id === id) match = object;
  });
  return match;
}

const picker = document.querySelector<HTMLSelectElement>('#example-picker')!;
const requestedExample = new URLSearchParams(window.location.search).get('example');
if (requestedExample && requestedExample in examples) picker.value = requestedExample;
picker.addEventListener('change', () => { void showExample(picker.value as ExampleId); });
void showExample(picker.value as ExampleId);
renderer.setAnimationLoop(() => {
  controls.update();
  renderer.render(scene, camera);
});

window.addEventListener('resize', () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});
