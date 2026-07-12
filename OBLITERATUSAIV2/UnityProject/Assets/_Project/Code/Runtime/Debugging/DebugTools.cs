using ObliteratusAI.CameraSystem;
using ObliteratusAI.Interactions;
using ObliteratusAI.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ObliteratusAI.Debugging
{
    /// <summary>
    /// Development helpers ported from the legacy ThirdPersonController:
    /// F toggles a free-fly debug camera (WASD + mouse, E/Q up/down, Shift
    /// fast), R teleports the player back to spawn. Both are ignored while
    /// driving. Purely additive — while inactive nothing is touched.
    /// </summary>
    public sealed class DebugTools : MonoBehaviour
    {
        [SerializeField] private float flySpeed = 14f;
        [SerializeField] private float flyFastMultiplier = 4f;
        [SerializeField] private float lookSensitivity = 0.12f;

        private Camera _camera;
        private ThirdPersonCamera _followCamera;
        private PlayerMotor _motor;
        private PlayerInteractor _interactor;
        private CharacterController _controller;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _spawnCaptured;
        private bool _flying;
        private float _flyYaw;
        private float _flyPitch;

        public bool IsFlying => _flying;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            if (!TryResolveReferences())
            {
                return;
            }

            // The motor is disabled while seated in a vehicle; both debug
            // actions are unavailable there, matching the legacy controller.
            bool onFoot = _motor != null && _motor.enabled;

            if (keyboard.rKey.wasPressedThisFrame && onFoot && !_flying && _spawnCaptured)
            {
                TeleportToSpawn();
            }

            if (keyboard.fKey.wasPressedThisFrame && (onFoot || _flying))
            {
                if (_flying)
                {
                    EndFly();
                }
                else
                {
                    BeginFly();
                }
            }

            if (_flying)
            {
                TickFly(keyboard);
            }
        }

        private bool TryResolveReferences()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera != null)
                {
                    _followCamera = _camera.GetComponent<ThirdPersonCamera>();
                }
            }
            if (_motor == null)
            {
                _motor = FindFirstObjectByType<PlayerMotor>();
                if (_motor != null)
                {
                    _interactor = _motor.GetComponent<PlayerInteractor>();
                    _controller = _motor.GetComponent<CharacterController>();
                    _spawnPosition = _motor.transform.position;
                    _spawnRotation = _motor.transform.rotation;
                    _spawnCaptured = true;
                }
            }
            return _camera != null && _motor != null;
        }

        private void TeleportToSpawn()
        {
            _motor.ResetMotion();
            if (_controller != null)
            {
                _controller.enabled = false;
            }
            _motor.transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            if (_controller != null)
            {
                _controller.enabled = true;
            }
        }

        private void BeginFly()
        {
            _flying = true;
            Vector3 euler = _camera.transform.rotation.eulerAngles;
            _flyYaw = euler.y;
            _flyPitch = euler.x > 180f ? euler.x - 360f : euler.x;
            if (_followCamera != null)
            {
                _followCamera.enabled = false;
            }
            _motor.enabled = false;
            if (_interactor != null)
            {
                _interactor.enabled = false;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void EndFly()
        {
            _flying = false;
            if (_motor != null)
            {
                _motor.enabled = true;
            }
            if (_interactor != null)
            {
                _interactor.enabled = true;
            }
            if (_followCamera != null)
            {
                _followCamera.AdoptCurrentRotation();
                _followCamera.enabled = true;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void TickFly(Keyboard keyboard)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
                _flyYaw += look.x;
                _flyPitch = Mathf.Clamp(_flyPitch - look.y, -89f, 89f);
            }
            Quaternion rotation = Quaternion.Euler(_flyPitch, _flyYaw, 0f);

            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                move += rotation * Vector3.forward;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                move -= rotation * Vector3.forward;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                move += rotation * Vector3.right;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                move -= rotation * Vector3.right;
            }
            if (keyboard.eKey.isPressed || keyboard.spaceKey.isPressed)
            {
                move += Vector3.up;
            }
            if (keyboard.qKey.isPressed || keyboard.cKey.isPressed)
            {
                move -= Vector3.up;
            }

            bool fast = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            float speed = flySpeed * (fast ? flyFastMultiplier : 1f);
            _camera.transform.SetPositionAndRotation(
                _camera.transform.position + move.normalized * (speed * Time.deltaTime),
                rotation);
        }

        private void OnDisable()
        {
            if (_flying)
            {
                EndFly();
            }
        }
    }
}
