using System.Collections;
using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Player
{
    // 주인공 캐릭터 모델의 애니메이션을 정한다 (2026-10-31 사용자 요청: 캡슐 대신 JellyFishGirl을 주인공으로).
    //  - 가만히 있으면 대기, 걸으면 걷기, 달리면 달리기 (이동 속도로 정한다. 부드럽게 섞인다)
    //  - 사격하면 사격 동작, 맞으면 피격 동작 (한 번 재생하고 돌아온다)
    //  - 죽으면 동작을 멈춘다 (쓰러지는 것은 PlayerHealth가 몸을 눕힌다)
    // 모델에 점프·회피 동작이 없어서: 공중에서는 동작을 거의 멈추고(다리가 달리듯 움직이지 않게), 회피(Q)는 몸을 앞으로 한 바퀴 굴려서 보여 준다 (2026-11-06).
    // 붙일 곳: Player (모델은 자식 Model, Animator는 모델에 있다)
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] PlayerMovement movement;
        [SerializeField] CharacterController controller;
        [SerializeField] PlayerHealth health;
        [SerializeField] AutoGun gun;

        [Tooltip("이 속도(m/s)를 넘으면 달리기 동작. 걷기 5.05, 달리기 7.85의 가운데쯤")]
        [SerializeField] float smoothTime = 0.08f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int ShootHash = Animator.StringToHash("Shoot");
        static readonly int HitHash = Animator.StringToHash("Hit");

        float speed;
        float speedVelocity;

        [Header("점프·회피 (2026-11-06 사용자 요청: 플레이어 어색한 부분 고치기)")]
        [Tooltip("공중에서는 다리가 달리듯 움직이지 않게 동작 재생 속도를 이만큼으로 늦춘다 (모델에 점프 동작이 없다)")]
        [SerializeField] float airPlaybackSpeed = 0.15f;

        [Tooltip("회피(Q) 때 몸을 앞으로 한 바퀴 구르는 시간 (초). 0이면 구르지 않는다. 회피 시간과 같게")]
        [SerializeField] float rollTime = 0.25f;

        [Tooltip("구르는 중심 높이 (m, 발 기준). 몸 한가운데쯤")]
        [SerializeField] float rollCenterHeight = 0.95f;

        Coroutine rollRoutine;
        bool dead;

        void OnEnable()
        {
            if (movement != null)
                movement.Dodged += OnDodged;
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
            if (movement != null)
                movement.Dodged -= OnDodged;
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

            // 공중(점프·낙하)에서는 다리가 땅을 달리듯 움직이지 않게 동작을 거의 멈춘다
            if (!dead)
            {
                bool airborne = movement.enabled && movement.State == PlayerState.Jump;
                animator.speed = Mathf.MoveTowards(animator.speed, airborne ? airPlaybackSpeed : 1f, Time.deltaTime * (airborne ? 12f : 8f));
            }
        }

        // 회피(Q): 몸이 앞으로 한 바퀴 구른다 (모델에 회피 동작이 없어서 몸 전체를 굴려서 보여 준다)
        void OnDodged()
        {
            if (rollTime <= 0f || dead || animator == null)
                return;
            if (rollRoutine != null)
                StopCoroutine(rollRoutine);
            rollRoutine = StartCoroutine(Roll());
        }

        IEnumerator Roll()
        {
            Transform model = animator.transform;
            Quaternion baseRotation = model.localRotation;
            Vector3 basePosition = model.localPosition;
            Vector3 center = Vector3.up * rollCenterHeight;
            for (float t = 0f; t < rollTime; t += Time.deltaTime)
            {
                float k = t / rollTime;
                float eased = k * k * (3f - 2f * k);
                Quaternion spin = Quaternion.Euler(360f * eased, 0f, 0f); // 앞으로 구른다 (몸 앞쪽이 아래로)
                Quaternion total = baseRotation * spin;
                model.localRotation = total;
                // 몸 한가운데(center)가 제자리에 있도록 위치를 맞춘다 (발을 축으로 돌면 땅속으로 파묻힌다)
                model.localPosition = basePosition + baseRotation * center - total * center;
                yield return null;
            }
            model.localRotation = baseRotation;
            model.localPosition = basePosition;
            rollRoutine = null;
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
            dead = true;
            animator.speed = 0f; // 쓰러진 자세 그대로 멈춘다
        }
    }
}
