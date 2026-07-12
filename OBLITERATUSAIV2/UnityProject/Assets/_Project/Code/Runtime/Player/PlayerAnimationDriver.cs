using UnityEngine;

namespace ObliteratusAI.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float authoredWalkSpeed = 1.55f;
        [SerializeField] private float minimumPlaybackSpeed = 0.65f;
        [SerializeField] private float maximumPlaybackSpeed = 3.4f;
        [SerializeField] private float response = 12f;

        private CharacterController controller;
        private float playbackSpeed;

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
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

            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            float movementSpeed = velocity.magnitude;
            float targetPlaybackSpeed = movementSpeed > 0.1f
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
