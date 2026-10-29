using System;
using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Enemy
{
    // 적의 처치 (기획서 2.4, 2.5 — 2026-10-30 변경: 헤드샷 원탭만).
    // 머리에 맞으면 즉사. 몸에 맞으면 피해는 없고, 빨갛게 깜빡이며 잠깐 멈칫한다.
    // 처치는 한 번만 일어나고, 그때 Died 이벤트를 보낸다 (Phase 5에서 드롭이 여기에 연결된다).
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (Hitbox는 자식 콜라이더에)
    public class EnemyHealth : MonoBehaviour
    {
        [Tooltip("맞았을 때 잠깐 빨갛게 보이는 시간 (초)")]
        [SerializeField] float hitFlashTime = 0.1f;

        public bool IsDead { get; private set; }

        // 처치되는 순간 한 번 불린다.
        public event Action<EnemyHealth> Died;

        // 몸에 맞았을 때 불린다. EnemyAI가 잠깐 멈칫(Hit 상태)하는 데 쓴다.
        public event Action<EnemyHealth> Damaged;

        Renderer[] renderers;
        MaterialPropertyBlock flashBlock;
        float flashEndTime;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            flashBlock = new MaterialPropertyBlock();
            flashBlock.SetColor("_BaseColor", new Color(1f, 0.3f, 0.3f));
        }

        // 총알이 Hitbox에 맞았을 때 부른다.
        public void TakeHit(HitPart part)
        {
            if (IsDead)
                return;

            if (part == HitPart.Head)
            {
                Debug.Log($"[EnemyHealth] {name} 헤드샷!");
                Die();
            }
            else
            {
                Debug.Log($"[EnemyHealth] {name} 몸 명중 (피해 없음)");
                Flash();
                Damaged?.Invoke(this);
            }
        }

        void Die()
        {
            IsDead = true; // 먼저 표시해서 처치가 두 번 처리되지 않게 한다
            Debug.Log($"[EnemyHealth] {name} 처치");
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        void Flash()
        {
            foreach (var r in renderers)
                r.SetPropertyBlock(flashBlock);
            flashEndTime = Time.time + hitFlashTime;
        }

        void Update()
        {
            if (flashEndTime > 0f && Time.time >= flashEndTime)
            {
                foreach (var r in renderers)
                    r.SetPropertyBlock(null);
                flashEndTime = 0f;
            }
        }
    }
}
