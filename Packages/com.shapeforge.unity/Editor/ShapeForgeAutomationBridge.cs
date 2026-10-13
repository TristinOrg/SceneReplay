using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEditor;

namespace ShapeForge.Unity.Editor
{
    /// <summary>
    /// Executes file-based automation requests through the real ShapeForge C# implementation.
    /// </summary>
    public static class ShapeForgeAutomationBridge
    {
        private const string RequestPath = "Library/ShapeForgeAutomation/request.json";
        private const string ResultPath  = "Library/ShapeForgeAutomation/result.json";

        private static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting       = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>Processes the pending request written by the ShapeForge Python CLI.</summary>
        [MenuItem("ShapeForge/Automation/Process Request")]
        public static void ProcessRequest()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            try
            {
                AutomationRequest request = JsonConvert.DeserializeObject<AutomationRequest>(File.ReadAllText(RequestPath)) ??
                                            throw new InvalidOperationException("Automation request is empty.");
                WriteResult(Execute(request));
            }
            catch (Exception exception)
            {
                WriteResult(new AutomationResult { Success = false, Error = exception.Message });
            }
        }

        private static AutomationResult Execute(AutomationRequest request)
        {
            ShapeJsonSerializer serializer = new();
            switch (request.Command)
            {
                case "validate":
                {
                    ShapeDefinition definition = JsonConvert.DeserializeObject<ShapeDefinition>(Read(request.Source), Settings);
                    ShapeDiagnosticReport report = new ShapeDefinitionValidator().Analyze(definition);
                    return Result(report.IsValid, JToken.FromObject(report, JsonSerializer.Create(Settings)));
                }
                case "diff":
                {
                    ShapeDefinition before = serializer.DeserializeShape(Read(request.Source));
                    ShapeDefinition after  = serializer.DeserializeShape(Read(request.Other));
                    ShapeDiffReport report = new ShapeDefinitionDiffer().Compare(before, after);
                    return Result(true, JToken.FromObject(report, JsonSerializer.Create(Settings)));
                }
                case "patch":
                {
                    ShapeDefinition definition = serializer.DeserializeShape(Read(request.Source));
                    ShapePatchDocument patch    = serializer.DeserializePatch(Read(request.Other));
                    ShapePatchResult result     = new ShapePatchApplier().TryApply(definition, patch);
                    JToken data = result.Succeeded
                        ? JToken.Parse(serializer.Serialize(result.Definition))
                        : JToken.FromObject(result.Diagnostics, JsonSerializer.Create(Settings));
                    return Result(result.Succeeded, data);
                }
                case "quality":
                {
                    ShapeDefinition definition = serializer.DeserializeShape(Read(request.Source));
                    ShapeQualityPolicy policy   = serializer.DeserializeQualityPolicy(Read(request.Other));
                    ShapeQualityReport report   = new ShapeQualityGate().Evaluate(definition, policy);
                    return Result(report.Passed, JToken.FromObject(report, JsonSerializer.Create(Settings)));
                }
                case "inventory":
                {
                    ShapeDefinition definition = serializer.DeserializeShape(Read(request.Source));
                    ShapeDetailInventory inventory = serializer.DeserializeDetailInventory(Read(request.Other));
                    ShapeDetailCoverageReport report = new ShapeDetailCoverageAnalyzer().Analyze(definition, inventory);
                    return Result(report.Passed, JToken.FromObject(report, JsonSerializer.Create(Settings)));
                }
                case "plan":
                {
                    ShapeConstructionPlan plan = serializer.DeserializeConstructionPlan(Read(request.Source));
                    ShapeConstructionPlanReport report = new ShapeConstructionPlanEvaluator().Evaluate(plan);
                    return Result(report.Diagnostics.IsValid, JToken.FromObject(report, JsonSerializer.Create(Settings)));
                }
                case "step":
                {
                    ShapeDefinition definition = serializer.DeserializeShape(Read(request.Source));
                    ShapeConstructionPlan plan = serializer.DeserializeConstructionPlan(Read(request.Other));
                    ShapeConstructionStepResult result = new ShapeConstructionPlanExecutor().Apply(
                        definition, plan, request.Argument);
                    JObject data = new()
                    {
                        ["diagnostics"] = JToken.FromObject(result.Diagnostics, JsonSerializer.Create(Settings))
                    };
                    if (result.Succeeded)
                    {
                        data["definition"] = JToken.Parse(serializer.Serialize(result.Definition));
                        data["plan"] = JToken.FromObject(result.Plan, JsonSerializer.Create(Settings));
                    }
                    return Result(result.Succeeded, data);
                }
                case "game":
                {
                    ShapeDefinition definition = serializer.DeserializeShape(Read(request.Source));
                    ShapeGameMetadata metadata  = serializer.DeserializeGameMetadata(Read(request.Other));
                    ShapeGameMetadataReport report = new ShapeGameMetadataAnalyzer().Analyze(definition, metadata);
                    return Result(report.IsValid, JToken.FromObject(report, JsonSerializer.Create(Settings)));
                }
                case "discover":
                {
                    JsonSerializer json = JsonSerializer.Create(Settings);
                    JArray capabilities = new();
                    JArray templates    = new();
                    foreach (IShapeAutomationCatalogProvider provider in ShapeAutomationCatalogRegistry.GetProviders())
                    {
                        capabilities.Add(JToken.FromObject(provider.CreateCapabilities(), json));
                        templates.Add(JToken.FromObject(provider.CreateTemplates(), json));
                    }
                    JObject data = new()
                    {
                        ["capabilities"] = capabilities,
                        ["templates"]    = templates
                    };
                    return Result(true, data);
                }
                case "export-glb":
                {
                    ShapeDefinition definition = serializer.DeserializeShape(Read(request.Source));
                    if (string.IsNullOrWhiteSpace(request.Argument))
                        throw new InvalidOperationException("GLB export requires an output path.");
                    UnityEngine.GameObject generated = null;
                    try
                    {
                        foreach (IShapeAutomationModelCompiler compiler in ShapeAutomationCatalogRegistry.GetModelCompilers())
                        {
                            if (!compiler.CanCompile(definition))
                                continue;
                            generated = compiler.Compile(definition);
                            break;
                        }
                        if (generated == null)
                            throw new InvalidOperationException("No registered automation compiler supports this definition.");
                        UnityShapeGlbExportResult result = UnityShapeGlbExporter.Export(generated, request.Argument);
                        return Result(true, JToken.FromObject(result, JsonSerializer.Create(Settings)));
                    }
                    finally
                    {
                        if (generated != null)
                            UnityEngine.Object.DestroyImmediate(generated);
                    }
                }
                default:
                    throw new InvalidOperationException($"Unknown automation command '{request.Command}'.");
            }
        }

        private static string Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Automation command requires an input path.");
            return File.ReadAllText(Path.GetFullPath(path));
        }

        private static AutomationResult Result(bool success, JToken data) =>
            new() { Success = success, Data = data };

        private static void WriteResult(AutomationResult result)
        {
            File.WriteAllText(ResultPath, JsonConvert.SerializeObject(result, Settings));
        }

        [Serializable]
        private sealed class AutomationRequest
        {
            public string Command { get; set; }
            public string Source { get; set; }
            public string Other { get; set; }
            public string Argument { get; set; }
        }

        [Serializable]
        private sealed class AutomationResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public JToken Data { get; set; }
        }
    }
}
