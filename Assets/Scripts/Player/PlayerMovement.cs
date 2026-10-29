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
        [SerializeField] float walkSpeed = 4.5f;   // m/s
        [SerializeField] float runSpeed = 7f;      // m/s
        [SerializeField] float turnSpeed = 720f;   // 몸이 이동 방향으로 도는 속도 (초당 각도)

        [Header("점프 (기본값)")]
        [SerializeField] float jumpHeight = 1.5f;  // m
        [SerializeField] float gravity = -20f;     // m/s². 현실(-9.81)보다 세게 해서 덜 둥둥 뜨게 한다

        [Header("회피 (기본값)")]
        [SerializeField] float dodgeDistance = 3f;         // m
        [SerializeField] float dodgeDuration = 0.25f;      // 3m를 0.25초 동안 이동
        [SerializeField] float dodgeCooldown = 1.2f;       // 초
        [SerializeField] float dodgeInvincibleTime = 0.3f; // 초

        [Header("테스트")]
        [Tooltip("재생 중 화면 왼쪽 위에 상태와 속도를 표시한다")]
        [SerializeField] bool showDebug = true;

        CharacterController controller;
        ThirdPersonCamera look;

        float verticalVelocity;   // 위아래 속도 (점프, 중력)
        Vector3 dodgeDirection;
        float dodgeEndTime;
        float invincibleEndTime;
        float nextDodgeTime;
        float currentSpeed;       // 테스트 표시용 수평 속도

        public PlayerState State { get; private set; } = PlayerState.Idle;

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
            }

            // 점프와 중력
            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -2f; // 바닥에 붙어 있게 살짝 눌러 준다
            if (grounded && keyboard.spaceKey.wasPressedThisFrame && State != PlayerState.Dodge)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // 1.5m 높이에 필요한 처음 속도
            verticalVelocity += gravity * Time.deltaTime;

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
            currentSpeed = horizontal.magnitude;

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

        void OnGUI()
        {
            if (!showDebug)
                return;
            float cooldown = Mathf.Max(0f, nextDodgeTime - Time.time);
            // 화면 해상도가 커도 읽을 수 있게 글자 크기를 화면 높이에 맞춘다.
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 30) };
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(10, 10, Screen.width, Screen.height / 4f),
                $"상태: {State}\n속도: {currentSpeed:0.0} m/s   높이: {transform.position.y:0.00} m\n" +
                $"무적: {(IsInvincible ? "예" : "아니오")}   회피 쿨타임: {cooldown:0.0}초", style);
        }
    }
}
