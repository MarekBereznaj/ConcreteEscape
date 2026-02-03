using UnityEngine;

namespace _Project.Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;

        [Header("Smoothing")]
        [SerializeField] private float damp = 0.06f;

        private int _speedHash, _groundedHash, _jumpHash, _crouchHash;
        private int _moveXHash, _moveZHash;

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();

            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);

            _speedHash = Animator.StringToHash("Speed");
            _groundedHash = Animator.StringToHash("IsGrounded");
            _jumpHash = Animator.StringToHash("Jump");
            _crouchHash = Animator.StringToHash("Crouch");

            _moveXHash = Animator.StringToHash("MoveX");
            _moveZHash = Animator.StringToHash("MoveZ");
        }

        private void OnEnable()
        {
            if (movement != null) movement.OnJump += HandleJump;
        }

        private void OnDisable()
        {
            if (movement != null) movement.OnJump -= HandleJump;
        }

        private void Update()
        {
            if (animator == null || movement == null) return;

            // reálná rychlost -> Speed 0..1
            Vector3 v = movement.Velocity;
            v.y = 0f;

            float maxSpeed = movement.CurrentMaxSpeed;
            if (maxSpeed < 0.01f) maxSpeed = 1f;

            float speed01 = Mathf.Clamp01(v.magnitude / maxSpeed);
            if (speed01 < 0.03f) speed01 = 0f;

            animator.SetFloat(_speedHash, speed01, damp, Time.deltaTime);

            // vstup (směr) -> 2D BlendTree
            float ix = Input.GetAxisRaw("Horizontal");
            float iz = Input.GetAxisRaw("Vertical");

            Vector2 dir = new Vector2(ix, iz);
            dir = Vector2.ClampMagnitude(dir, 1f);

            // klíč: směr * speed => žádné "strafe když stojíš"
            animator.SetFloat(_moveXHash, dir.x * speed01, damp, Time.deltaTime);
            animator.SetFloat(_moveZHash, dir.y * speed01, damp, Time.deltaTime);

            animator.SetBool(_groundedHash, movement.IsGrounded);
            animator.SetBool(_crouchHash, movement.IsCrouching);
        }

        private void HandleJump()
        {
            if (animator == null) return;
            animator.SetTrigger(_jumpHash);
        }
    }
}
