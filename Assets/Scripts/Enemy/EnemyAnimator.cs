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

        static readonly int SpeedHash = Animator.StringToHash("Speed");

        Vector3 lastPosition;
        float speed;
        float speedVelocity;

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
        }
    }
}
