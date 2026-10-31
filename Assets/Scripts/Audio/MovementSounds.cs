using LastBullet.Player;
using UnityEngine;

namespace LastBullet.Audio
{
    // 플레이어 움직임 소리 (2026-10-31 사용자 요청): 걸을 때·달릴 때 발소리, 점프, 착지, 회피.
    // 발소리는 "시간"이 아니라 "걸은 거리"로 낸다: 걸을수록 한 걸음마다, 멈추면 바로 멈추고, 달리면 걸음이 더 빠르고 크다.
    // 점프·착지·회피는 PlayerMovement의 알림(Jumped, Landed, Dodged)에 맞춰 낸다.
    // 일시정지·팝업·사망으로 이동이 막히면(PlayerMovement가 꺼지면) 소리도 멈춘다.
    // 붙일 곳: Player 아래의 MovementSfx (AudioSource와 SfxPlayer 5개와 같이. 메뉴 LastBullet > Phase 23 움직임 소리 넣기)
    public class MovementSounds : MonoBehaviour
    {
        [SerializeField] PlayerMovement movement;
        [SerializeField] CharacterController controller;

        [SerializeField] SfxPlayer walkStep;
        [SerializeField] SfxPlayer runStep;
        [SerializeField] SfxPlayer jump;
        [SerializeField] SfxPlayer land;
        [SerializeField] SfxPlayer dodge;

        [Header("발소리 간격 (걸은 거리, m)")]
        [SerializeField] float walkStride = 2.0f;
        [SerializeField] float runStride = 2.6f;

        [Header("점프 발구름 (걸을 때 발소리 대비)")]
        [Tooltip("점프할 때 발소리를 몇 배 크게 낼지")]
        [SerializeField] float pushOffVolume = 1.5f;
        [Tooltip("점프할 때 발소리의 음높이 배율 (1보다 작으면 낮고 묵직하다)")]
        [SerializeField] float pushOffPitch = 0.85f;

        float distance; // 마지막 발소리 뒤로 걸은 거리

        void OnEnable()
        {
            movement.Jumped += OnJumped;
            movement.Dodged += OnDodged;
            movement.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (movement == null)
                return;
            movement.Jumped -= OnJumped;
            movement.Dodged -= OnDodged;
            movement.Landed -= OnLanded;
        }

        void Update()
        {
            if (!movement.enabled || movement.State != PlayerState.Move)
            {
                distance = 0f; // 서 있다가 다시 걸으면 바로 첫 걸음이 나지 않고 한 걸음 거리만큼 걸은 뒤에 난다
                return;
            }

            var velocity = controller.velocity;
            velocity.y = 0f;
            distance += velocity.magnitude * Time.deltaTime;

            bool running = movement.IsRunning;
            if (distance >= (running ? runStride : walkStride))
            {
                distance = 0f;
                (running ? runStep : walkStep).Play();
            }
        }

        // 점프 (2026-10-31 사용자 결정: 땅을 차는 소리): 지금 밟고 있는 바닥의 발소리(풀·모래·나무)를 더 크고 낮게 한 번 내고(발구름),
        // 그 위에 옷자락 스치는 짧은 소리(jump)를 깐다. 바닥이 바뀌면 발구름 소리도 같이 바뀐다.
        void OnJumped()
        {
            walkStep.Play(pushOffVolume, pushOffPitch);
            if (jump != null)
                jump.Play();
            distance = 0f;
        }

        void OnDodged()
        {
            dodge.Play();
        }

        void OnLanded(float fallSpeed)
        {
            land.Play();
            distance = 0f;
        }
    }
}
