using System.Collections.Generic;
using System.Linq;
using ObliteratusAI.CameraSystem;
using ObliteratusAI.Debugging;
using ObliteratusAI.Interactions;
using ObliteratusAI.Pedestrians;
using ObliteratusAI.Player;
using ObliteratusAI.Traffic;
using ObliteratusAI.Vehicles;
using ObliteratusAI.World;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ObliteratusAI.EditorTools
{
    public static class PlayerSandboxBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/PlayerSandbox.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/PrototypePlayer.prefab";
        // Exclusive player skin: Suit_Male is reserved for the player and is
        // deliberately absent from the pedestrian variant roster.
        private const string PlayerCharacterModelPath =
            "Assets/ThirdParty/Quaternius/UltimateAnimatedCharacters/Models/Suit_Male.gltf";
        private const string PlayerCharacterControllerPath =
            "Assets/_Project/Art/Characters/Player_SuitMale.controller";
        private const string CharacterModelPath = "Assets/ThirdParty/Khronos/CesiumMan/human-casual.glb";
        private const string CharacterControllerPath = "Assets/_Project/Art/Characters/CesiumMan.controller";
        private const string VehicleDefinitionPath = "Assets/_Project/Data/Vehicles/PrototypeSedan.asset";
        private const string VehicleVisualPrefabPath = "Assets/_Project/Prefabs/Vehicles/PrototypeSedanVisual.prefab";
        private const string VehiclePrefabPath = "Assets/_Project/Prefabs/Vehicles/PrototypeSedan.prefab";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const string ReflectionProbePath =
            "Assets/_Project/Scenes/Lighting/DowntownReflectionProbe.exr";

        [MenuItem("OBLITERATUS AI/Build Player Sandbox")]
        public static void Build()
        {
            ProjectLayerBootstrap.EnsureLayers();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material playerMaterial = GetOrCreateMaterial("M_PrototypePlayer", new Color(0.96f, 0.72f, 0.16f));
            Material vehicleBody = GetOrCreateMaterial("M_VehicleBody", new Color(0.04f, 0.72f, 0.66f));
            Material vehicleTrim = GetOrCreateMaterial("M_VehicleTrim", new Color(0.025f, 0.035f, 0.045f));

            ReflectionProbe reflectionProbe = CreateLighting();
            CreateWind();
            GameObject legacyVehicleVisual = LegacyCityPortBuilder.Build();
            GameObject player = CreatePlayer(playerMaterial);
            CreateCamera(player.transform);
            CreateGarage();
            CreateVehicle(vehicleBody, vehicleTrim, legacyVehicleVisual);
            CreateCityLifeSystems();

            PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            BakeReflectionProbe(reflectionProbe);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log($"Player sandbox created at {ScenePath}");
        }

        private static ReflectionProbe CreateLighting()
        {
            GameObject lightObject = new GameObject("Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.95f, 0.97f, 1f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            // Stand-in for the legacy player-following sun rig: a directional
            // light with enough shadow range to cover the downtown view.
            if (GraphicsSettings.currentRenderPipeline
                is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urpAsset
                && urpAsset.shadowDistance < 150f)
            {
                urpAsset.shadowDistance = 150f;
                EditorUtility.SetDirty(urpAsset);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.3f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.16f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.055f, 0.06f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.16f, 0.24f, 0.3f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 95f;

            GameObject probeObject = new GameObject("DowntownReflectionProbe");
            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.resolution = 128;
            probe.hdr = true;
            probe.intensity = 0.75f;
            probe.boxProjection = true;
            probe.size = new Vector3(340f, 90f, 340f);
            probe.center = new Vector3(0f, 28f, 0f);
            probe.blendDistance = 24f;
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = 650f;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            return probe;
        }

        private static void BakeReflectionProbe(ReflectionProbe probe)
        {
            EditorBuildUtility.EnsureFolder("Assets/_Project/Scenes/Lighting");
            if (AssetDatabase.LoadAssetAtPath<Texture>(ReflectionProbePath) != null)
            {
                AssetDatabase.DeleteAsset(ReflectionProbePath);
            }

            if (!Lightmapping.BakeReflectionProbe(probe, ReflectionProbePath))
            {
                Debug.LogWarning(
                    $"Downtown reflection probe could not be baked to {ReflectionProbePath}.",
                    probe);
                return;
            }

            AssetDatabase.ImportAsset(ReflectionProbePath, ImportAssetOptions.ForceUpdate);
            Texture bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(ReflectionProbePath);
            probe.mode = ReflectionProbeMode.Custom;
            probe.customBakedTexture = bakedTexture;
            EditorUtility.SetDirty(probe);
        }

        private static void CreateWind()
        {
            GameObject windObject = new GameObject("WorldWind", typeof(WindZone));
            windObject.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            windObject.AddComponent<TreeWindController>().Configure(
                main: 0.32f,
                turbulence: 0.16f,
                pulseMagnitude: 0.22f,
                pulseFrequency: 0.11f);
        }

        private static GameObject CreatePlayer(Material material)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PrototypePlayer";
            // Spawns on the garage forecourt at the east ring road, not in
            // the middle of a street.
            player.transform.position = new Vector3(157f, 1.35f, 22f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.GetComponent<Renderer>().sharedMaterial = material;

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.center = Vector3.zero;
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 50f;
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerInteractor>();
            AttachCharacterVisual(player, material);
            return player;
        }

        private static void AttachCharacterVisual(GameObject player, Material fallbackMaterial)
        {
            Renderer capsuleRenderer = player.GetComponent<Renderer>();
            string modelPath = PlayerCharacterModelPath;
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                modelPath = CharacterModelPath;
                modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            }
            if (modelAsset == null)
            {
                capsuleRenderer.sharedMaterial = fallbackMaterial;
                Debug.LogWarning($"Character model is not imported yet; using capsule visual: {modelPath}");
                return;
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset, player.transform) as GameObject;
            if (visual == null)
            {
                capsuleRenderer.sharedMaterial = fallbackMaterial;
                Debug.LogWarning("Character model could not be instantiated; using capsule visual.");
                return;
            }

            visual.name = "CharacterVisual";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (!TryGetCombinedBounds(renderers, out Bounds bounds) || bounds.size.y <= 0.001f)
            {
                Object.DestroyImmediate(visual);
                capsuleRenderer.sharedMaterial = fallbackMaterial;
                Debug.LogWarning("Character model has no usable renderer bounds; using capsule visual.");
                return;
            }

            const float targetHeight = 1.8f;
            float uniformScale = targetHeight / bounds.size.y;
            visual.transform.localScale = Vector3.one * uniformScale;

            if (TryGetCombinedBounds(renderers, out bounds))
            {
                float capsuleFeetY = player.transform.position.y - 1f;
                visual.transform.position += Vector3.up * (capsuleFeetY - bounds.min.y);
            }

            capsuleRenderer.enabled = false;
            ConfigureCharacterAnimation(player, visual, modelPath);
        }

        private static void ConfigureCharacterAnimation(
            GameObject player,
            GameObject visual,
            string modelPath)
        {
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning("Character rig was not imported; movement animation is disabled.");
                return;
            }

            AnimatorController controller;
            if (modelPath == PlayerCharacterModelPath)
            {
                // Quaternius character: Idle/Walk controller from its own
                // clip set.
                controller = CharacterAnimationBuilder.EnsureController(
                    PlayerCharacterControllerPath, modelPath);
            }
            else
            {
                // Legacy CesiumMan fallback: single looping walk clip.
                AnimationClip animationClip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
                if (animationClip == null)
                {
                    Debug.LogWarning("Character animation clip was not imported; movement animation is disabled.");
                    return;
                }
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterControllerPath);
                if (controller == null)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(
                        CharacterControllerPath,
                        animationClip);
                }
            }

            if (controller == null)
            {
                Debug.LogWarning("No animator controller could be built; movement animation is disabled.");
                return;
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            player.AddComponent<PlayerAnimationDriver>().Configure(animator);
            Debug.Log($"Character animation configured from '{modelPath}'.");
        }

        private static bool TryGetCombinedBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static void CreateCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("GameplayCamera", typeof(UnityEngine.Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = target.position + new Vector3(0f, 3f, -5f);
            UnityEngine.Camera gameCamera = cameraObject.GetComponent<UnityEngine.Camera>();
            gameCamera.fieldOfView = 65f;
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 500f;
            cameraObject.AddComponent<ThirdPersonCamera>().SetTarget(target);
        }

        /// <summary>
        /// Home garage for the player's car at the east ring road, opening
        /// west onto the street. The driveway pad bridges the curb so the car
        /// rolls out smoothly, and VehicleGarageReturn teleports the car back
        /// to this slot when abandoned.
        /// </summary>
        private static void CreateGarage()
        {
            Material wall = GetOrCreateMaterial("M_GarageWall", new Color(0.62f, 0.60f, 0.56f));
            Material floor = GetOrCreateMaterial("M_GarageFloor", new Color(0.30f, 0.31f, 0.33f));
            Material roof = GetOrCreateMaterial("M_GarageRoof", new Color(0.22f, 0.22f, 0.24f));

            GameObject garage = new GameObject("Garage");
            CreatePrimitive("DrivewayPad", PrimitiveType.Cube,
                new Vector3(158.7f, -0.03f, 14f), new Vector3(11f, 0.16f, 8f), floor, garage.transform);
            CreatePrimitive("BackWall", PrimitiveType.Cube,
                new Vector3(163.9f, 1.6f, 14f), new Vector3(0.3f, 3.2f, 6.4f), wall, garage.transform);
            CreatePrimitive("SideWallNorth", PrimitiveType.Cube,
                new Vector3(160.2f, 1.6f, 17.05f), new Vector3(7.4f, 3.2f, 0.3f), wall, garage.transform);
            CreatePrimitive("SideWallSouth", PrimitiveType.Cube,
                new Vector3(160.2f, 1.6f, 10.95f), new Vector3(7.4f, 3.2f, 0.3f), wall, garage.transform);
            CreatePrimitive("DoorHeader", PrimitiveType.Cube,
                new Vector3(156.6f, 2.85f, 14f), new Vector3(0.3f, 0.9f, 6.4f), wall, garage.transform);
            CreatePrimitive("Roof", PrimitiveType.Cube,
                new Vector3(160.2f, 3.35f, 14f), new Vector3(7.8f, 0.3f, 7.0f), roof, garage.transform);
        }

        private static void CreateVehicle(
            Material bodyMaterial,
            Material trimMaterial,
            GameObject preferredVisualPrefab)
        {
            GameObject visualPrefab = preferredVisualPrefab;
            if (visualPrefab == null)
            {
                GameObject visualRoot = new GameObject("PrototypeSedanVisual");
                CreateVisualPrimitive("Body", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f),
                    new Vector3(1.9f, 0.65f, 4.1f), bodyMaterial, visualRoot.transform);
                CreateVisualPrimitive("Cabin", PrimitiveType.Cube, new Vector3(0f, 0.72f, -0.2f),
                    new Vector3(1.55f, 0.65f, 1.9f), trimMaterial, visualRoot.transform);

                Vector3[] wheelPositions =
                {
                    new Vector3(-1f, -0.18f, 1.25f),
                    new Vector3(1f, -0.18f, 1.25f),
                    new Vector3(-1f, -0.18f, -1.25f),
                    new Vector3(1f, -0.18f, -1.25f)
                };
                for (int i = 0; i < wheelPositions.Length; i++)
                {
                    GameObject wheel = CreateVisualPrimitive($"Wheel_{i + 1}", PrimitiveType.Cylinder,
                        wheelPositions[i], new Vector3(0.42f, 0.16f, 0.42f), trimMaterial, visualRoot.transform);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }

                visualPrefab = PrefabUtility.SaveAsPrefabAsset(visualRoot, VehicleVisualPrefabPath);
                Object.DestroyImmediate(visualRoot);
            }

            VehicleDefinition definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(VehicleDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<VehicleDefinition>();
                AssetDatabase.CreateAsset(definition, VehicleDefinitionPath);
            }

            definition.ConfigurePrototype(visualPrefab);
            EditorUtility.SetDirty(definition);

            GameObject vehicle = new GameObject("PrototypeSedan");
            // Parked inside the home garage, nose facing the west opening.
            vehicle.transform.position = new Vector3(160.4f, 0.75f, 14f);
            vehicle.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            BoxCollider vehicleCollider = vehicle.AddComponent<BoxCollider>();
            vehicleCollider.center = new Vector3(0f, 0.15f, 0f);
            // Footprint follows the actual visual (the Kenney sedan is much
            // shorter than the legacy 4.15 m box, which left an invisible
            // bumper when parking). The tuned vertical center/height stay
            // untouched: the controller's ride height depends on them.
            Vector3 vehicleColliderSize = new Vector3(1.95f, 1.25f, 4.15f);
            GameObject measureInstance = visualPrefab != null
                ? PrefabUtility.InstantiatePrefab(visualPrefab) as GameObject
                : null;
            if (measureInstance != null)
            {
                if (EditorBuildUtility.TryGetCombinedBounds(measureInstance, out Bounds visualBounds)
                    && visualBounds.size.x > 0.5f
                    && visualBounds.size.z > 0.5f)
                {
                    vehicleColliderSize.x = visualBounds.size.x;
                    vehicleColliderSize.z = visualBounds.size.z + 0.1f;
                }
                Object.DestroyImmediate(measureInstance);
            }
            vehicleCollider.size = vehicleColliderSize;

            Rigidbody vehicleBody = vehicle.AddComponent<Rigidbody>();
            vehicleBody.mass = definition.Mass;
            vehicleBody.linearDamping = 0.35f;
            vehicleBody.angularDamping = 3f;
            vehicleBody.interpolation = RigidbodyInterpolation.Interpolate;
            vehicleBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            vehicleBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            ArcadeVehicleController controller = vehicle.AddComponent<ArcadeVehicleController>();
            controller.Configure(definition);
            vehicle.AddComponent<VehicleSeat>().Configure(controller);
            vehicle.AddComponent<VehicleGarageReturn>()
                .Configure(new Vector3(160.4f, 0.75f, 14f), -90f);
            PrefabUtility.InstantiatePrefab(visualPrefab, vehicle.transform);
            PrefabUtility.SaveAsPrefabAssetAndConnect(vehicle, VehiclePrefabPath, InteractionMode.AutomatedAction);
        }

        private static void CreateCityLifeSystems()
        {
            TrafficAssetBuilder.Result trafficAssets = TrafficAssetBuilder.EnsureAssets();
            TrafficSignalNetwork signals = Object.FindFirstObjectByType<TrafficSignalNetwork>();

            GameObject trafficObject = new GameObject("TrafficSystem");
            trafficObject.AddComponent<TrafficSystem>().Configure(
                signals,
                trafficAssets.Car2,
                trafficAssets.Car9,
                trafficAssets.Truck,
                trafficAssets.Pod);

            PedestrianDefinition pedestrianDefinition = PedestrianAssetBuilder.EnsureAssets();
            GameObject pedestrianObject = new GameObject("PedestrianSystem");
            pedestrianObject.AddComponent<PedestrianSystem>().Configure(
                pedestrianDefinition,
                signals);

            GameObject developerTools = new GameObject("DeveloperTools");
            developerTools.AddComponent<DebugTools>();
            developerTools.AddComponent<PerformanceHud>();

            Material cloudMaterial = CloudTextureBaker.EnsureCloudMaterial();
            GameObject cloudObject = new GameObject("CloudLayer");
            cloudObject.AddComponent<ObliteratusAI.World.CloudLayer>().Configure(cloudMaterial);
        }

        private static GameObject CreateVisualPrimitive(
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Transform parent)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            return visual;
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent);
            gameObject.transform.SetPositionAndRotation(position, Quaternion.identity);
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = new Material(shader)
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == ScenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
