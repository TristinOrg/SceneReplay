using System;
using System.Collections.Generic;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Reuses a complete Low Poly pipeline for validated definitions and external JSON documents.
    /// </summary>
    public sealed class LowPolyModelGenerator
    {
        private readonly ShapeJsonSerializer      serializer = new();
        private readonly ShapeStyleResolver       styleResolver;
        private readonly UnityShapeModelGenerator modelGenerator;

        /// <summary>
        /// Initializes a reusable Low Poly generation pipeline.
        /// </summary>
        public LowPolyModelGenerator(
            IEnumerable<ShapeStyleDefinition> styles           = null,
            ShapeValidationLimits              validationLimits = null)
        {
            styleResolver  = new(styles ?? Array.Empty<ShapeStyleDefinition>());
            modelGenerator = new(
                new IUnityShapeGenerator[] { new LowPolyPrimitiveGenerator() },
                styleResolver,
                validationLimits: validationLimits);
        }

        /// <summary>
        /// Adds or replaces an engine-agnostic style for subsequent generations.
        /// </summary>
        public void SetStyle(ShapeStyleDefinition style)
        {
            styleResolver.Set(style);
        }

        /// <summary>
        /// Adds or replaces a validated style JSON document for subsequent generations.
        /// </summary>
        public void SetStyleJson(string json)
        {
            styleResolver.Set(serializer.DeserializeStyle(json));
        }

        /// <summary>
        /// Validates and generates a model from an engine-agnostic definition.
        /// </summary>
        public GameObject Generate(ShapeDefinition definition, Transform parent = null)
        {
            return modelGenerator.Generate(definition, parent);
        }

        /// <summary>
        /// Atomically replaces an existing generated model after the new definition succeeds.
        /// </summary>
        public GameObject Regenerate(UnityShapeModel existingModel, ShapeDefinition definition)
        {
            return modelGenerator.Regenerate(existingModel, definition);
        }

        /// <summary>
        /// Creates a caller-controlled batch for repeated generation without a single-frame spike.
        /// </summary>
        public LowPolyGenerationBatch CreateBatch(
            ShapeDefinition    definition,
            int                totalCount,
            Transform          parent      = null,
            Action<GameObject> onGenerated = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (totalCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalCount));

            UnityShapeGenerationPlan plan = modelGenerator.Prepare(definition);
            return new(plan, totalCount, parent, onGenerated);
        }

        /// <summary>
        /// Parses and validates JSON once for efficient repeated model generation.
        /// </summary>
        public ShapeDefinition ParseJson(string json)
        {
            return serializer.DeserializeShape(json);
        }

        /// <summary>
        /// Parses and generates a one-off model directly from a SceneReplay JSON document.
        /// </summary>
        public GameObject GenerateJson(string json, Transform parent = null)
        {
            return Generate(ParseJson(json), parent);
        }

        /// <summary>
        /// Parses external JSON and replaces an existing model only after parsing and generation succeed.
        /// </summary>
        public GameObject RegenerateJson(UnityShapeModel existingModel, string json)
        {
            return Regenerate(existingModel, ParseJson(json));
        }
    }
}
