using UnityEngine;

namespace LastBullet.Enemy
{
    // 적 캐릭터 모델(스틱맨)의 동작을 정한다 (2026-10-31 사용자 요청: 에셋 Free Pack - Stick Man으로 적 만들기).
    // 서 있으면 대기, 천천히 가면 걷기, 쫓아오면 달리기, 돌진하면 빨리 달리기 — 동작은 "움직이는 속도"로만 정한다.
    // (AI가 위치를 어떻게 옮기든(NavMeshAgent의 이동이든 돌진이든) 실제로 움직인 거리를 재서 정하므로 AI는 건드리지 않는다.)
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (자식 Model에 Animator가 있다)
    public class EnemyAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;

        [Tooltip("속도가 바뀔 때 섞이는 시간 (초)")]
        [SerializeField] float smoothTime = 0.1f;

        [Header("발 박자 맞추기 (2026-11-06 사용자 요청: 적이 어색한 부분 고치기)")]
        [Tooltip("켜면 실제 이동 속도에 맞게 동작 재생 속도를 바꿔 발이 땅에서 미끄러지는 것을 줄인다")]
        [SerializeField] bool matchFeet = true;

        [Tooltip("재생 속도를 이 범위 안으로만 바꾼다 (너무 허둥대거나 굼뜨지 않게)")]
        [SerializeField] Vector2 speedRange = new Vector2(0.5f, 1.6f);

        // 모델 동작의 자연스러운 이동 속도(m/s, 몸 크기 1배 기준, 발 이동 거리로 잼): 걷기 약 1.8, 달리기 약 3.4, 빨리 달리기 약 4.0
        const float WalkNatural = 1.8f, RunNatural = 3.4f, RunFastNatural = 4.0f;
        // 애니메이터의 동작 바뀌는 속도 (EnemyStickman.controller): 걷기 0.3~, 달리기 3.5~, 빨리 달리기 6~
        const float RunThreshold = 3.5f, RunFastThreshold = 6f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");

        Vector3 lastPosition;
        float speed;
        float speedVelocity;
        float playbackSpeed = 1f;
        EnemyHealth health;

        void Start()
        {
            lastPosition = transform.position;
        }

        void Update()
        {
            if (Time.deltaTime <= 0f)
                return;
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            lastPosition = transform.position;
            float target = delta.magnitude / Time.deltaTime;
            speed = Mathf.SmoothDamp(speed, target, ref speedVelocity, smoothTime);
            animator.SetFloat(SpeedHash, speed);

            if (health == null)
                health = GetComponent<EnemyHealth>();
            if (health != null && health.IsDead)
                return; // 처치되면 EnemyDeathEffect가 동작을 멈춘다 (재생 속도를 다시 건드리지 않는다)

            if (matchFeet)
            {
                // 지금 재생 중인 동작의 자연스러운 속도(몸이 커지면 그만큼 커진다)와 실제 속도의 비율로 재생 속도를 정한다
                float scale = Mathf.Max(0.1f, transform.lossyScale.x);
                float natural = speed >= RunFastThreshold ? RunFastNatural : speed >= RunThreshold ? RunNatural : WalkNatural;
                float wanted = speed < 0.3f ? 1f : Mathf.Clamp(speed / (natural * scale), speedRange.x, speedRange.y);
                playbackSpeed = Mathf.Lerp(playbackSpeed, wanted, 1f - Mathf.Exp(-8f * Time.deltaTime));
                animator.speed = playbackSpeed;
            }
        }
    }
}
