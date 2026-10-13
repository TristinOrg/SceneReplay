using NUnit.Framework;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies the first official Low Poly shape implementation.
    /// </summary>
    public sealed class LowPolyPrimitiveGeneratorTests
    {
        private GameObject generatedRoot;

        [TearDown]
        public void TearDown()
        {
            if (generatedRoot != null)
                UnityEngine.Object.DestroyImmediate(generatedRoot);
        }

        [Test]
        public void GenerateCreatesRenderableCubeWithoutPhysicsDependency()
        {
            ShapeNode cube = new("cube", "Cube", LowPolyShapeTypes.Cube);
            cube.Transform.Scale                = new(2f, 3f, 4f);
            cube.Appearance.HasColorOverride    = true;
            cube.Appearance.Color               = new(1f, 0f, 0f);

            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Cube", cube));

            Assert.That(generatedRoot.GetComponent<MeshFilter>(), Is.Not.Null);
            Assert.That(generatedRoot.GetComponent<MeshRenderer>(), Is.Not.Null);
            Assert.That(generatedRoot.GetComponent<Collider>(), Is.Null);
            Assert.That(generatedRoot.transform.localScale, Is.EqualTo(new Vector3(2f, 3f, 4f)));

            UnityShapeAppearanceManifest manifest = generatedRoot.GetComponent<UnityShapeAppearanceManifest>();
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.BindingCount, Is.EqualTo(1));

            MaterialPropertyBlock properties = new();
            Renderer renderer = generatedRoot.GetComponent<Renderer>();
            renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetColor(Shader.PropertyToID("_Color")), Is.EqualTo(Color.red));

            renderer.SetPropertyBlock(null);
            manifest.Apply();
            renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetColor(Shader.PropertyToID("_Color")), Is.EqualTo(Color.red));
        }

        [Test]
        public void GenerateSupportsCachedBuiltInPrimitiveMeshes()
        {
            ShapeNode root = new("primitives", "Primitives", ShapeTypes.Group);
            root
                .Add(new("sphere-a", "Sphere A", LowPolyShapeTypes.Sphere))
                .Add(new("sphere-b", "Sphere B", LowPolyShapeTypes.Sphere))
                .Add(new("cylinder", "Cylinder", LowPolyShapeTypes.Cylinder))
                .Add(new("capsule", "Capsule", LowPolyShapeTypes.Capsule));

            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Primitives", root));

            MeshFilter firstSphere  = generatedRoot.transform.Find("Sphere A").GetComponent<MeshFilter>();
            MeshFilter secondSphere = generatedRoot.transform.Find("Sphere B").GetComponent<MeshFilter>();

            Assert.That(generatedRoot.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(4));
            Assert.That(generatedRoot.GetComponentsInChildren<Collider>().Length, Is.Zero);
            Assert.That(firstSphere.sharedMesh, Is.SameAs(secondSphere.sharedMesh));
        }

        [Test]
        public void GenerateSupportsCachedParameterizedMeshes()
        {
            ShapeNode root          = new("procedural", "Procedural Shapes", ShapeTypes.Group);
            ShapeNode wedge         = new("wedge", "Wedge", LowPolyShapeTypes.Wedge);
            ShapeNode firstFrustum  = CreateFrustum("frustum-a", "Frustum A", 0.45f);
            ShapeNode secondFrustum = CreateFrustum("frustum-b", "Frustum B", 0.45f);
            ShapeNode wideFrustum   = CreateFrustum("frustum-c", "Frustum C", 0.8f);
            root.Add(wedge).Add(firstFrustum).Add(secondFrustum).Add(wideFrustum);

            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Procedural Shapes", root));

            Mesh wedgeMesh     = generatedRoot.transform.Find("Wedge").GetComponent<MeshFilter>().sharedMesh;
            Mesh firstMesh     = generatedRoot.transform.Find("Frustum A").GetComponent<MeshFilter>().sharedMesh;
            Mesh secondMesh    = generatedRoot.transform.Find("Frustum B").GetComponent<MeshFilter>().sharedMesh;
            Mesh differentMesh = generatedRoot.transform.Find("Frustum C").GetComponent<MeshFilter>().sharedMesh;

            Assert.That(wedgeMesh.vertexCount, Is.EqualTo(18));
            Assert.That(wedgeMesh.hideFlags, Is.EqualTo(HideFlags.None));
            Assert.That(wedgeMesh.normals[0].y, Is.LessThan(-0.99f));
            Assert.That(firstMesh.vertexCount, Is.EqualTo(24));
            Assert.That(firstMesh.normals[0].y, Is.LessThan(-0.99f));
            Assert.That(firstMesh, Is.SameAs(secondMesh));
            Assert.That(firstMesh, Is.Not.SameAs(differentMesh));
        }

        [Test]
        public void GenerateTriangulatesAndCachesConcaveExtrudedProfiles()
        {
            ForgeVector2[] points =
            {
                new(-0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, -0.5f),
                new(0f, -0.15f), new(-0.5f, -0.5f)
            };
            ShapeNode first  = CreateProfile("profile-a", "Profile A", points);
            ShapeNode second = CreateProfile("profile-b", "Profile B", points);
            ShapeNode root   = new("profiles", "Profiles", ShapeTypes.Group);
            root.Add(first).Add(second);

            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Profiles", root));

            Mesh firstMesh  = generatedRoot.transform.Find("Profile A").GetComponent<MeshFilter>().sharedMesh;
            Mesh secondMesh = generatedRoot.transform.Find("Profile B").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(firstMesh.name, Is.EqualTo("Low Poly Extruded Profile"));
            Assert.That(firstMesh.vertexCount, Is.EqualTo(158));
            Assert.That(firstMesh.bounds.size.z, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(firstMesh, Is.SameAs(secondMesh));
        }

        [Test]
        public void GenerateRebuildsDestroyedProceduralCacheEntries()
        {
            ForgeVector2[] points =
            {
                new(-0.5f, -0.5f), new(0.5f, -0.5f), new(0.5f, 0.5f), new(-0.5f, 0.5f)
            };
            ShapeNode first  = CreateProfile("profile-a", "Profile A", points);
            ShapeNode second = CreateProfile("profile-b", "Profile B", points);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Profile A", first));
            Mesh destroyed = generatedRoot.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(destroyed);
            Object.DestroyImmediate(generatedRoot);

            generatedRoot = generator.Generate(new("Profile B", second));

            Mesh rebuilt = generatedRoot.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(rebuilt, Is.Not.Null);
            Assert.That(rebuilt, Is.Not.SameAs(destroyed));
        }

        [Test]
        public void GenerateBuildsAndCachesConcaveProfileLofts()
        {
            ForgeVector2[] points =
            {
                new(-0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, -0.5f),
                new(0f, -0.15f), new(-0.5f, -0.5f)
            };
            ShapeNode first  = CreateLoft("loft-a", "Loft A", points);
            ShapeNode second = CreateLoft("loft-b", "Loft B", points);
            ShapeNode root   = new("lofts", "Lofts", ShapeTypes.Group);
            root.Add(first).Add(second);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Lofts", root));

            Mesh firstMesh  = generatedRoot.transform.Find("Loft A").GetComponent<MeshFilter>().sharedMesh;
            Mesh secondMesh = generatedRoot.transform.Find("Loft B").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(firstMesh.name, Is.EqualTo("Low Poly Profile Loft"));
            Assert.That(firstMesh.vertexCount, Is.EqualTo(138));
            Assert.That(firstMesh.bounds.size.z, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(firstMesh, Is.SameAs(secondMesh));
            AssertDuplicateVerticesShareNormals(firstMesh);
        }

        [Test]
        public void GenerateBuildsAndCachesSmoothLatheProfiles()
        {
            ShapeNode first     = CreateLathe("lathe-a", "Lathe A", 12, true);
            ShapeNode second    = CreateLathe("lathe-b", "Lathe B", 12, true);
            ShapeNode lowDetail = CreateLathe("lathe-c", "Lathe C", 8, false);
            ShapeNode root      = new("lathes", "Lathes", ShapeTypes.Group);
            root.Add(first).Add(second).Add(lowDetail);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Lathes", root));

            Mesh firstMesh  = generatedRoot.transform.Find("Lathe A").GetComponent<MeshFilter>().sharedMesh;
            Mesh secondMesh = generatedRoot.transform.Find("Lathe B").GetComponent<MeshFilter>().sharedMesh;
            Mesh detailMesh = generatedRoot.transform.Find("Lathe C").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(firstMesh.name, Is.EqualTo("Low Poly Lathe Profile"));
            Assert.That(firstMesh.vertexCount, Is.EqualTo(216));
            Assert.That(firstMesh.bounds.size.y, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(firstMesh, Is.SameAs(secondMesh));
            Assert.That(firstMesh, Is.Not.SameAs(detailMesh));
            AssertDuplicateVerticesShareNormals(firstMesh);
        }

        [Test]
        public void GenerateBuildsAndCachesIndependentProfileCages()
        {
            ShapeNode first  = CreateCage("cage-a", "Cage A");
            ShapeNode second = CreateCage("cage-b", "Cage B");
            ShapeNode coarse = CreateCage("cage-c", "Cage C");
            coarse.Parameters[LowPolyShapeParameters.CageSubdivisions] = 0f;
            ShapeNode root   = new("cages", "Cages", ShapeTypes.Group);
            root.Add(first).Add(second).Add(coarse);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Cages", root));

            Mesh firstMesh  = generatedRoot.transform.Find("Cage A").GetComponent<MeshFilter>().sharedMesh;
            Mesh secondMesh = generatedRoot.transform.Find("Cage B").GetComponent<MeshFilter>().sharedMesh;
            Mesh coarseMesh = generatedRoot.transform.Find("Cage C").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(firstMesh.name, Is.EqualTo("Low Poly Profile Cage"));
            Assert.That(firstMesh.bounds.size.z, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(firstMesh.bounds.max.x, Is.GreaterThan(0.55f));
            Assert.That(firstMesh, Is.SameAs(secondMesh));
            Assert.That(firstMesh, Is.Not.SameAs(coarseMesh));
            Assert.That(firstMesh.vertexCount, Is.GreaterThan(coarseMesh.vertexCount));
            AssertDuplicateVerticesShareNormals(firstMesh);
        }

        [Test]
        public void GenerateRejectsProfileCagesWithMismatchedPointCounts()
        {
            ShapeNode cage = CreateCage("invalid-cage", "Invalid Cage");
            cage.ProfileCageSections[1].Profile.RemoveAt(0);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            Assert.Throws<System.ArgumentException>(() => generator.Generate(new("Invalid", cage)));
        }

        [Test]
        public void GenerateSmoothsProfileControlPointsBeforeCachingMeshes()
        {
            ShapeNode first  = CreateLathe("smooth-a", "Smooth A", 12, true);
            ShapeNode second = CreateLathe("smooth-b", "Smooth B", 12, true);
            ShapeNode sharp  = CreateLathe("sharp", "Sharp", 12, true);
            first.Parameters[LowPolyShapeParameters.ProfileSmoothing]  = 1f;
            second.Parameters[LowPolyShapeParameters.ProfileSmoothing] = 1f;
            ShapeNode root = new("smoothed", "Smoothed", ShapeTypes.Group);
            root.Add(first).Add(second).Add(sharp);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Smoothed", root));

            Mesh firstMesh  = generatedRoot.transform.Find("Smooth A").GetComponent<MeshFilter>().sharedMesh;
            Mesh secondMesh = generatedRoot.transform.Find("Smooth B").GetComponent<MeshFilter>().sharedMesh;
            Mesh sharpMesh  = generatedRoot.transform.Find("Sharp").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(firstMesh.vertexCount, Is.EqualTo(408));
            Assert.That(firstMesh.bounds.min.y, Is.EqualTo(-0.5f).Within(0.0001f));
            Assert.That(firstMesh.bounds.max.y, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(firstMesh, Is.SameAs(secondMesh));
            Assert.That(firstMesh, Is.Not.SameAs(sharpMesh));
        }

        [Test]
        public void GenerateBuildsAndCachesSmoothedProfileSweeps()
        {
            ShapeNode first  = CreateSweep("sweep-a", "Sweep A");
            ShapeNode second = CreateSweep("sweep-b", "Sweep B");
            ShapeNode root   = new("sweeps", "Sweeps", ShapeTypes.Group);
            root.Add(first).Add(second);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new LowPolyPrimitiveGenerator()
            });

            generatedRoot = generator.Generate(new("Sweeps", root));

            Mesh firstMesh  = generatedRoot.transform.Find("Sweep A").GetComponent<MeshFilter>().sharedMesh;
            Mesh secondMesh = generatedRoot.transform.Find("Sweep B").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(firstMesh.name, Is.EqualTo("Low Poly Profile Sweep"));
            Assert.That(firstMesh.vertexCount, Is.EqualTo(260));
            Assert.That(firstMesh.bounds.size.y, Is.GreaterThan(1.1f));
            Assert.That(firstMesh, Is.SameAs(secondMesh));
            AssertDuplicateVerticesShareNormals(firstMesh);
        }

        private static ShapeNode CreateFrustum(string id, string name, float topWidth)
        {
            ShapeNode node = new(id, name, LowPolyShapeTypes.Frustum);
            node.Parameters[LowPolyShapeParameters.TopWidth]    = topWidth;
            node.Parameters[LowPolyShapeParameters.TopDepth]    = 0.6f;
            node.Parameters[LowPolyShapeParameters.BottomWidth] = 1f;
            node.Parameters[LowPolyShapeParameters.BottomDepth] = 0.9f;
            return node;
        }

        private static ShapeNode CreateProfile(string id, string name, ForgeVector2[] points)
        {
            ShapeNode node = new(id, name, LowPolyShapeTypes.ExtrudedProfile);
            node.Parameters[LowPolyShapeParameters.ProfileDepth]         = 0.2f;
            node.Parameters[LowPolyShapeParameters.ProfileBevel]         = 0.03f;
            node.Parameters[LowPolyShapeParameters.ProfileBevelSegments] = 3f;
            foreach (ForgeVector2 point in points)
                node.Profile.Add(point);

            return node;
        }

        private static ShapeNode CreateLoft(string id, string name, ForgeVector2[] points)
        {
            ShapeNode node = new(id, name, LowPolyShapeTypes.ProfileLoft);
            node.Parameters[LowPolyShapeParameters.LoftSubdivisions] = 2f;
            node.Parameters[LowPolyShapeParameters.SmoothNormals]    = 1f;
            foreach (ForgeVector2 point in points)
                node.Profile.Add(point);

            node.ProfileSections.Add(new(-0.5f, new(0.75f, 0.8f), ForgeVector2.Zero));
            node.ProfileSections.Add(new(0f, ForgeVector2.One, new(0f, 0.05f)));
            node.ProfileSections.Add(new(0.5f, new(0.8f, 0.9f), ForgeVector2.Zero));
            return node;
        }

        private static ShapeNode CreateLathe(string id, string name, int radialSegments, bool smoothNormals)
        {
            ShapeNode node = new(id, name, LowPolyShapeTypes.LatheProfile);
            node.Profile.Add(new(0.28f, -0.5f));
            node.Profile.Add(new(0.5f, -0.18f));
            node.Profile.Add(new(0.42f, 0.28f));
            node.Profile.Add(new(0.24f, 0.5f));
            node.Parameters[LowPolyShapeParameters.RadialSegments] = radialSegments;
            node.Parameters[LowPolyShapeParameters.SmoothNormals]  = smoothNormals ? 1f : 0f;
            return node;
        }

        private static ShapeNode CreateCage(string id, string name)
        {
            ShapeNode node = new(id, name, LowPolyShapeTypes.ProfileCage);
            node.ProfileCageSections.Add(new(-0.5f, new ForgeVector2[]
            {
                new(-0.4f, -0.5f), new(0.4f, -0.5f), new(0.5f, 0.5f), new(-0.5f, 0.5f)
            }));
            node.ProfileCageSections.Add(new(0f, new ForgeVector2[]
            {
                new(-0.55f, -0.45f), new(0.45f, -0.55f), new(0.62f, 0.4f), new(-0.48f, 0.58f)
            }));
            node.ProfileCageSections.Add(new(0.5f, new ForgeVector2[]
            {
                new(-0.3f, -0.4f), new(0.5f, -0.5f), new(0.35f, 0.6f), new(-0.45f, 0.4f)
            }));
            node.Parameters[LowPolyShapeParameters.ProfileSmoothing] = 1f;
            node.Parameters[LowPolyShapeParameters.CageSubdivisions] = 2f;
            node.Parameters[LowPolyShapeParameters.SmoothNormals]    = 1f;
            return node;
        }

        private static ShapeNode CreateSweep(string id, string name)
        {
            ShapeNode node = new(id, name, LowPolyShapeTypes.ProfileSweep);
            node.Profile.Add(new(-0.12f, -0.08f));
            node.Profile.Add(new(0.12f, -0.08f));
            node.Profile.Add(new(0.12f, 0.08f));
            node.Profile.Add(new(-0.12f, 0.08f));
            node.Path.Add(new(0f, 0f, 0f));
            node.Path.Add(new(0f, 0.4f, 0.3f));
            node.Path.Add(new(0.2f, 0.8f, 0.6f));
            node.Path.Add(new(0f, 1.2f, 1f));
            node.Parameters[LowPolyShapeParameters.ProfileSmoothing] = 1f;
            node.Parameters[LowPolyShapeParameters.PathSmoothing]    = 1f;
            node.Parameters[LowPolyShapeParameters.SmoothNormals]    = 1f;
            return node;
        }

        private static void AssertDuplicateVerticesShareNormals(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals  = mesh.normals;
            bool      compared = false;
            for (int first = 0; first < vertices.Length; first++)
            {
                for (int second = first + 1; second < vertices.Length; second++)
                {
                    if (vertices[first] != vertices[second])
                        continue;

                    Assert.That(Vector3.Distance(normals[first], normals[second]), Is.LessThan(0.0001f));
                    compared = true;
                }
            }

            Assert.That(compared, Is.True);
        }
    }
}
