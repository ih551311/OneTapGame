using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Player
{
    // 주인공 캐릭터 모델의 애니메이션을 정한다 (2026-10-31 사용자 요청: 캡슐 대신 JellyFishGirl을 주인공으로).
    //  - 가만히 있으면 대기, 걸으면 걷기, 달리면 달리기 (이동 속도로 정한다. 부드럽게 섞인다)
    //  - 사격하면 사격 동작, 맞으면 피격 동작 (한 번 재생하고 돌아온다)
    //  - 죽으면 동작을 멈춘다 (쓰러지는 것은 PlayerHealth가 몸을 눕힌다)
    // 모델에 점프·회피 동작이 없어서 그때는 지금 동작이 이어진다.
    // 붙일 곳: Player (모델은 자식 Model, Animator는 모델에 있다)
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] PlayerMovement movement;
        [SerializeField] CharacterController controller;
        [SerializeField] PlayerHealth health;
        [SerializeField] OneBulletGun gun;

        [Tooltip("이 속도(m/s)를 넘으면 달리기 동작. 걷기 5.05, 달리기 7.85의 가운데쯤")]
        [SerializeField] float smoothTime = 0.08f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int ShootHash = Animator.StringToHash("Shoot");
        static readonly int HitHash = Animator.StringToHash("Hit");

        float speed;
        float speedVelocity;

        void OnEnable()
        {
            if (gun != null)
                gun.Fired += OnFired;
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (gun != null)
                gun.Fired -= OnFired;
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        void Update()
        {
            // 이동이 막혀 있으면(팝업·일시정지·사망) 속도 0. 그 외에는 땅 위(수평) 속도.
            float target = 0f;
            if (movement.enabled && movement.State != PlayerState.Idle)
            {
                var velocity = controller.velocity;
                velocity.y = 0f;
                target = velocity.magnitude;
            }
            speed = Mathf.SmoothDamp(speed, target, ref speedVelocity, smoothTime);
            animator.SetFloat(SpeedHash, speed);
        }

        void OnFired()
        {
            animator.SetTrigger(ShootHash);
        }

        void OnDamaged(int damage)
        {
            animator.SetTrigger(HitHash);
        }

        void OnDied()
        {
            animator.speed = 0f; // 쓰러진 자세 그대로 멈춘다
        }
    }
}
