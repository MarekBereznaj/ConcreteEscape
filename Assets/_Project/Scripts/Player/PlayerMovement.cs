using System;
using UnityEngine;

namespace _Project.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 50f;
        [SerializeField] private float sprintSpeed = 80f;
        [SerializeField] private float jumpForce = 10.6f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 120f;
        [SerializeField] private float maxLookAngle = 80f;

        [Header("Crouch")]
        [SerializeField] private float crouchHeight = 1.2f;
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float cameraCrouchOffset = -0.4f;

        [Header("View Mode")]
        [SerializeField] private bool startInThirdPerson = false;
        [SerializeField] private Vector3 thirdPersonCameraOffset = new Vector3(0f, 1.6f, -3f);
        [SerializeField] private float thirdPersonMouseSensitivity = 90f;

        [Header("Landing Effect")]
        [SerializeField] private float landingDipAmount = 0.15f;
        [SerializeField] private float landingDipDuration = 0.2f;
        [SerializeField] private float minFallSpeedForEffect = -5f;
        [SerializeField] private AnimationCurve landingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private CharacterController _characterController;
        private Transform _cameraTransform;

        private float _verticalVelocity;
        private float _cameraPitch;
        private bool _isThirdPerson;

        private bool _isCrouching;
        private Vector3 _cameraStandPosition;

        private bool _wasGrounded;
        private float _landingEffectProgress;
        private bool _isPlayingLandingEffect;
        private float _landingEffectIntensity;

        // ========= Pro animace (čte PlayerAnimationController) =========
        public Vector3 Velocity => _characterController != null ? _characterController.velocity : Vector3.zero;
        public bool IsGrounded => _characterController != null && _characterController.isGrounded;
        public bool IsCrouching => _isCrouching;

        public float CurrentMaxSpeed
        {
            get
            {
                if (_isCrouching) return crouchSpeed;

                float vz = Input.GetAxis("Vertical");
                bool sprint = Input.GetKey(KeyCode.LeftShift) && vz > 0.1f;

                return sprint ? sprintSpeed : moveSpeed;
            }
        }

        public event Action OnJump;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _cameraTransform = GetComponentInChildren<Camera>().transform;

            _cameraStandPosition = _cameraTransform.localPosition;

            _isThirdPerson = startInThirdPerson;
            UpdateCameraView();

            _wasGrounded = true;
        }

        private void Update()
        {
            HandleViewSwitch();
            HandleCrouch();
            HandleMovement();
            HandleMouseLook();
            HandleLandingEffect();
        }

        private void HandleMovement()
        {
            float inputX = Input.GetAxis("Horizontal");
            float inputZ = Input.GetAxis("Vertical");

            Vector3 move = transform.right * inputX + transform.forward * inputZ;
            move = Vector3.ClampMagnitude(move, 1f);

            bool grounded = _characterController.isGrounded;

            if (grounded)
            {
                // Dopad (landing effect)
                if (!_wasGrounded && _verticalVelocity < minFallSpeedForEffect)
                {
                    TriggerLandingEffect();
                }

                // "přilepení" k zemi
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                // Skok
                if (Input.GetButtonDown("Jump") && !_isCrouching)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
                    OnJump?.Invoke();
                }
            }
            else
            {
                // Gravitace
                _verticalVelocity += gravity * Time.deltaTime;
            }

            _wasGrounded = grounded;

            bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputZ > 0.1f && !_isCrouching;

            float currentSpeed = moveSpeed;
            if (_isCrouching)
                currentSpeed = crouchSpeed;
            else if (isSprinting)
                currentSpeed = sprintSpeed;

            Vector3 velocity = move * currentSpeed;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            float currentSensitivity = _isThirdPerson ? thirdPersonMouseSensitivity : mouseSensitivity;

            float mouseX = Input.GetAxis("Mouse X") * currentSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * currentSensitivity * Time.deltaTime;

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -maxLookAngle, maxLookAngle);

            _cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleCrouch()
        {
            bool crouchInput = Input.GetKey(KeyCode.LeftControl);

            if (crouchInput)
            {
                if (!_isCrouching)
                    StartCrouch();
            }
            else
            {
                if (_isCrouching && CanStandUp())
                    StopCrouch();
            }
        }

        private void HandleViewSwitch()
        {
            if (!Input.GetKeyDown(KeyCode.V)) return;

            _isThirdPerson = !_isThirdPerson;
            UpdateCameraView();
        }

        private void TriggerLandingEffect()
        {
            _landingEffectIntensity = Mathf.Clamp01(Mathf.Abs(_verticalVelocity) / 20f);
            _landingEffectProgress = 0f;
            _isPlayingLandingEffect = true;
        }

        private void HandleLandingEffect()
        {
            if (!_isPlayingLandingEffect) return;

            _landingEffectProgress += Time.deltaTime / landingDipDuration;

            if (_landingEffectProgress >= 1f)
            {
                _landingEffectProgress = 1f;
                _isPlayingLandingEffect = false;
            }

            float curveValue = landingCurve.Evaluate(_landingEffectProgress);
            float dipOffset = Mathf.Sin(curveValue * Mathf.PI) * landingDipAmount * _landingEffectIntensity;

            UpdateCameraPosition(-dipOffset);
        }

        private void UpdateCameraPosition(float verticalOffset)
        {
            Vector3 targetPosition;

            if (_isThirdPerson)
            {
                targetPosition = thirdPersonCameraOffset;
            }
            else if (_isCrouching)
            {
                targetPosition = _cameraStandPosition + Vector3.up * cameraCrouchOffset;
            }
            else
            {
                targetPosition = _cameraStandPosition;
            }

            targetPosition += Vector3.up * verticalOffset;
            _cameraTransform.localPosition = targetPosition;
        }

        private void StartCrouch()
        {
            _isCrouching = true;

            _characterController.enabled = false;
            _characterController.height = crouchHeight;
            _characterController.enabled = true;

            _verticalVelocity = -2f;

            if (!_isPlayingLandingEffect)
                _cameraTransform.localPosition = _cameraStandPosition + Vector3.up * cameraCrouchOffset;
        }

        private void StopCrouch()
        {
            _isCrouching = false;

            _characterController.enabled = false;
            _characterController.height = standingHeight;
            _characterController.enabled = true;

            _verticalVelocity = -2f;

            if (_isPlayingLandingEffect) return;
            _cameraTransform.localPosition = _isThirdPerson ? thirdPersonCameraOffset : _cameraStandPosition;
        }

        private bool CanStandUp()
        {
            Vector3 rayStart = transform.position + Vector3.up * (_characterController.height - 0.1f);
            float checkHeight = standingHeight - crouchHeight + 0.2f;

            return !Physics.Raycast(rayStart, Vector3.up, checkHeight, ~0, QueryTriggerInteraction.Ignore);
        }

        private void UpdateCameraView()
        {
            if (_isPlayingLandingEffect) return;

            if (_isThirdPerson)
            {
                _cameraTransform.localPosition = thirdPersonCameraOffset;
            }
            else
            {
                _cameraTransform.localPosition = _isCrouching
                    ? _cameraStandPosition + Vector3.up * cameraCrouchOffset
                    : _cameraStandPosition;
            }
        }
    }
}
