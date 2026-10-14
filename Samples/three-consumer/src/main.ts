import {
  AmbientLight,
  AxesHelper,
  Color,
  DirectionalLight,
  GridHelper,
  PerspectiveCamera,
  SRGBColorSpace,
  Scene,
  WebGLRenderer,
} from 'three';
import { compileShapeDefinition, type ShapeDefinition } from '@scenereplay/three-adapter';
import shapeDefinition from '../scene.json';
import './style.css';

const scene = new Scene();
scene.background = new Color('#20242b');
scene.add(new GridHelper(5, 10), new AxesHelper(1));
scene.add(new AmbientLight(0xffffff, 1.5));
const keyLight = new DirectionalLight(0xffffff, 3);
keyLight.position.set(3, 5, 4);
scene.add(keyLight);
scene.add(compileShapeDefinition(shapeDefinition as ShapeDefinition));

const camera = new PerspectiveCamera(45, window.innerWidth / window.innerHeight, 0.1, 100);
camera.position.set(3, 2.5, 4);
camera.lookAt(0, 0.8, 0);

const renderer = new WebGLRenderer({ antialias: true });
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.outputColorSpace = SRGBColorSpace;
document.body.appendChild(renderer.domElement);
renderer.render(scene, camera);

window.addEventListener('resize', () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

document.querySelector('#status')!.textContent =
  `Compiled ${scene.getObjectByName('SceneReplay Sample')?.children.length ?? 0} semantic root node(s).`;
