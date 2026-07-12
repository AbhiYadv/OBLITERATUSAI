using System.Collections.Generic;
using ObliteratusAI.Core;
using ObliteratusAI.Pedestrians;
using ObliteratusAI.Player;
using ObliteratusAI.Traffic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Focused Play Mode smoke test for ambient pedestrians. The validator
    /// survives the Play Mode domain reload through SessionState and is also
    /// callable from Unity batch mode with -executeMethod.
    /// </summary>
    [InitializeOnLoad]
    internal static class PedestrianSimulationValidator
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/PlayerSandbox.unity";
        private const string DefinitionPath =
            "Assets/_Project/Data/Characters/Pedestrian_Casual.asset";
        private const string StateKey = "ObliteratusAI.PedestrianSmoke.State";
        private const string BatchKey = "ObliteratusAI.PedestrianSmoke.Batch";
        private const string PassedKey = "ObliteratusAI.PedestrianSmoke.Passed";
        private const string MessageKey = "ObliteratusAI.PedestrianSmoke.Message";

        private static readonly Dictionary<int, Vector3> PreTeleportPositions =
            new Dictionary<int, Vector3>(32);

        private static bool _observing;
        private static bool _prepared;
        private static bool _sawIdle;
        private static bool _sawWait;
        private static bool _sawCrossing;
        private static int _phase;
        private static float _simulationStartedAt;
        private static float _phaseStartedAt;
        private static double _wallStartedAt;

        static PedestrianSimulationValidator()
        {
            EditorApplication.delayCall += ResumeIfNeeded;
        }

        [MenuItem("OBLITERATUS AI/Validation/Run Pedestrian Simulation Smoke Test")]
        public static void RunFromMenu()
        {
            StartValidation(batchMode: false);
        }

        public static void RunBatch()
        {
            StartValidation(batchMode: true);
        }

        private static void StartValidation(bool batchMode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Stop Play Mode before starting the pedestrian smoke test.");
                return;
            }

            SessionState.SetInt(StateKey, 1);
            SessionState.SetBool(BatchKey, batchMode);
            SessionState.SetBool(PassedKey, false);
            SessionState.SetString(MessageKey, "Pedestrian smoke test did not finish.");
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.isPlaying = true;
        }

        private static void ResumeIfNeeded()
        {
            int state = SessionState.GetInt(StateKey, 0);
            if (state == 0)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (state == 1 && EditorApplication.isPlaying)
            {
                BeginObservation();
            }
            else if (state == 2 && EditorApplication.isPlaying)
            {
                BeginObservation();
            }
            else if (state == 3 && !EditorApplication.isPlaying)
            {
                FinishValidation();
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetInt(StateKey, 2);
                BeginObservation();
            }
            else if (change == PlayModeStateChange.EnteredEditMode
                && SessionState.GetInt(StateKey, 0) == 3)
            {
                FinishValidation();
            }
        }

        private static void BeginObservation()
        {
            if (_observing)
            {
                return;
            }

            _observing = true;
            _prepared = false;
            _sawIdle = false;
            _sawWait = false;
            _sawCrossing = false;
            _phase = 0;
            _wallStartedAt = EditorApplication.timeSinceStartup;
            PreTeleportPositions.Clear();
            EditorApplication.update -= ObservePlayMode;
            EditorApplication.update += ObservePlayMode;
        }

        private static void ObservePlayMode()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    return;
                }
                if (EditorApplication.timeSinceStartup - _wallStartedAt > 25.0)
                {
                    EndPlayMode(false, "Timed out while observing pedestrian simulation.");
                    return;
                }

                PedestrianAgent[] agents = Object.FindObjectsByType<PedestrianAgent>(
                    FindObjectsSortMode.None);
                TrafficVehicle[] vehicles = Object.FindObjectsByType<TrafficVehicle>(
                    FindObjectsSortMode.None);
                if (!_prepared)
                {
                    if (agents.Length != 28 || vehicles.Length != 24)
                    {
                        return;
                    }

                    PrepareDeterministicBehaviors(agents);
                    Time.timeScale = 4f;
                    _simulationStartedAt = Time.time;
                    _prepared = true;
                }

                for (int i = 0; i < agents.Length; i++)
                {
                    switch (agents[i].CurrentState)
                    {
                        case PedestrianAgent.PedestrianState.Idling:
                            _sawIdle = true;
                            break;
                        case PedestrianAgent.PedestrianState.WaitingToCross:
                            _sawWait = true;
                            break;
                        case PedestrianAgent.PedestrianState.Crossing:
                            _sawCrossing = true;
                            break;
                    }
                }

                if (_phase == 0 && Time.time - _simulationStartedAt >= 24f)
                {
                    if (!_sawIdle || !_sawWait || !_sawCrossing)
                    {
                        EndPlayMode(
                            false,
                            $"Missing observed states: idle={_sawIdle}, "
                            + $"wait={_sawWait}, crossing={_sawCrossing}.");
                        return;
                    }

                    PreTeleportPositions.Clear();
                    for (int i = 0; i < agents.Length; i++)
                    {
                        PreTeleportPositions[agents[i].GetInstanceID()] = agents[i].Position;
                    }
                    TeleportPlayer(new Vector3(-135f, 1.35f, 125f));
                    _phase = 1;
                    _phaseStartedAt = Time.time;
                }
                else if (_phase == 1 && Time.time - _phaseStartedAt >= 5f)
                {
                    int recycled = 0;
                    for (int i = 0; i < agents.Length; i++)
                    {
                        if (!PreTeleportPositions.TryGetValue(
                                agents[i].GetInstanceID(),
                                out Vector3 previous))
                        {
                            continue;
                        }
                        Vector3 delta = agents[i].Position - previous;
                        delta.y = 0f;
                        if (delta.sqrMagnitude > 30f * 30f)
                        {
                            recycled++;
                        }
                    }

                    bool passed = recycled > 0;
                    EndPlayMode(
                        passed,
                        passed
                            ? $"Observed idle, curb wait, crossing, and {recycled} pooled recycles."
                            : "No off-screen pooled pedestrian recycle was observed after teleport.");
                }
            }
            catch (System.Exception exception)
            {
                EndPlayMode(false, $"Pedestrian smoke test exception: {exception}");
            }
        }

        private static void PrepareDeterministicBehaviors(PedestrianAgent[] agents)
        {
            PedestrianDefinition definition =
                AssetDatabase.LoadAssetAtPath<PedestrianDefinition>(DefinitionPath);
            if (definition == null)
            {
                throw new System.InvalidOperationException(
                    $"Missing pedestrian definition at {DefinitionPath}.");
            }

            // One slow mid-block agent deterministically reaches an idle.
            agents[0].Recycle(13, 0.1f, 0.1f, 1u);

            // Eight agents approach two northbound crossings from separate
            // curb queues. Seeds are selected so their next corner decision
            // chooses the crosswalk branch.
            for (int i = 1; i <= 8; i++)
            {
                int queueSlot = (i - 1) / 2;
                int blockIndex = (i & 1) == 0 ? 13 : 14;
                float fraction = 0.476f + queueSlot * 0.007f;
                uint behaviorSeed = FindCrosswalkSeed((uint)(100 + i), definition.CrosswalkChance);
                agents[i].Recycle(blockIndex, fraction, 1.55f, behaviorSeed);
            }
        }

        private static uint FindCrosswalkSeed(uint firstSeed, float chance)
        {
            for (uint seed = firstSeed; seed < firstSeed + 10000u; seed++)
            {
                DeterministicRandom random = new DeterministicRandom(seed);
                random.NextFloat(); // Scheduled idle interval during Recycle.
                if (random.NextFloat() < chance)
                {
                    return seed;
                }
            }
            throw new System.InvalidOperationException("Could not find a crosswalk behavior seed.");
        }

        private static void TeleportPlayer(Vector3 position)
        {
            PlayerMotor player = Object.FindFirstObjectByType<PlayerMotor>();
            if (player == null)
            {
                throw new System.InvalidOperationException("Smoke test could not find PlayerMotor.");
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled)
            {
                controller.enabled = false;
            }
            player.transform.position = position;
            if (wasEnabled)
            {
                controller.enabled = true;
            }
        }

        private static void EndPlayMode(bool passed, string message)
        {
            if (SessionState.GetInt(StateKey, 0) == 3)
            {
                return;
            }

            Time.timeScale = 1f;
            SessionState.SetBool(PassedKey, passed);
            SessionState.SetString(MessageKey, message);
            SessionState.SetInt(StateKey, 3);
            EditorApplication.update -= ObservePlayMode;
            _observing = false;
            EditorApplication.isPlaying = false;
        }

        private static void FinishValidation()
        {
            bool passed = SessionState.GetBool(PassedKey, false);
            bool batchMode = SessionState.GetBool(BatchKey, false);
            string message = SessionState.GetString(
                MessageKey,
                "Pedestrian smoke test finished without a result message.");

            SessionState.EraseInt(StateKey);
            SessionState.EraseBool(BatchKey);
            SessionState.EraseBool(PassedKey);
            SessionState.EraseString(MessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (passed)
            {
                Debug.Log($"Pedestrian simulation smoke test passed: {message}");
            }
            else
            {
                Debug.LogError($"Pedestrian simulation smoke test failed: {message}");
            }

            if (batchMode)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }
}
