import {
  BoxGeometry,
  BufferGeometry,
  CapsuleGeometry,
  CylinderGeometry,
  CurvePath,
  ExtrudeGeometry,
  Float32BufferAttribute,
  LatheGeometry,
  LineCurve3,
  SphereGeometry,
  Shape,
  ShapeUtils,
  Vector2,
  Vector3,
} from 'three';

export const supportedLowPolyTypes = Object.freeze([
  'lowpoly/cube',
  'lowpoly/sphere',
  'lowpoly/cylinder',
  'lowpoly/capsule',
  'lowpoly/wedge',
  'lowpoly/frustum',
  'lowpoly/extruded-profile',
  'lowpoly/profile-loft',
  'lowpoly/profile-cage',
  'lowpoly/lathe-profile',
  'lowpoly/profile-sweep',
]);

export function validateLowPolyNode(node, path) {
  switch (node.type) {
    case 'lowpoly/frustum':
      for (const [key, fallback] of [['topWidth', 0.65], ['topDepth', 0.65], ['bottomWidth', 1], ['bottomDepth', 1]]) {
        positiveParameter(node, key, fallback, `${path}.parameters.${key}`);
      }
      break;
    case 'lowpoly/extruded-profile': {
      const profile = validateProfile(node.profile, 3, `${path}.profile`, true);
      const depth = positiveParameter(node, 'profileDepth', 0.2, `${path}.parameters.profileDepth`);
      const bevel = nonNegativeParameter(node, 'profileBevel', 0, `${path}.parameters.profileBevel`);
      if (bevel * 2 >= depth) throw new Error(`${path}.parameters.profileBevel must be less than half the extrusion depth.`);
      integerParameter(node, 'profileBevelSegments', 1, 1, 8, `${path}.parameters.profileBevelSegments`);
      const smoothing = integerParameter(node, 'profileSmoothing', 0, 0, 4, `${path}.parameters.profileSmoothing`);
      validateSmoothNormals(node, path);
      const bevelSegments = integerParameter(node, 'profileBevelSegments', 1, 1, 8, `${path}.parameters.profileBevelSegments`);
      assertVertexBudget(profile.length * (2 ** smoothing) * (2 + (bevelSegments * 2)) * 6, path);
      break;
    }
    case 'lowpoly/profile-loft': {
      const profile = validateProfile(node.profile, 3, `${path}.profile`, true);
      const sections = node.profileSections;
      if (!Array.isArray(sections) || sections.length < 2) throw new Error(`${path}.profileSections: at least two sections are required.`);
      let previousZ = Number.NEGATIVE_INFINITY;
      sections.forEach((section, index) => {
        const sectionPath = `${path}.profileSections[${index}]`;
        if (!section || !Number.isFinite(section.z) || section.z <= previousZ) {
          throw new Error(`${sectionPath}.z: section depths must be finite and strictly increasing.`);
        }
        previousZ = section.z;
        validatePositiveVector2(section.scale, `${sectionPath}.scale`);
        validateVector2(section.offset, `${sectionPath}.offset`);
      });
      const subdivisions = integerParameter(node, 'loftSubdivisions', 0, 0, 8, `${path}.parameters.loftSubdivisions`);
      const smoothing = integerParameter(node, 'profileSmoothing', 0, 0, 4, `${path}.parameters.profileSmoothing`);
      validateSmoothNormals(node, path);
      assertVertexBudget(profile.length * (2 ** smoothing) * (sections.length * (subdivisions + 1)) * 6, path);
      break;
    }
    case 'lowpoly/profile-cage': {
      const sections = node.profileCageSections;
      if (!Array.isArray(sections) || sections.length < 2) throw new Error(`${path}.profileCageSections: at least two sections are required.`);
      let previousZ = Number.NEGATIVE_INFINITY;
      let pointCount;
      let winding;
      sections.forEach((section, index) => {
        const sectionPath = `${path}.profileCageSections[${index}]`;
        if (!section || !Number.isFinite(section.z) || section.z <= previousZ) {
          throw new Error(`${sectionPath}.z: section depths must be finite and strictly increasing.`);
        }
        previousZ = section.z;
        const profile = validateProfile(section.profile, 3, `${sectionPath}.profile`, true);
        if (pointCount !== undefined && profile.length !== pointCount) {
          throw new Error(`${sectionPath}.profile: all sections must have matching point counts.`);
        }
        const sectionWinding = Math.sign(signedArea(profile));
        if (winding !== undefined && sectionWinding !== winding) {
          throw new Error(`${sectionPath}.profile: all sections must use the same point winding.`);
        }
        winding = sectionWinding;
        pointCount = profile.length;
      });
      const subdivisions = integerParameter(node, 'cageSubdivisions', 0, 0, 8, `${path}.parameters.cageSubdivisions`);
      const smoothing = integerParameter(node, 'profileSmoothing', 0, 0, 4, `${path}.parameters.profileSmoothing`);
      validateSmoothNormals(node, path);
      assertVertexBudget(pointCount * (2 ** smoothing) * (sections.length * (subdivisions + 1)) * 6, path);
      break;
    }
    case 'lowpoly/lathe-profile': {
      const profile = validateProfile(node.profile, 2, `${path}.profile`, false);
      let previousHeight = Number.NEGATIVE_INFINITY;
      profile.forEach((point, index) => {
        if (point.x < 0 || point.y <= previousHeight) {
          throw new Error(`${path}.profile[${index}]: lathe radii must be non-negative and heights strictly increasing.`);
        }
        previousHeight = point.y;
      });
      const segments = integerParameter(node, 'radialSegments', 12, 3, 64, `${path}.parameters.radialSegments`);
      const smoothing = integerParameter(node, 'profileSmoothing', 0, 0, 4, `${path}.parameters.profileSmoothing`);
      validateSmoothNormals(node, path);
      assertVertexBudget(profile.length * (2 ** smoothing) * segments * 6, path);
      break;
    }
    case 'lowpoly/profile-sweep': {
      const profile = validateProfile(node.profile, 3, `${path}.profile`, true);
      const pathPoints = node.path;
      if (!Array.isArray(pathPoints) || pathPoints.length < 2) throw new Error(`${path}.path: at least two path points are required.`);
      pathPoints.forEach((point, index) => validateVector3(point, `${path}.path[${index}]`));
      for (let index = 1; index < pathPoints.length; index++) {
        if (sameVector3(pathPoints[index - 1], pathPoints[index])) {
          throw new Error(`${path}.path[${index}]: consecutive path points cannot repeat.`);
        }
      }
      const profileSmoothing = integerParameter(node, 'profileSmoothing', 0, 0, 4, `${path}.parameters.profileSmoothing`);
      const pathSmoothing = integerParameter(node, 'pathSmoothing', 0, 0, 4, `${path}.parameters.pathSmoothing`);
      validateSmoothNormals(node, path);
      assertVertexBudget(profile.length * (2 ** profileSmoothing) * pathPoints.length * (2 ** pathSmoothing) * 6, path);
      break;
    }
  }
}

export function createLowPolyGeometry(node) {
  switch (node.type) {
    case 'lowpoly/cube':
      return new BoxGeometry(1, 1, 1);
    case 'lowpoly/sphere':
      return new SphereGeometry(0.5, 12, 8);
    case 'lowpoly/cylinder':
      return new CylinderGeometry(0.5, 0.5, 2, 12);
    case 'lowpoly/capsule':
      return new CapsuleGeometry(0.5, 1, 4, 8);
    case 'lowpoly/wedge':
      return createWedgeGeometry();
    case 'lowpoly/frustum':
      return createFrustumGeometry(node);
    case 'lowpoly/extruded-profile':
      return createExtrudedProfile(node);
    case 'lowpoly/profile-loft':
      return createProfileLoft(node);
    case 'lowpoly/profile-cage':
      return createProfileCage(node);
    case 'lowpoly/lathe-profile':
      return createLatheProfile(node);
    case 'lowpoly/profile-sweep':
      return createProfileSweep(node);
    default:
      throw new Error(`Unsupported shape type '${node.type}'.`);
  }
}

function createWedgeGeometry() {
  const bfl = [-0.5, -0.5, -0.5], bfr = [0.5, -0.5, -0.5];
  const bbr = [0.5, -0.5, 0.5], bbl = [-0.5, -0.5, 0.5];
  const tbr = [0.5, 0.5, 0.5], tbl = [-0.5, 0.5, 0.5];
  return geometryFromFaces([
    [bfl, bfr, bbr, bbl], [bfr, bfl, tbl, tbr], [bbl, bbr, tbr, tbl],
    [bfl, bbl, tbl], [bbr, bfr, tbr],
  ]);
}

function createFrustumGeometry(node) {
  const topWidth = positiveParameter(node, 'topWidth', 0.65);
  const topDepth = positiveParameter(node, 'topDepth', 0.65);
  const bottomWidth = positiveParameter(node, 'bottomWidth', 1);
  const bottomDepth = positiveParameter(node, 'bottomDepth', 1);
  const corner = (width, depth, x, z, y) => [width * x * 0.5, y, depth * z * 0.5];
  const bfl = corner(bottomWidth, bottomDepth, -1, -1, -0.5);
  const bfr = corner(bottomWidth, bottomDepth, 1, -1, -0.5);
  const bbr = corner(bottomWidth, bottomDepth, 1, 1, -0.5);
  const bbl = corner(bottomWidth, bottomDepth, -1, 1, -0.5);
  const tfl = corner(topWidth, topDepth, -1, -1, 0.5);
  const tfr = corner(topWidth, topDepth, 1, -1, 0.5);
  const tbr = corner(topWidth, topDepth, 1, 1, 0.5);
  const tbl = corner(topWidth, topDepth, -1, 1, 0.5);
  return geometryFromFaces([
    [bfl, bfr, bbr, bbl], [tbl, tbr, tfr, tfl], [bfr, bfl, tfl, tfr],
    [bbl, bbr, tbr, tbl], [bfl, bbl, tbl, tfl], [bbr, bfr, tfr, tbr],
  ]);
}

function createExtrudedProfile(node) {
  const profile = smoothClosed(ensureCounterClockwise(node.profile), integerParameter(node, 'profileSmoothing', 0, 0, 4));
  const shape = createShape(profile);
  const depth = positiveParameter(node, 'profileDepth', 0.2);
  const bevel = nonNegativeParameter(node, 'profileBevel', 0);
  const bevelSegments = integerParameter(node, 'profileBevelSegments', 1, 1, 8);
  const geometry = new ExtrudeGeometry(shape, {
    depth: depth - (bevel * 2),
    steps: 1,
    bevelEnabled: bevel > 0,
    bevelThickness: bevel,
    bevelSize: bevel,
    bevelSegments,
    curveSegments: 1,
  });
  geometry.translate(0, 0, -depth * 0.5);
  return geometry;
}

function createProfileLoft(node) {
  const profile = smoothClosed(ensureCounterClockwise(node.profile), integerParameter(node, 'profileSmoothing', 0, 0, 4));
  const sections = node.profileSections.map(section => ({
    z: section.z,
    profile: profile.map(point => new Vector2(
      (point.x * section.scale.x) + section.offset.x,
      (point.y * section.scale.y) + section.offset.y,
    )),
  }));
  return createRingGeometry(interpolateRings(sections, integerParameter(node, 'loftSubdivisions', 0, 0, 8)),
    profile, node.parameters?.smoothNormals > 0);
}

function createProfileCage(node) {
  const smoothing = integerParameter(node, 'profileSmoothing', 0, 0, 4);
  const sections = node.profileCageSections.map(section => ({
    z: section.z,
    profile: smoothClosed(ensureCounterClockwise(section.profile), smoothing).map(point => new Vector2(point.x, point.y)),
  }));
  const subdivisions = integerParameter(node, 'cageSubdivisions', 0, 0, 8);
  return createRingGeometry(interpolateRings(sections, subdivisions), sections[0].profile,
    node.parameters?.smoothNormals > 0);
}

function createLatheProfile(node) {
  const profile = smoothOpen(node.profile, integerParameter(node, 'profileSmoothing', 0, 0, 4))
    .map(point => new Vector2(point.x, point.y));
  const segments = integerParameter(node, 'radialSegments', 12, 3, 64);
  const geometry = new LatheGeometry(profile, segments);
  if (node.parameters?.smoothNormals > 0) return geometry;
  const flat = geometry.toNonIndexed();
  geometry.dispose();
  flat.computeVertexNormals();
  return flat;
}

function createProfileSweep(node) {
  const profile = smoothClosed(ensureCounterClockwise(node.profile), integerParameter(node, 'profileSmoothing', 0, 0, 4));
  const pathPoints = smoothOpen(node.path, integerParameter(node, 'pathSmoothing', 0, 0, 4))
    .map(point => new Vector3(point.x, point.y, point.z));
  const path = new CurvePath();
  for (let index = 0; index < pathPoints.length - 1; index++) {
    path.add(new LineCurve3(pathPoints[index], pathPoints[index + 1]));
  }
  const shape = createShape(profile);
  return new ExtrudeGeometry(shape, {
    steps: pathPoints.length - 1,
    bevelEnabled: false,
    extrudePath: path,
  });
}

function createRingGeometry(sections, baseProfile, smoothNormals) {
  const rings = sections.map(section => section.profile.map(point => [point.x, point.y, section.z]));
  const pointCount = rings[0].length;
  const positions = [];
  const indices = [];
  for (const ring of rings) for (const point of ring) positions.push(...point);
  for (let ringIndex = 0; ringIndex < rings.length - 1; ringIndex++) {
    const currentStart = ringIndex * pointCount;
    const nextStart = (ringIndex + 1) * pointCount;
    for (let pointIndex = 0; pointIndex < pointCount; pointIndex++) {
      const nextPoint = (pointIndex + 1) % pointCount;
      const a = currentStart + pointIndex, b = currentStart + nextPoint;
      const c = nextStart + nextPoint, d = nextStart + pointIndex;
      indices.push(a, b, c, a, c, d);
    }
  }
  const contour = ensureCounterClockwise(baseProfile).map(point => new Vector2(point.x, point.y));
  const caps = ShapeUtils.triangulateShape(contour, []);
  const firstCapStart = positions.length / 3;
  for (const point of rings[0]) positions.push(...point);
  const lastCapStart = positions.length / 3;
  for (const point of rings.at(-1)) positions.push(...point);
  for (const face of caps) {
    indices.push(firstCapStart + face[2], firstCapStart + face[1], firstCapStart + face[0]);
    indices.push(lastCapStart + face[0], lastCapStart + face[1], lastCapStart + face[2]);
  }
  if (!smoothNormals) return geometryFromIndexedFaces(positions, indices);
  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new Float32BufferAttribute(positions, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals();
  return geometry;
}

function geometryFromIndexedFaces(positions, indices) {
  const triangles = [];
  for (let index = 0; index < indices.length; index++) {
    const pointIndex = indices[index] * 3;
    triangles.push(positions[pointIndex], positions[pointIndex + 1], positions[pointIndex + 2]);
  }
  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new Float32BufferAttribute(triangles, 3));
  geometry.computeVertexNormals();
  return geometry;
}

function geometryFromFaces(faces) {
  const positions = [];
  for (const face of faces) {
    for (let index = 1; index < face.length - 1; index++) {
      positions.push(...face[0], ...face[index], ...face[index + 1]);
    }
  }
  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new Float32BufferAttribute(positions, 3));
  geometry.computeVertexNormals();
  return geometry;
}

function createShape(profile) {
  const points = ensureCounterClockwise(profile);
  const shape = new Shape();
  shape.moveTo(points[0].x, points[0].y);
  for (let index = 1; index < points.length; index++) shape.lineTo(points[index].x, points[index].y);
  shape.closePath();
  return shape;
}

function interpolateRings(sections, subdivisions) {
  if (subdivisions === 0) return sections;
  const result = [];
  const stride = subdivisions + 1;
  for (let index = 0; index < sections.length - 1; index++) {
    const first = sections[index], second = sections[index + 1];
    result.push(first);
    for (let step = 1; step < stride; step++) {
      const t = step / stride;
      const eased = t * t * (3 - (2 * t));
      result.push({
        z: lerp(first.z, second.z, eased),
        profile: first.profile.map((point, pointIndex) => new Vector2(
          lerp(point.x, second.profile[pointIndex].x, eased),
          lerp(point.y, second.profile[pointIndex].y, eased),
        )),
      });
    }
  }
  result.push(sections.at(-1));
  return result;
}

function smoothClosed(points, iterations) {
  let result = points.map(point => ({ x: point.x, y: point.y }));
  for (let iteration = 0; iteration < iterations; iteration++) {
    const next = [];
    for (let index = 0; index < result.length; index++) {
      const current = result[index], following = result[(index + 1) % result.length];
      next.push(lerpPoint(current, following, 0.25), lerpPoint(current, following, 0.75));
    }
    result = next;
  }
  return result;
}

function smoothOpen(points, iterations) {
  let result = points.map(point => ({ ...point }));
  for (let iteration = 0; iteration < iterations; iteration++) {
    const next = [result[0]];
    for (let index = 0; index < result.length - 1; index++) {
      next.push(lerpPoint(result[index], result[index + 1], 0.25));
      next.push(lerpPoint(result[index], result[index + 1], 0.75));
    }
    next.push(result.at(-1));
    result = next;
  }
  return result;
}

function validateProfile(profile, minimumCount, path, closed) {
  if (!Array.isArray(profile) || profile.length < minimumCount) {
    throw new Error(`${path}: expected at least ${minimumCount} points.`);
  }
  if (profile.length > 2048) throw new Error(`${path}: profile cannot exceed 2,048 points.`);
  profile.forEach((point, index) => validateVector2(point, `${path}[${index}]`));
  if (closed) {
    const area = signedArea(profile);
    if (Math.abs(area) < 1e-8) throw new Error(`${path}: closed profile must enclose a non-zero area.`);
    const contour = profile.map(point => new Vector2(point.x, point.y));
    if (ShapeUtils.triangulateShape(contour, []).length !== profile.length - 2) {
      throw new Error(`${path}: profile must be a simple polygon.`);
    }
  }
  return profile;
}

function validateVector2(point, path) {
  if (!point || !Number.isFinite(point.x) || !Number.isFinite(point.y)) {
    throw new Error(`${path}: expected finite x and y values.`);
  }
}

function validateVector3(point, path) {
  if (!point || ![point.x, point.y, point.z].every(Number.isFinite)) {
    throw new Error(`${path}: expected finite x, y, and z values.`);
  }
}

function validatePositiveVector2(point, path) {
  validateVector2(point, path);
  if (point.x <= 0 || point.y <= 0) throw new Error(`${path}: both components must be positive.`);
}

function integerParameter(node, key, fallback, minimum, maximum, path = `Node '${node.id}' parameter '${key}'`) {
  const value = node.parameters?.[key] ?? fallback;
  if (!Number.isInteger(value) || value < minimum || value > maximum) {
    throw new Error(`${path} must be an integer from ${minimum} through ${maximum}.`);
  }
  return value;
}

function positiveParameter(node, key, fallback, path = `Node '${node.id}' parameter '${key}'`) {
  const value = node.parameters?.[key] ?? fallback;
  if (!Number.isFinite(value) || value <= 0) throw new Error(`${path} must be finite and positive.`);
  return value;
}

function nonNegativeParameter(node, key, fallback, path = `Node '${node.id}' parameter '${key}'`) {
  const value = node.parameters?.[key] ?? fallback;
  if (!Number.isFinite(value) || value < 0) throw new Error(`${path} must be finite and non-negative.`);
  return value;
}

function assertVertexBudget(vertices, path) {
  if (vertices > 60000) throw new Error(`${path}: generated geometry exceeds the 60,000 vertex budget.`);
}

function validateSmoothNormals(node, path) {
  nonNegativeParameter(node, 'smoothNormals', 0, `${path}.parameters.smoothNormals`);
}

function ensureCounterClockwise(profile) {
  return signedArea(profile) >= 0 ? profile : [...profile].reverse();
}

function signedArea(profile) {
  let area = 0;
  for (let index = 0; index < profile.length; index++) {
    const first = profile[index], second = profile[(index + 1) % profile.length];
    area += first.x * second.y - second.x * first.y;
  }
  return area * 0.5;
}

function lerpPoint(first, second, t) {
  const result = { x: lerp(first.x, second.x, t), y: lerp(first.y, second.y, t) };
  if ('z' in first) result.z = lerp(first.z, second.z, t);
  return result;
}

function lerp(first, second, t) { return first + ((second - first) * t); }

function sameVector3(first, second) {
  return first.x === second.x && first.y === second.y && first.z === second.z;
}

