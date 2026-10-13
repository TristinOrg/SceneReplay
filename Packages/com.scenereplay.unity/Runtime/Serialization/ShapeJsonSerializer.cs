using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Provides the default JSON implementation for SceneReplay documents.
    /// </summary>
    public sealed class ShapeJsonSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver  = new CamelCasePropertyNamesContractResolver(),
            Formatting        = Formatting.None,
            TypeNameHandling  = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters        = { new StringEnumConverter(new CamelCaseNamingStrategy()) }
        };
        private readonly ShapeDefinitionValidator      shapeValidator = new ShapeDefinitionValidator();
        private readonly ShapeStyleDefinitionValidator styleValidator = new ShapeStyleDefinitionValidator();

        /// <summary>
        /// Serializes a shape definition using the versioned SceneReplay JSON contract.
        /// </summary>
        public string Serialize(ShapeDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return JsonConvert.SerializeObject(definition, Settings);
        }

        /// <summary>
        /// Deserializes a versioned SceneReplay shape document.
        /// </summary>
        public ShapeDefinition DeserializeShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Shape JSON cannot be empty.", nameof(json));

            ShapeDefinition definition = JsonConvert.DeserializeObject<ShapeDefinition>(json, Settings) ??
                                         throw new JsonSerializationException("Shape JSON produced no definition.");
            shapeValidator.Validate(definition);
            return definition;
        }

        /// <summary>
        /// Serializes an ordered ShapePatch document for external editing tools.
        /// </summary>
        public string Serialize(ShapePatchDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return JsonConvert.SerializeObject(document, Settings);
        }

        /// <summary>
        /// Deserializes a versioned ShapePatch document for atomic application by ShapePatchApplier.
        /// </summary>
        public ShapePatchDocument DeserializePatch(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("ShapePatch JSON cannot be empty.", nameof(json));

            ShapePatchDocument document = JsonConvert.DeserializeObject<ShapePatchDocument>(json, Settings) ??
                                          throw new JsonSerializationException("ShapePatch JSON produced no document.");
            if (!string.Equals(document.Schema, ShapePatchDocument.CurrentSchema, StringComparison.Ordinal))
                throw new JsonSerializationException($"Unsupported ShapePatch schema '{document.Schema}'.");
            if (document.Operations == null)
                throw new JsonSerializationException("ShapePatch JSON requires an operation collection.");

            return document;
        }

        /// <summary>
        /// Serializes a declarative game-asset quality policy for external authoring tools.
        /// </summary>
        public string Serialize(ShapeQualityPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            return JsonConvert.SerializeObject(policy, Settings);
        }

        /// <summary>
        /// Deserializes a versioned SceneReplay quality-policy document.
        /// </summary>
        public ShapeQualityPolicy DeserializeQualityPolicy(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Quality-policy JSON cannot be empty.", nameof(json));

            ShapeQualityPolicy policy = JsonConvert.DeserializeObject<ShapeQualityPolicy>(json, Settings) ??
                                        throw new JsonSerializationException("Quality-policy JSON produced no policy.");
            if (!string.Equals(policy.Schema, ShapeQualityPolicy.CurrentSchema, StringComparison.Ordinal))
                throw new JsonSerializationException($"Unsupported quality-policy schema '{policy.Schema}'.");

            return policy;
        }

        /// <summary>Deserializes and validates a versioned semantic detail inventory.</summary>
        public ShapeDetailInventory DeserializeDetailInventory(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Detail-inventory JSON cannot be empty.", nameof(json));

            ShapeDetailInventory inventory = JsonConvert.DeserializeObject<ShapeDetailInventory>(json, Settings) ??
                                             throw new JsonSerializationException("Detail-inventory JSON produced no inventory.");
            ShapeDiagnosticReport report = new ShapeDetailInventoryValidator().Analyze(inventory);
            if (!report.IsValid)
                throw new JsonSerializationException(report.Diagnostics[0].Message);
            return inventory;
        }

        /// <summary>Deserializes and validates a resumable construction plan.</summary>
        public ShapeConstructionPlan DeserializeConstructionPlan(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Construction-plan JSON cannot be empty.", nameof(json));

            ShapeConstructionPlan plan = JsonConvert.DeserializeObject<ShapeConstructionPlan>(json, Settings) ??
                                         throw new JsonSerializationException("Construction-plan JSON produced no plan.");
            ShapeDiagnosticReport report = new ShapeConstructionPlanValidator().Analyze(plan);
            if (!report.IsValid)
                throw new JsonSerializationException(report.Diagnostics[0].Message);
            return plan;
        }

        /// <summary>Deserializes and validates portable game-semantic metadata.</summary>
        public ShapeGameMetadata DeserializeGameMetadata(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Game-metadata JSON cannot be empty.", nameof(json));

            ShapeGameMetadata metadata = JsonConvert.DeserializeObject<ShapeGameMetadata>(json, Settings) ??
                                         throw new JsonSerializationException("Game-metadata JSON produced no metadata.");
            ShapeDiagnosticReport report = new ShapeGameMetadataValidator().Analyze(metadata);
            if (!report.IsValid)
                throw new JsonSerializationException(report.Diagnostics[0].Message);
            return metadata;
        }

        /// <summary>
        /// Serializes a style definition using the versioned SceneReplay JSON contract.
        /// </summary>
        public string Serialize(ShapeStyleDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return JsonConvert.SerializeObject(definition, Settings);
        }

        /// <summary>
        /// Serializes a versioned shape-capability catalog for external authoring tools.
        /// </summary>
        public string Serialize(ShapeCapabilityCatalogDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return JsonConvert.SerializeObject(document, Settings);
        }

        /// <summary>
        /// Serializes versioned semantic-template discovery data for external authoring tools.
        /// </summary>
        public string Serialize(ShapeTemplateCatalogDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return JsonConvert.SerializeObject(document, Settings);
        }

        /// <summary>
        /// Deserializes a versioned SceneReplay style document.
        /// </summary>
        public ShapeStyleDefinition DeserializeStyle(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Style JSON cannot be empty.", nameof(json));

            ShapeStyleDefinition definition = JsonConvert.DeserializeObject<ShapeStyleDefinition>(json, Settings) ??
                                              throw new JsonSerializationException("Style JSON produced no definition.");
            styleValidator.Validate(definition);
            return definition;
        }

        /// <summary>
        /// Serializes a template-owned semantic specification without coupling the adapter to its package.
        /// </summary>
        public string SerializeSpecification<TSpecification>(TSpecification specification)
            where TSpecification : class
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            return JsonConvert.SerializeObject(specification, Settings);
        }

        /// <summary>
        /// Deserializes and validates a template-owned semantic specification.
        /// </summary>
        public TSpecification DeserializeSpecification<TSpecification>(
            string                 json,
            Action<TSpecification> validate)
            where TSpecification : class
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Specification JSON cannot be empty.", nameof(json));

            if (validate == null)
                throw new ArgumentNullException(nameof(validate));

            TSpecification specification = JsonConvert.DeserializeObject<TSpecification>(json, Settings) ??
                                           throw new JsonSerializationException(
                                               "Specification JSON produced no definition.");
            validate(specification);
            return specification;
        }
    }
}
