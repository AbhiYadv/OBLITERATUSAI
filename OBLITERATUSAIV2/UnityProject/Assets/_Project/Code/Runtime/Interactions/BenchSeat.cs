using ObliteratusAI.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ObliteratusAI.Interactions
{
    /// <summary>
    /// Sit-on-bench interaction following the VehicleSeat occupancy pattern:
    /// entering disables the player's motor, interactor, and animation
    /// driver, parks the player on the seat anchor, and raises the animator's
    /// "Sitting" bool (SitDown holds its final seated pose). E stands up:
    /// Sitting clears, the StandUp clip plays through, and control returns at
    /// the captured pre-sit position.
    /// </summary>
    public sealed class BenchSeat : MonoBehaviour, IInteractable
    {
        private static readonly int SittingParameter = Animator.StringToHash("Sitting");
        private static readonly int WalkingParameter = Animator.StringToHash("Walking");
        private static readonly int RunningParameter = Animator.StringToHash("Running");

        // Placement self-calibrates: the animator is snapped to the SitDown
        // clip's final frame for one hidden evaluation, then the pelvis and
        // knee bone world positions are read back. Height and centering come
        // from the pelvis (landing it on the seat surface); the forward
        // position comes from the knees, slid until the shins clear the
        // seat's front edge instead of dropping through the slab. This stays
        // correct for any rig scale or armature axis convention.
        [SerializeField] private float seatTopLocalHeight = 0.505f;
        [SerializeField] private float pelvisAboveSeat = 0.07f;
        [SerializeField] private float seatFrontLocalEdge = 0.275f;
        [SerializeField] private float kneeClearance = 0.06f;
        [SerializeField] private Vector3 fineTuneOffset = Vector3.zero;
        [SerializeField] private float standUpDuration = 1.05f;
        [SerializeField] private float exitCooldown = 0.5f;

        private GameObject occupant;
        private CharacterController occupantController;
        private PlayerMotor occupantMotor;
        private PlayerInteractor occupantInteractor;
        private PlayerAnimationDriver occupantAnimation;
        private Animator occupantAnimator;
        private Vector3 standPosition;
        private Quaternion standRotation;
        private float satAt;
        private float standTimer;
        private GUIStyle promptStyle;

        public string InteractionPrompt => occupant == null ? "Sit on bench" : "Bench occupied";

        public void Interact(GameObject interactor)
        {
            if (occupant != null || standTimer > 0f || interactor == null)
            {
                return;
            }

            Sit(interactor);
        }

        private void Update()
        {
            if (standTimer > 0f)
            {
                standTimer -= Time.deltaTime;
                if (standTimer <= 0f)
                {
                    ReleaseOccupant();
                }
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (occupant != null
                && keyboard != null
                && Time.time >= satAt + exitCooldown
                && keyboard.eKey.wasPressedThisFrame)
            {
                BeginStandUp();
            }
        }

        private void OnGUI()
        {
            if (occupant == null || standTimer > 0f)
            {
                return;
            }

            promptStyle ??= new GUIStyle(GUI.skin.box)
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
            GUI.Box(rect, "E  Stand up", promptStyle);
        }

        private void Sit(GameObject playerObject)
        {
            occupantController = playerObject.GetComponent<CharacterController>();
            occupantMotor = playerObject.GetComponent<PlayerMotor>();
            occupantInteractor = playerObject.GetComponent<PlayerInteractor>();
            occupantAnimation = playerObject.GetComponent<PlayerAnimationDriver>();
            occupantAnimator = playerObject.GetComponentInChildren<Animator>(true);
            if (occupantController == null || occupantMotor == null || occupantInteractor == null)
            {
                Debug.LogWarning("Bench seating requires a player CharacterController, PlayerMotor, and PlayerInteractor.");
                return;
            }
            if (occupantAnimator == null || !HasParameter(occupantAnimator, SittingParameter))
            {
                // The active character rig has no seat animations (legacy
                // fallback controller); refuse rather than freeze standing
                // on the bench.
                Debug.LogWarning("Bench seating requires a character with SitDown/StandUp animations.");
                return;
            }

            occupant = playerObject;
            standPosition = occupant.transform.position;
            standRotation = occupant.transform.rotation;

            occupantMotor.enabled = false;
            occupantInteractor.enabled = false;
            if (occupantAnimation != null)
            {
                occupantAnimation.enabled = false;
            }
            occupantController.enabled = false;

            // Rough start: player origin (1 m above the feet) over the seat
            // front at bench height, facing the bench's forward direction.
            occupant.transform.SetPositionAndRotation(
                transform.TransformPoint(new Vector3(0f, 1f, 0.3f)),
                transform.rotation);

            occupantAnimator.SetBool(WalkingParameter, false);
            if (HasParameter(occupantAnimator, RunningParameter))
            {
                occupantAnimator.SetBool(RunningParameter, false);
            }
            occupantAnimator.speed = 1f;
            occupantAnimator.SetBool(SittingParameter, true);

            // Self-calibrate: evaluate the final seated frame once (not
            // rendered), read the pelvis and knee bones, and shift the
            // player so the pelvis rests on the seat while the knees clear
            // its front edge. Then rewind and let the sit-down play
            // normally.
            Transform pelvis = FindBone(occupantAnimator.transform, "Body", "Hips", "Pelvis");
            Transform kneeLeft = FindBone(occupantAnimator.transform, "LowerLeg.L", "LeftLeg", "Knee.L");
            Transform kneeRight = FindBone(occupantAnimator.transform, "LowerLeg.R", "RightLeg", "Knee.R");
            occupantAnimator.Play("SitDown", 0, 1f);
            occupantAnimator.Update(0f);
            if (pelvis != null)
            {
                Vector3 pelvisLocal = transform.InverseTransformPoint(pelvis.position);
                Vector3 delta;
                delta.x = 0f - pelvisLocal.x;
                delta.y = seatTopLocalHeight + pelvisAboveSeat - pelvisLocal.y;
                if (kneeLeft != null || kneeRight != null)
                {
                    Transform kneeA = kneeLeft != null ? kneeLeft : kneeRight;
                    Transform kneeB = kneeRight != null ? kneeRight : kneeLeft;
                    float kneeZ = Mathf.Min(
                        transform.InverseTransformPoint(kneeA.position).z,
                        transform.InverseTransformPoint(kneeB.position).z);
                    delta.z = seatFrontLocalEdge + kneeClearance - kneeZ;
                }
                else
                {
                    delta.z = 0.1f - pelvisLocal.z;
                }
                occupant.transform.position += transform.TransformVector(delta + fineTuneOffset);
            }
            occupantAnimator.Play("SitDown", 0, 0f);
            occupantAnimator.Update(0f);
            satAt = Time.time;
        }

        private void BeginStandUp()
        {
            occupantAnimator.SetBool(SittingParameter, false);
            standTimer = standUpDuration;
        }

        private void ReleaseOccupant()
        {
            occupant.transform.SetPositionAndRotation(standPosition, standRotation);
            occupantController.enabled = true;
            occupantMotor.enabled = true;
            occupantInteractor.enabled = true;
            if (occupantAnimation != null)
            {
                occupantAnimation.enabled = true;
            }

            occupant = null;
            occupantController = null;
            occupantMotor = null;
            occupantInteractor = null;
            occupantAnimation = null;
            occupantAnimator = null;
        }

        /// <summary>
        /// Bone lookup by preferred names. The Quaternius rigs use "Body"
        /// for the pelvis (with the spine chain as children) and
        /// "LowerLeg.L/R" for the knee joints; the extra names cover common
        /// rig conventions.
        /// </summary>
        private static Transform FindBone(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name && (name != "Body" || child.childCount > 0))
                    {
                        return child;
                    }
                }
            }
            return null;
        }

        private static bool HasParameter(Animator animator, int nameHash)
        {
            if (animator.runtimeAnimatorController == null)
            {
                return false;
            }
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == nameHash)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
