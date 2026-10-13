using System;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Reuses one validated, immutable SceneReplay definition for repeated Unity hierarchy generation.
    /// </summary>
    public sealed class UnityShapeGenerationPlan
    {
        private readonly UnityShapeModelGenerator generator;
        private readonly ShapeDefinition          definition;

        internal UnityShapeGenerationPlan(
            UnityShapeModelGenerator generator,
            ShapeDefinition          definition)
        {
            this.generator  = generator ?? throw new ArgumentNullException(nameof(generator));
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>
        /// Generates another hierarchy without repeating definition validation.
        /// </summary>
        public GameObject Generate(Transform parent = null)
        {
            return generator.GeneratePrepared(definition, parent);
        }
    }
}
