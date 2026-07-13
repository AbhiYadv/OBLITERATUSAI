using UnityEngine;

namespace ObliteratusAI.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float authoredWalkSpeed = 1.55f;
        [SerializeField] private float authoredRunSpeed = 4.4f;
        [SerializeField] private float runSpeedThreshold = 6f;
        [SerializeField] private float minimumPlaybackSpeed = 0.65f;
        [SerializeField] private float maximumPlaybackSpeed = 3.4f;
        [SerializeField] private float response = 12f;

        private static readonly int WalkingParameter = Animator.StringToHash("Walking");
        private static readonly int RunningParameter = Animator.StringToHash("Running");

        private CharacterController controller;
        private float playbackSpeed;
        private bool hasWalkingParameter;
        private bool hasRunningParameter;
        private bool parametersChecked;

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
            parametersChecked = false;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            if (!parametersChecked)
            {
                parametersChecked = true;
                hasWalkingParameter = false;
                hasRunningParameter = false;
                if (animator.runtimeAnimatorController != null)
                {
                    foreach (AnimatorControllerParameter parameter in animator.parameters)
                    {
                        if (parameter.nameHash == WalkingParameter)
                        {
                            hasWalkingParameter = true;
                        }
                        else if (parameter.nameHash == RunningParameter)
                        {
                            hasRunningParameter = true;
                        }
                    }
                }
            }

            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            float movementSpeed = velocity.magnitude;
            bool moving = movementSpeed > 0.1f;

            if (hasWalkingParameter)
            {
                // Idle/Walk/Run controller: the idle clip plays at its
                // authored pace while standing; moving scales foot speed to
                // match actual movement against the active gait's authored
                // pace. Sprint speed flips to the run clip.
                bool running = hasRunningParameter && movementSpeed > runSpeedThreshold;
                animator.SetBool(WalkingParameter, moving);
                if (hasRunningParameter)
                {
                    animator.SetBool(RunningParameter, running);
                }
                float authoredPace = running ? authoredRunSpeed : authoredWalkSpeed;
                animator.speed = moving
                    ? Mathf.Clamp(movementSpeed / authoredPace, minimumPlaybackSpeed, maximumPlaybackSpeed)
                    : 1f;
                return;
            }

            // Legacy single-clip controller: freeze the walk pose while idle.
            float targetPlaybackSpeed = moving
                ? Mathf.Clamp(movementSpeed / authoredWalkSpeed, minimumPlaybackSpeed, maximumPlaybackSpeed)
                : 0f;

            float blend = 1f - Mathf.Exp(-response * Time.deltaTime);
            playbackSpeed = Mathf.Lerp(playbackSpeed, targetPlaybackSpeed, blend);
            if (targetPlaybackSpeed == 0f && playbackSpeed < 0.02f)
            {
                playbackSpeed = 0f;
            }

            animator.speed = playbackSpeed;
        }
    }
}
