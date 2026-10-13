using System;
using System.Collections.Generic;
using System.Diagnostics;
using SceneReplay.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace SceneReplay.LowPoly.Editor
{
    /// <summary>
    /// Measures the complete external-JSON Low Poly generation path in the Unity Editor.
    /// </summary>
    internal static class LowPolyGenerationBenchmark
    {
        private const int DefaultModelCount = 200;

        [MenuItem("SceneReplay/Diagnostics/Benchmark JSON Generation", false, 100)]
        private static void RunFromMenu()
        {
            LowPolyGenerationBenchmarkReport report = Run(DefaultModelCount);
            Debug.Log(report.ToString());
        }

        public static LowPolyGenerationBenchmarkReport Run(int modelCount)
        {
            if (modelCount < 2)
                throw new ArgumentOutOfRangeException(nameof(modelCount));

            ShapeJsonSerializer  serializer = new();
            LowPolyModelGenerator generator  = new();
            string                tableJson  = serializer.Serialize(LowPolyWorkbenchPreset.CreateDefinition());
            string                robotJson  = serializer.Serialize(LowPolyRobotPreset.CreateDefinition());

            generator.SetStyle(LowPolyWorkbenchPreset.CreateStyle());
            generator.SetStyle(LowPolyRobotPreset.CreateStyle());
            WarmUp(generator, tableJson);

            double parsingMilliseconds = MeasureParsing(serializer, tableJson, robotJson, modelCount);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            GameObject      container         = new("SceneReplay Benchmark");
            ShapeDefinition tableDefinition   = serializer.DeserializeShape(tableJson);
            ShapeDefinition robotDefinition   = serializer.DeserializeShape(robotJson);
            double validationMilliseconds = MeasureValidation(
                tableDefinition,
                robotDefinition,
                modelCount);
            int             tableCount        = (modelCount + 1) / 2;
            int             robotCount        = modelCount / 2;
            LowPolyGenerationBatch tableBatch = generator.CreateBatch(
                tableDefinition,
                tableCount,
                container.transform);
            LowPolyGenerationBatch robotBatch = generator.CreateBatch(
                robotDefinition,
                robotCount,
                container.transform);
            Stopwatch       stopwatch         = Stopwatch.StartNew();
            long            managedHeapBefore = Profiler.GetMonoUsedSizeLong();

            try
            {
                tableBatch.GenerateNext(tableCount);
                robotBatch.GenerateNext(robotCount);

                long managedHeapGrowthBytes = Profiler.GetMonoUsedSizeLong() - managedHeapBefore;
                stopwatch.Stop();
                return CreateReport(
                    container,
                    modelCount,
                    parsingMilliseconds,
                    validationMilliseconds,
                    stopwatch.Elapsed.TotalMilliseconds,
                    managedHeapGrowthBytes);
            }
            finally
            {
                Object.DestroyImmediate(container);
            }
        }

        private static double MeasureParsing(
            ShapeJsonSerializer serializer,
            string              tableJson,
            string              robotJson,
            int                 modelCount)
        {
            Stopwatch       stopwatch      = Stopwatch.StartNew();
            ShapeDefinition lastDefinition = null;

            for (int index = 0; index < modelCount; index++)
            {
                string json = (index & 1) == 0 ? tableJson : robotJson;
                lastDefinition = serializer.DeserializeShape(json);
            }

            stopwatch.Stop();
            GC.KeepAlive(lastDefinition);
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private static double MeasureValidation(
            ShapeDefinition tableDefinition,
            ShapeDefinition robotDefinition,
            int             modelCount)
        {
            ShapeDefinitionValidator validator = new();
            Stopwatch                stopwatch = Stopwatch.StartNew();

            for (int index = 0; index < modelCount; index++)
            {
                ShapeDefinition definition = (index & 1) == 0 ? tableDefinition : robotDefinition;
                validator.Validate(definition);
            }

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private static void WarmUp(LowPolyModelGenerator generator, string json)
        {
            GameObject warmUp = generator.GenerateJson(json);
            Object.DestroyImmediate(warmUp);
        }

        private static LowPolyGenerationBenchmarkReport CreateReport(
            GameObject container,
            int        modelCount,
            double     parsingMilliseconds,
            double     validationMilliseconds,
            double     generationMilliseconds,
            long       managedHeapGrowthBytes)
        {
            MeshRenderer[]    renderers       = container.GetComponentsInChildren<MeshRenderer>();
            HashSet<Mesh>     uniqueMeshes    = new();
            HashSet<Material> uniqueMaterials = new();

            foreach (MeshRenderer renderer in renderers)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                uniqueMeshes.Add(meshFilter.sharedMesh);
                uniqueMaterials.Add(renderer.sharedMaterial);
            }

            return new(
                modelCount,
                renderers.Length,
                uniqueMeshes.Count,
                uniqueMaterials.Count,
                parsingMilliseconds,
                validationMilliseconds,
                generationMilliseconds,
                managedHeapGrowthBytes);
        }
    }

    /// <summary>
    /// Captures stable structural counts and machine-specific timing from one benchmark run.
    /// </summary>
    internal readonly struct LowPolyGenerationBenchmarkReport
    {
        public LowPolyGenerationBenchmarkReport(
            int    modelCount,
            int    rendererCount,
            int    uniqueMeshCount,
            int    uniqueMaterialCount,
            double parsingMilliseconds,
            double validationMilliseconds,
            double generationMilliseconds,
            long   managedHeapGrowthBytes)
        {
            ModelCount             = modelCount;
            RendererCount          = rendererCount;
            UniqueMeshCount        = uniqueMeshCount;
            UniqueMaterialCount    = uniqueMaterialCount;
            ParsingMilliseconds    = parsingMilliseconds;
            ValidationMilliseconds = validationMilliseconds;
            GenerationMilliseconds = generationMilliseconds;
            ManagedHeapGrowthBytes = managedHeapGrowthBytes;
        }

        public int ModelCount { get; }

        public int RendererCount { get; }

        public int UniqueMeshCount { get; }

        public int UniqueMaterialCount { get; }

        public double ParsingMilliseconds { get; }

        public double ValidationMilliseconds { get; }

        public double GenerationMilliseconds { get; }

        public long ManagedHeapGrowthBytes { get; }

        public override string ToString()
        {
            double parsingPerModel    = ParsingMilliseconds / ModelCount;
            double validationPerModel = ValidationMilliseconds / ModelCount;
            double generationPerModel = GenerationMilliseconds / ModelCount;
            double heapBytesPerModel   = (double)ManagedHeapGrowthBytes / ModelCount;

            return
                $"SceneReplay JSON benchmark: {ModelCount} models, {RendererCount} renderers, " +
                $"parse {ParsingMilliseconds:F2} ms ({parsingPerModel:F3} ms/model), " +
                $"validate {ValidationMilliseconds:F2} ms ({validationPerModel:F3} ms/model), " +
                $"prepared generate {GenerationMilliseconds:F2} ms ({generationPerModel:F3} ms/model), " +
                $"{ManagedHeapGrowthBytes:N0} managed heap growth, {heapBytesPerModel:N0} bytes/model, " +
                $"{UniqueMeshCount} unique meshes, {UniqueMaterialCount} unique materials.";
        }
    }
}
