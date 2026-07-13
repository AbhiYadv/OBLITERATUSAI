using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Idle/Walk/Run animator controllers for the Quaternius animated
    /// characters, plus SitDown/StandUp states for seat interactions.
    /// "Walking" and "Running" bools switch locomotion with short
    /// crossfades; the runtime drivers keep scaling Animator.speed while
    /// moving so foot pace follows movement speed, and drop it back to 1
    /// while idle or seated so those clips play at authored pace. The
    /// "Sitting" bool enters SitDown from any state; the non-looping clip
    /// holds its final seated pose until Sitting clears, then StandUp plays
    /// through back to Idle.
    /// </summary>
    internal static class CharacterAnimationBuilder
    {
        public const string WalkingParameter = "Walking";
        public const string RunningParameter = "Running";
        public const string SittingParameter = "Sitting";

        /// <summary>
        /// Create or rebuild a controller from the model's imported clips.
        /// Returns null when the model has no usable walk clip.
        /// </summary>
        public static AnimatorController EnsureController(
            string controllerPath,
            string modelAssetPath)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelAssetPath);
            AnimationClip[] clips = subAssets
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .ToArray();
            AnimationClip walk = FindClip(clips, "Walk") ?? clips.FirstOrDefault();
            AnimationClip idle = FindClip(clips, "Idle");
            AnimationClip run = FindClip(clips, "Run");
            AnimationClip sitDown = FindClip(clips, "SitDown");
            AnimationClip standUp = FindClip(clips, "StandUp");
            if (walk == null)
            {
                return null;
            }

            EnsureLooping(walk);
            EnsureLooping(idle);
            EnsureLooping(run);
            // glTFast imports every clip with looping enabled. The seat
            // clips must NOT loop: SitDown holds its final seated pose and
            // StandUp plays through exactly once — looping made characters
            // re-sit forever, swinging through the bench.
            SetLooping(sitDown, false);
            SetLooping(standUp, false);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            EnsureParameter(controller, WalkingParameter);
            EnsureParameter(controller, RunningParameter);
            EnsureParameter(controller, SittingParameter);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            AnimatorState walkState = machine.AddState("Walk");
            walkState.motion = walk;
            AnimatorState idleState = null;
            if (idle != null)
            {
                idleState = machine.AddState("Idle");
                idleState.motion = idle;
                machine.defaultState = idleState;

                AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
                toWalk.AddCondition(AnimatorConditionMode.If, 0f, WalkingParameter);
                toWalk.hasExitTime = false;
                toWalk.duration = 0.15f;

                AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
                toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, WalkingParameter);
                toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, RunningParameter);
                toIdle.hasExitTime = false;
                toIdle.duration = 0.2f;
            }
            else
            {
                machine.defaultState = walkState;
            }

            if (run != null)
            {
                AnimatorState runState = machine.AddState("Run");
                runState.motion = run;

                AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
                walkToRun.AddCondition(AnimatorConditionMode.If, 0f, RunningParameter);
                walkToRun.hasExitTime = false;
                walkToRun.duration = 0.12f;

                AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
                runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, RunningParameter);
                runToWalk.AddCondition(AnimatorConditionMode.If, 0f, WalkingParameter);
                runToWalk.hasExitTime = false;
                runToWalk.duration = 0.15f;

                if (idleState != null)
                {
                    AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
                    idleToRun.AddCondition(AnimatorConditionMode.If, 0f, RunningParameter);
                    idleToRun.hasExitTime = false;
                    idleToRun.duration = 0.12f;

                    AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
                    runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, RunningParameter);
                    runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, WalkingParameter);
                    runToIdle.hasExitTime = false;
                    runToIdle.duration = 0.2f;
                }
            }

            if (sitDown != null && standUp != null && idleState != null)
            {
                AnimatorState sitState = machine.AddState("SitDown");
                sitState.motion = sitDown; // not looped: holds the seated pose
                AnimatorState standState = machine.AddState("StandUp");
                standState.motion = standUp;

                AnimatorStateTransition anyToSit = machine.AddAnyStateTransition(sitState);
                anyToSit.AddCondition(AnimatorConditionMode.If, 0f, SittingParameter);
                anyToSit.hasExitTime = false;
                anyToSit.duration = 0.2f;
                anyToSit.canTransitionToSelf = false;

                AnimatorStateTransition sitToStand = sitState.AddTransition(standState);
                sitToStand.AddCondition(AnimatorConditionMode.IfNot, 0f, SittingParameter);
                sitToStand.hasExitTime = false;
                sitToStand.duration = 0.1f;

                AnimatorStateTransition standToIdle = standState.AddTransition(idleState);
                standToIdle.hasExitTime = true;
                standToIdle.exitTime = 0.92f;
                standToIdle.duration = 0.15f;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureParameter(AnimatorController controller, string name)
        {
            if (!controller.parameters.Any(p => p.name == name))
            {
                controller.AddParameter(name, AnimatorControllerParameterType.Bool);
            }
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string name)
        {
            return clips.FirstOrDefault(
                clip => string.Equals(clip.name, name, System.StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureLooping(AnimationClip clip)
        {
            SetLooping(clip, true);
        }

        private static void SetLooping(AnimationClip clip, bool loop)
        {
            if (clip == null || clip.isLooping == loop)
            {
                return;
            }

            // glTFast clips are sub-assets of the imported model; loop time
            // lives in the serialized clip settings and is safe to set from
            // editor code.
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
    }
}
