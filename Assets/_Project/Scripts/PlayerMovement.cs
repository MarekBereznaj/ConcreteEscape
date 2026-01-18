using UnityEngine;

namespace _Project.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")] [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpForce = 1.6f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Mouse Look")] [SerializeField]
        private float mouseSensitivity = 120f;

        [SerializeField] private float maxLookAngle = 80f;

        [Header("Crouch")] [SerializeField] private float crouchHeight = 1.2f;
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float cameraCrouchOffset = -0.4f;
        
        [Header("View Mode")] [SerializeField] private bool startInThirdPerson = false;
        [SerializeField] private Vector3 thirdPersonCameraOffset = new Vector3(0f, 1.6f, -3f);
        [SerializeField] private float thirdPersonMouseSensitivity = 90f;


        private CharacterController _characterController;
        private Transform _cameraTransform;

        private float _verticalVelocity;
        private float _cameraPitch;

        private bool _isCrouching;
        private Vector3 _cameraStandPosition;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _cameraTransform = GetComponentInChildren<Camera>().transform;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _cameraStandPosition = _cameraTransform.localPosition;
        }

        private void Update()
        {
            HandleCrouch();
            HandleMovement();
            HandleMouseLook();
        }

        private void HandleMovement()
        {
            var inputX = Input.GetAxis("Horizontal");
            var inputZ = Input.GetAxis("Vertical");

            var move = transform.right * inputX + transform.forward * inputZ;

            if (_characterController.isGrounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                if (Input.GetButtonDown("Jump"))
                    _verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
            }

            _verticalVelocity += gravity * Time.deltaTime;

            var isSprinting = Input.GetKey(KeyCode.LeftShift);
            var currentSpeed = moveSpeed;

            if (_isCrouching)
                currentSpeed = crouchSpeed;
            else if (isSprinting)
                currentSpeed = sprintSpeed;


            var velocity = move * currentSpeed;
            velocity.y = _verticalVelocity;


            _characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -maxLookAngle, maxLookAngle);

            _cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleCrouch()
        {
            if (!_characterController.isGrounded)
                return;

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

        private void StartCrouch()
        {
            _isCrouching = true;

            _characterController.height = crouchHeight;
            _characterController.center = new Vector3(0f, crouchHeight / 2f, 0f);

            _cameraTransform.localPosition = _cameraStandPosition + Vector3.up * cameraCrouchOffset;
        }

        private void StopCrouch()
        {
            _isCrouching = false;

            _characterController.height = standingHeight;
            _characterController.center = new Vector3(0f, standingHeight / 2f, 0f);

            _cameraTransform.localPosition = _cameraStandPosition;
        }

        private bool CanStandUp()
        {
            var checkDistance = standingHeight - crouchHeight;
            var origin = transform.position + Vector3.up * crouchHeight;

            return !Physics.SphereCast(
                origin,
                _characterController.radius,
                Vector3.up,
                out _,
                checkDistance
            );
        }
    }
}