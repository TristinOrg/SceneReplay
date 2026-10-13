using System;
using System.Diagnostics;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Spreads repeated model generation across caller-controlled steps without scheduler allocations.
    /// </summary>
    public sealed class LowPolyGenerationBatch
    {
        private readonly UnityShapeGenerationPlan plan;
        private readonly Transform                parent;
        private readonly Action<GameObject>       onGenerated;

        internal LowPolyGenerationBatch(
            UnityShapeGenerationPlan plan,
            int                      totalCount,
            Transform                parent,
            Action<GameObject>       onGenerated)
        {
            this.plan        = plan;
            this.parent      = parent;
            this.onGenerated = onGenerated;
            TotalCount       = totalCount;
        }

        /// <summary>
        /// Gets the requested number of model instances.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Gets the number of model instances generated so far.
        /// </summary>
        public int GeneratedCount { get; private set; }

        /// <summary>
        /// Gets whether every requested model has been generated.
        /// </summary>
        public bool IsCompleted => GeneratedCount >= TotalCount;

        /// <summary>
        /// Generates up to the supplied model budget and returns the generated count for this step.
        /// </summary>
        public int GenerateNext(int modelBudget)
        {
            if (modelBudget <= 0)
                throw new ArgumentOutOfRangeException(nameof(modelBudget));

            int stepCount = Math.Min(modelBudget, TotalCount - GeneratedCount);
            for (int index = 0; index < stepCount; index++)
                GenerateOne();

            return stepCount;
        }

        /// <summary>
        /// Generates models until the supplied elapsed-time budget is reached.
        /// </summary>
        public int GenerateForMilliseconds(double millisecondBudget)
        {
            if (double.IsNaN(millisecondBudget) || millisecondBudget <= 0d)
                throw new ArgumentOutOfRangeException(nameof(millisecondBudget));

            long startedAt      = Stopwatch.GetTimestamp();
            int  generatedCount = 0;

            while (!IsCompleted)
            {
                GenerateOne();
                generatedCount++;

                double elapsedMilliseconds =
                    (Stopwatch.GetTimestamp() - startedAt) * 1000d / Stopwatch.Frequency;
                if (elapsedMilliseconds >= millisecondBudget)
                    break;
            }

            return generatedCount;
        }

        private void GenerateOne()
        {
            GameObject generated = plan.Generate(parent);
            GeneratedCount++;
            onGenerated?.Invoke(generated);
        }
    }
}
