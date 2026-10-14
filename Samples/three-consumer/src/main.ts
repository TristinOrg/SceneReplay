import {
  AmbientLight,
  AxesHelper,
  Color,
  DirectionalLight,
  GridHelper,
  Group,
  Mesh,
  PerspectiveCamera,
  SRGBColorSpace,
  Scene,
  WebGLRenderer,
} from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import {
  applyShapePatch,
  compileShapeDefinition,
  type ShapeDefinition,
  type ShapeGameMetadata,
} from '@scenereplay/three-adapter';
import shapeDefinition from '../scene.json';
import gameMetadata from '../game-metadata.json';
import weaponDefinition from '../examples/weapon.json';
import weaponMetadata from '../examples/weapon.game-metadata.json';
import buildingDefinition from '../examples/building.json';
import buildingMetadata from '../examples/building.game-metadata.json';
import profilesDefinition from '../examples/profiles.json';
import './style.css';

const scene = new Scene();
scene.background = new Color('#20242b');
scene.add(new GridHelper(5, 10), new AxesHelper(1));
scene.add(new AmbientLight(0xffffff, 1.5));
const keyLight = new DirectionalLight(0xffffff, 3);
keyLight.position.set(3, 5, 4);
scene.add(keyLight);
const examples = {
  character: { definition: shapeDefinition, metadata: gameMetadata },
  weapon: { definition: weaponDefinition, metadata: weaponMetadata },
  building: { definition: buildingDefinition, metadata: buildingMetadata },
  profiles: { definition: profilesDefinition, metadata: undefined },
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
controls.target.set(0, 0.9, 0);
controls.enableDamping = true;
controls.update();

let currentModel: Group | undefined;
let armRaised = false;
function showExample(exampleId: keyof typeof examples): void {
  if (currentModel) {
    scene.remove(currentModel);
    currentModel.traverse(object => {
      if (object instanceof Mesh) {
        object.geometry.dispose();
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        materials.forEach(material => material.dispose());
      }
    });
  }
  const example = examples[exampleId];
  currentModel = compileShapeDefinition(example.definition as ShapeDefinition, {
    gameMetadata: example.metadata as ShapeGameMetadata | undefined,
  });
  armRaised = false;
  document.querySelector<HTMLButtonElement>('#patch-toggle')!.hidden = exampleId !== 'character';
  document.querySelector<HTMLButtonElement>('#patch-toggle')!.textContent = 'Raise arm with semantic patch';
  scene.add(currentModel);
  document.querySelector('#status')!.textContent =
    `${example.definition.name} · ${currentModel.children.length} semantic root node(s)`;
}

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
picker.addEventListener('change', () => showExample(picker.value as keyof typeof examples));
showExample(picker.value as keyof typeof examples);
renderer.setAnimationLoop(() => {
  controls.update();
  renderer.render(scene, camera);
});

window.addEventListener('resize', () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});
