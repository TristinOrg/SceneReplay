using System;
using System.Collections.Generic;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Demonstrates transform animation through SceneReplay target interfaces without defining a motion format.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityShapeModel))]
    public sealed class LowPolyMotionExample : MonoBehaviour
    {
        [SerializeField] private LowPolyMotionPreset preset;
        [SerializeField] private float               duration = 3f;
        [SerializeField] private float               walkSpeed = 0.8f;

        private readonly List<TargetPose> targets = new();

        private UnityShapeModel model;
        private float           elapsedTime;

        /// <summary>Configures the example animation preset.</summary>
        public void Configure(LowPolyMotionPreset selectedPreset)
        {
            preset = selectedPreset;
            ResetBindings();
        }

        /// <summary>Evaluates the preset at a normalized loop time for previews and tests.</summary>
        public void Evaluate(float normalizedTime)
        {
            EnsureBindings();
            float phase = Mathf.Repeat(normalizedTime, 1f);

            switch (preset)
            {
                case LowPolyMotionPreset.RobotShowcase:
                    EvaluateRobot(phase, phase * walkSpeed * duration);
                    break;
                case LowPolyMotionPreset.WorkbenchShowcase:
                    EvaluateWorkbench(phase);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;
            EnsureBindings();
            float phase = Mathf.Repeat(elapsedTime / duration, 1f);

            switch (preset)
            {
                case LowPolyMotionPreset.RobotShowcase:
                    EvaluateRobot(phase, elapsedTime * walkSpeed);
                    break;
                case LowPolyMotionPreset.WorkbenchShowcase:
                    EvaluateWorkbench(phase);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.01f, duration);
            walkSpeed = Mathf.Max(0f, walkSpeed);
        }

        private void OnDisable()
        {
            RestoreInitialPose();
            elapsedTime = 0f;
        }

        private void EvaluateRobot(float phase, float distance)
        {
            float wave      = Mathf.Sin(phase * Mathf.PI * 2f);
            float stepLift  = Mathf.Abs(wave) * 0.045f;
            float leftKnee  = Mathf.Max(0f, wave) * 42f;
            float rightKnee = Mathf.Max(0f, -wave) * 42f;

            SetPositionOffset(0, new(0f, stepLift, -distance));
            SetRotationOffset(1, new(0f, -wave * 4f, 0f));
            SetRotationOffset(2, new(-wave * 24f, 0f, 0f));
            SetRotationOffset(3, new(wave * 24f, 0f, 0f));
            SetRotationOffset(4, new(14f + (Mathf.Max(0f, wave) * 10f), 0f, 0f));
            SetRotationOffset(5, new(14f + (Mathf.Max(0f, -wave) * 10f), 0f, 0f));
            SetRotationOffset(6, new(wave * 30f, 0f, 0f));
            SetRotationOffset(7, new(-wave * 30f, 0f, 0f));
            SetRotationOffset(8, new(-leftKnee, 0f, 0f));
            SetRotationOffset(9, new(-rightKnee, 0f, 0f));
        }

        private void EvaluateWorkbench(float phase)
        {
            float wave       = Mathf.Sin(phase * Mathf.PI * 2f);
            float openAmount = Mathf.Sin(phase * Mathf.PI);
            openAmount *= openAmount;

            SetPositionOffset(0, new(0f, 0f, -openAmount * 0.52f));
            SetRotationOffset(1, new(0f, wave * 24f, 0f));
            SetRotationOffset(2, new(0f, 0f, wave * 8f));
            SetPositionOffset(3, new(0f, openAmount * 0.04f, 0f));
        }

        private void EnsureBindings()
        {
            if (targets.Count > 0)
                return;

            model = GetComponent<UnityShapeModel>();
            if (preset == LowPolyMotionPreset.RobotShowcase)
            {
                Bind("robot");
                Bind("robot.head.pivot");
                Bind("robot.arm.left.shoulder.pivot");
                Bind("robot.arm.right.shoulder.pivot");
                Bind("robot.arm.left.elbow.pivot");
                Bind("robot.arm.right.elbow.pivot");
                Bind("robot.leg.left.hip.pivot");
                Bind("robot.leg.right.hip.pivot");
                Bind("robot.leg.left.knee.pivot");
                Bind("robot.leg.right.knee.pivot");
                return;
            }

            Bind("workbench.drawer");
            Bind("workbench.lamp");
            Bind("workbench.tool.hammer");
            Bind("workbench.mug");
        }

        private void Bind(string nodeId)
        {
            if (!model.TryGetTarget(nodeId, out IShapeTransformTarget target))
                throw new InvalidOperationException($"Low Poly motion target '{nodeId}' was not found.");

            targets.Add(new(target));
        }

        private void SetPositionOffset(int index, ForgeVector3 offset)
        {
            TargetPose pose = targets[index];
            pose.Target.LocalPosition = Add(pose.Position, offset);
        }

        private void SetRotationOffset(int index, ForgeVector3 offset)
        {
            TargetPose pose = targets[index];
            pose.Target.LocalEulerAngles = Add(pose.Rotation, offset);
        }

        private static ForgeVector3 Add(ForgeVector3 left, ForgeVector3 right)
        {
            return new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        private void RestoreInitialPose()
        {
            for (int index = 0; index < targets.Count; index++)
                targets[index].Restore();
        }

        private void ResetBindings()
        {
            RestoreInitialPose();
            targets.Clear();
            elapsedTime = 0f;
        }

        /// <summary>
        /// Caches one resolved target and its rest pose.
        /// </summary>
        private sealed class TargetPose
        {
            private readonly ForgeVector3 position;
            private readonly ForgeVector3 rotation;
            private readonly ForgeVector3 scale;

            public TargetPose(IShapeTransformTarget target)
            {
                Target   = target;
                position = target.LocalPosition;
                rotation = target.LocalEulerAngles;
                scale    = target.LocalScale;
            }

            public IShapeTransformTarget Target { get; }

            public ForgeVector3 Position => position;

            public ForgeVector3 Rotation => rotation;

            public void Restore()
            {
                Target.LocalPosition    = position;
                Target.LocalEulerAngles = rotation;
                Target.LocalScale       = scale;
            }
        }
    }

    /// <summary>
    /// Selects an official Low Poly transform animation example.
    /// </summary>
    public enum LowPolyMotionPreset
    {
        RobotShowcase     = 0,
        WorkbenchShowcase = 1
    }
}
