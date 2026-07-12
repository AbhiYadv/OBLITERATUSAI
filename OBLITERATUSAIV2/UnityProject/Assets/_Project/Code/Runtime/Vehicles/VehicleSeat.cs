using ObliteratusAI.CameraSystem;
using ObliteratusAI.Interactions;
using ObliteratusAI.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ObliteratusAI.Vehicles
{
    [RequireComponent(typeof(ArcadeVehicleController))]
    public sealed class VehicleSeat : MonoBehaviour, IInteractable
    {
        private static readonly Vector3[] ExitOffsets =
        {
            new Vector3(1.8f, 0f, 0f),
            new Vector3(-1.8f, 0f, 0f),
            new Vector3(0f, 0f, -2.8f),
            new Vector3(0f, 0f, 2.8f)
        };

        [SerializeField] private ArcadeVehicleController vehicleController;
        [SerializeField] private float exitCooldown = 0.4f;

        private GameObject occupant;
        private CharacterController occupantController;
        private PlayerMotor occupantMotor;
        private PlayerInteractor occupantInteractor;
        private PlayerAnimationDriver occupantAnimation;
        private Renderer[] occupantRenderers;
        private bool[] occupantRendererStates;
        private ThirdPersonCamera followCamera;
        private float enteredAt;
        private GUIStyle exitPromptStyle;

        public string InteractionPrompt => occupant == null ? "Sit in vehicle" : "Vehicle occupied";

        public bool IsOccupied => occupant != null;

        public void Configure(ArcadeVehicleController controller)
        {
            vehicleController = controller;
        }

        public void Interact(GameObject interactor)
        {
            if (occupant != null || interactor == null)
            {
                return;
            }

            EnterVehicle(interactor);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (occupant != null &&
                keyboard != null &&
                Time.time >= enteredAt + exitCooldown &&
                keyboard.eKey.wasPressedThisFrame)
            {
                ExitVehicle();
            }
        }

        private void OnGUI()
        {
            if (occupant == null)
            {
                return;
            }

            exitPromptStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            const float width = 220f;
            const float height = 38f;
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height * 0.82f,
                width,
                height);
            GUI.Box(rect, "E  Exit vehicle", exitPromptStyle);
        }

        private void EnterVehicle(GameObject playerObject)
        {
            occupantController = playerObject.GetComponent<CharacterController>();
            occupantMotor = playerObject.GetComponent<PlayerMotor>();
            occupantInteractor = playerObject.GetComponent<PlayerInteractor>();
            occupantAnimation = playerObject.GetComponent<PlayerAnimationDriver>();
            if (occupantController == null || occupantMotor == null || occupantInteractor == null)
            {
                Debug.LogWarning("Vehicle entry requires a player CharacterController, PlayerMotor, and PlayerInteractor.");
                return;
            }

            occupant = playerObject;
            occupantRenderers = occupant.GetComponentsInChildren<Renderer>(true);
            occupantRendererStates = new bool[occupantRenderers.Length];
            for (int i = 0; i < occupantRenderers.Length; i++)
            {
                occupantRendererStates[i] = occupantRenderers[i].enabled;
                occupantRenderers[i].enabled = false;
            }

            occupantMotor.enabled = false;
            occupantInteractor.enabled = false;
            if (occupantAnimation != null)
            {
                occupantAnimation.enabled = false;
            }

            occupantController.enabled = false;
            occupant.transform.SetParent(transform, false);
            occupant.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.35f, 0f), Quaternion.identity);

            followCamera = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.GetComponent<ThirdPersonCamera>()
                : null;
            followCamera?.SetTarget(transform, new Vector3(0f, 1.4f, 0f), 6.5f);
            vehicleController.SetControlsEnabled(true);
            enteredAt = Time.time;
        }

        private void ExitVehicle()
        {
            if (!TryFindExitPosition(out Vector3 exitPosition))
            {
                Debug.LogWarning("No safe vehicle exit position is currently available.");
                return;
            }

            vehicleController.SetControlsEnabled(false);
            occupant.transform.SetParent(null, true);
            occupant.transform.SetPositionAndRotation(exitPosition, transform.rotation);
            occupantController.enabled = true;
            occupantMotor.enabled = true;
            occupantInteractor.enabled = true;
            if (occupantAnimation != null)
            {
                occupantAnimation.enabled = true;
            }

            for (int i = 0; i < occupantRenderers.Length; i++)
            {
                occupantRenderers[i].enabled = occupantRendererStates[i];
            }

            followCamera?.SetTarget(occupant.transform, new Vector3(0f, 1.45f, 0f), 5f);
            occupant = null;
            occupantController = null;
            occupantMotor = null;
            occupantInteractor = null;
            occupantAnimation = null;
            occupantRenderers = null;
            occupantRendererStates = null;
        }

        private bool TryFindExitPosition(out Vector3 exitPosition)
        {
            foreach (Vector3 localOffset in ExitOffsets)
            {
                Vector3 candidate = transform.TransformPoint(localOffset);
                if (Physics.Raycast(candidate + Vector3.up * 2f, Vector3.down, out RaycastHit groundHit, 5f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    candidate.y = groundHit.point.y + 1.05f;
                }

                if (IsCapsuleClear(candidate))
                {
                    exitPosition = candidate;
                    return true;
                }
            }

            exitPosition = default;
            return false;
        }

        private bool IsCapsuleClear(Vector3 center)
        {
            float radius = occupantController.radius;
            float halfSegment = Mathf.Max(0f, occupantController.height * 0.5f - radius);
            Vector3 top = center + Vector3.up * halfSegment;
            Vector3 bottom = center - Vector3.up * halfSegment;
            Collider[] overlaps = Physics.OverlapCapsule(
                bottom,
                top,
                radius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (!overlap.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
