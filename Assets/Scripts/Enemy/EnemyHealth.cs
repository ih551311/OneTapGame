using System;
using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Enemy
{
    // 적의 체력과 처치 (기획서 2.4, 2.5).
    // 머리에 맞으면 체력과 관계없이 즉사, 몸에 맞으면 피해를 받는다.
    // 처치는 한 번만 일어나고, 그때 Died 이벤트를 보낸다 (Phase 5에서 드롭이 여기에 연결된다).
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (Hitbox는 자식 콜라이더에)
    public class EnemyHealth : MonoBehaviour
    {
        [Tooltip("최대 체력. 돌진형 30, 원거리형 20, 방어형 60")]
        [SerializeField] int maxHp = 30;

        [Tooltip("맞았을 때 잠깐 빨갛게 보이는 시간 (초)")]
        [SerializeField] float hitFlashTime = 0.1f;

        public int Hp { get; private set; }
        public bool IsDead { get; private set; }

        // 처치되는 순간 한 번 불린다.
        public event Action<EnemyHealth> Died;

        Renderer[] renderers;
        MaterialPropertyBlock flashBlock;
        float flashEndTime;

        void Awake()
        {
            Hp = maxHp;
            renderers = GetComponentsInChildren<Renderer>();
            flashBlock = new MaterialPropertyBlock();
            flashBlock.SetColor("_BaseColor", new Color(1f, 0.3f, 0.3f));
        }

        // 총알이 Hitbox에 맞았을 때 부른다. bodyDamage는 몸에 맞았을 때의 피해.
        public void TakeHit(HitPart part, int bodyDamage)
        {
            if (IsDead)
                return;

            if (part == HitPart.Head)
                Hp = 0; // 헤드샷 원탭
            else
                Hp = Mathf.Max(0, Hp - bodyDamage);

            Debug.Log($"[EnemyHealth] {name} {(part == HitPart.Head ? "헤드샷!" : $"몸 -{bodyDamage}")} → 체력 {Hp}/{maxHp}");

            if (Hp == 0)
                Die();
            else
                Flash();
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
