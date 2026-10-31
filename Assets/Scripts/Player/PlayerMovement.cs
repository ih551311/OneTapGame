using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Player
{
    // 기획서 2.3의 플레이어 상태. 지금은 Idle, Move, Jump, Dodge만 쓰고 나머지는 이후 Phase에서 쓴다.
    public enum PlayerState { Idle, Move, Jump, Dodge, Attack, Interact, Dead, Drawing }

    // WASD 이동, Shift 달리기, Space 점프, Q 회피.
    // 이동 방향은 카메라 기준이다 (W = 카메라가 보는 쪽).
    // 붙일 곳: Player (CharacterController, ThirdPersonCamera와 같은 오브젝트)
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("이동 (기본값)")]
        [SerializeField] float walkSpeed = 5.05f;  // m/s (2026-10-31 사용자 요청으로 4.5에서 올림)
        [SerializeField] float runSpeed = 7.85f;   // m/s (7에서 올림)
        [SerializeField] float turnSpeed = 720f;   // 몸이 이동 방향으로 도는 속도 (초당 각도)

        [Header("점프 (기본값)")]
        [SerializeField] float jumpHeight = 1.5f;  // m
        [SerializeField] float gravity = -20f;     // m/s². 현실(-9.81)보다 세게 해서 덜 둥둥 뜨게 한다

        [Header("회피 (기본값)")]
        [SerializeField] float dodgeDistance = 3f;         // m
        [SerializeField] float dodgeDuration = 0.25f;      // 3m를 0.25초 동안 이동
        [SerializeField] float dodgeCooldown = 1.2f;       // 초
        [SerializeField] float dodgeInvincibleTime = 0.3f; // 초

        CharacterController controller;
        ThirdPersonCamera look;

        float verticalVelocity;   // 위아래 속도 (점프, 중력)
        Vector3 dodgeDirection;
        float dodgeEndTime;
        float invincibleEndTime;
        float nextDodgeTime;

        public PlayerState State { get; private set; } = PlayerState.Idle;

        // 움직임 소리(MovementSounds)가 듣는 알림 (2026-10-31)
        public event System.Action Jumped;          // 점프로 바닥에서 뛰어오른 순간
        public event System.Action Dodged;          // 회피(Q)를 시작한 순간
        public event System.Action<float> Landed;   // 공중에서 바닥에 닿은 순간 (떨어지던 속도 m/s)

        // 지금 달리는 중인지 (Shift를 누른 채 땅 위에서 이동)
        public bool IsRunning { get; private set; }

        // 한 칸 이상의 높이에서 떨어졌을 때만 착지로 친다 (살짝 턱을 넘을 때 소리가 나지 않게)
        const float MinLandingSpeed = 3f;
        bool wasGrounded = true;
        float lastFallSpeed;

        // 회피 무적 중인지. 나중에 PlayerHealth가 피해를 받을지 정할 때 쓴다.
        public bool IsInvincible => Time.time < invincibleEndTime;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            look = GetComponent<ThirdPersonCamera>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            Vector3 moveDirection = GetMoveDirection(keyboard);
            bool grounded = controller.isGrounded;

            // 회피: 누르고 있는 방향으로, 아무 키도 안 누르면 몸이 보는 방향으로
            if (keyboard.qKey.wasPressedThisFrame && Time.time >= nextDodgeTime && State != PlayerState.Dodge)
            {
                dodgeDirection = moveDirection != Vector3.zero ? moveDirection : transform.forward;
                dodgeEndTime = Time.time + dodgeDuration;
                invincibleEndTime = Time.time + dodgeInvincibleTime;
                nextDodgeTime = Time.time + dodgeCooldown;
                State = PlayerState.Dodge;
                Dodged?.Invoke();
            }

            // 점프와 중력
            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -2f; // 바닥에 붙어 있게 살짝 눌러 준다
            if (grounded && keyboard.spaceKey.wasPressedThisFrame && State != PlayerState.Dodge)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // 1.5m 높이에 필요한 처음 속도
                Jumped?.Invoke();
            }
            verticalVelocity += gravity * Time.deltaTime;
            lastFallSpeed = Mathf.Max(0f, -verticalVelocity); // 착지 소리 크기 판단용 (움직이기 전 속도)

            // 수평 이동
            Vector3 horizontal;
            if (State == PlayerState.Dodge)
            {
                horizontal = dodgeDirection * (dodgeDistance / dodgeDuration);
                if (Time.time >= dodgeEndTime)
                    State = PlayerState.Idle;
            }
            else
            {
                float speed = keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed;
                horizontal = moveDirection * speed;
            }

            // 몸을 이동 방향으로 돌린다
            if (horizontal.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(horizontal);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

            // 공중에 있다가 바닥에 닿은 순간 = 착지
            bool groundedNow = controller.isGrounded;
            if (groundedNow && !wasGrounded && lastFallSpeed >= MinLandingSpeed)
                Landed?.Invoke(lastFallSpeed);
            wasGrounded = groundedNow;
            IsRunning = groundedNow && State != PlayerState.Dodge && keyboard.leftShiftKey.isPressed && horizontal.sqrMagnitude > 0.01f;

            if (State != PlayerState.Dodge)
            {
                if (!controller.isGrounded)
                    State = PlayerState.Jump;
                else
                    State = horizontal.sqrMagnitude > 0.01f ? PlayerState.Move : PlayerState.Idle;
            }
        }

        // WASD 입력을 카메라 기준 방향으로 바꾼다.
        Vector3 GetMoveDirection(Keyboard keyboard)
        {
            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (input == Vector2.zero)
                return Vector3.zero;

            input.Normalize(); // 대각선이 더 빠르지 않게
            float yaw = look != null ? look.Yaw : transform.eulerAngles.y;
            return Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
        }
    }
}
