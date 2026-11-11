using System;
using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Enemy
{
    // 적의 체력과 처치 (2026-11-04 사용자 결정: 데스티니식 전투 — B단계. 원탭·즉사 규칙을 없애고 체력(HP)을 다시 넣었다).
    //  - 총알 한 발은 damage만큼 깎는다. 머리에 맞으면 크리티컬(damage × 헤드샷 배수)이다.
    //  - 체력이 0이 되면 처치된다. 처치는 한 번만 일어나고, 그때 Died 이벤트를 보낸다 (EnemyDrop·EnemySplitter·KillHeal이 듣는다).
    //  - 맞을 때마다 정적 이벤트 DamageDealt가 불린다: 힛마커·데미지 숫자·적 체력바가 듣는다.
    //  - 연사에 계속 멈칫하면 못 움직이므로 맞아도 멈칫하지 않는 것이 기본이다 (AI의 hitStunTime 0). Damaged 이벤트는 멈칫이 켜진 적만 쓴다.
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (Hitbox는 자식 콜라이더에)
    public class EnemyHealth : MonoBehaviour
    {
        [Tooltip("화면 체력바에 보이는 이름")]
        [SerializeField] string displayName = "적";

        [Tooltip("최대 체력 (기본값). 적 프리팹마다 다르다")]
        [SerializeField] float maxHealth = 60f;

        [Tooltip("맞았을 때 잠깐 빨갛게 보이는 시간 (초)")]
        [SerializeField] float hitFlashTime = 0.06f;

        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float Health { get; private set; }
        public float HealthRatio => maxHealth > 0f ? Mathf.Clamp01(Health / maxHealth) : 0f;
        public bool IsDead { get; private set; }

        // 마지막으로 맞춘 총알이 크리티컬(머리)이었는지. 처치 순간에 정해져 있다 (EnemyDrop이 물감 보너스, KillHeal이 회복량을 가리는 데 쓴다).
        public bool KilledByHeadshot { get; private set; }

        // 처치되는 순간 한 번 불린다.
        public event Action<EnemyHealth> Died;

        // 맞았을 때 불린다 (멈칫이 켜진 AI가 듣는다).
        public event Action<EnemyHealth> Damaged;

        public struct DamageInfo
        {
            public EnemyHealth enemy;
            public float damage;       // 실제로 깎인 양
            public bool critical;      // 머리에 맞았는지
            public bool killed;        // 이 총알로 처치됐는지
            public Vector3 point;      // 맞은 자리 (월드)
        }

        // 어떤 적이든 맞는 순간 불린다 (화면 표시용)
        public static event Action<DamageInfo> DamageDealt;

        Renderer[] renderers;
        MaterialPropertyBlock flashBlock;
        float flashEndTime;

        // 몸 색 (EnemyDrop이 정한다: 이 적을 잡으면 얻는 물감 색). 맞아서 깜빡인 뒤에도 이 색으로 돌아온다.
        Renderer[] tintTargets;
        MaterialPropertyBlock tintBlock;

        public void SetTint(Renderer[] targets, Color color)
        {
            tintTargets = targets;
            tintBlock = new MaterialPropertyBlock();
            tintBlock.SetColor("_BaseColor", color);
            foreach (var r in targets)
                r.SetPropertyBlock(tintBlock);
        }

        void Awake()
        {
            Health = maxHealth;
            renderers = GetComponentsInChildren<Renderer>();
            flashBlock = new MaterialPropertyBlock();
            flashBlock.SetColor("_BaseColor", new Color(1f, 0.3f, 0.3f));
        }

        // 총알이 Hitbox에 맞았을 때 부른다. critical이면 머리. point는 맞은 자리.
        public HitResult ApplyDamage(float damage, bool critical, Vector3 point)
        {
            if (IsDead)
                return HitResult.Hurt;

            float dealt = Mathf.Min(damage, Health);
            Health -= damage;
            bool killed = Health <= 0f;
            if (killed)
                KilledByHeadshot = critical;
            else
            {
                Flash();
                Damaged?.Invoke(this);
            }

            DamageDealt?.Invoke(new DamageInfo { enemy = this, damage = dealt, critical = critical, killed = killed, point = point });

            if (killed)
            {
                Debug.Log($"[EnemyHealth] {name} 처치 ({(critical ? "크리티컬" : "일반")}, 마지막 {dealt:0})");
                Die();
                return critical ? HitResult.Killed : HitResult.KilledBody;
            }
            return HitResult.Hurt;
        }

        void Die()
        {
            IsDead = true; // 먼저 표시해서 처치가 두 번 처리되지 않게 한다
            Died?.Invoke(this);
            // 처치 연출(쓰러지고 터지는 동안)이 있으면 그 시간만큼 기다렸다가 지운다
            var deathEffect = GetComponent<EnemyDeathEffect>();
            Destroy(gameObject, deathEffect != null ? deathEffect.Duration : 0f);
        }

        // 지정한 색으로 잠깐 번쩍인다 (폭발에 맞은 적이 하얗게 달아오르는 연출, 2026-11-11). 끝나면 원래 몸 색으로 돌아온다.
        public void FlashColor(Color color, float time)
        {
            if (IsDead || time <= 0f)
                return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            foreach (var r in renderers)
                if (r != null)
                    r.SetPropertyBlock(block);
            flashEndTime = Time.time + time;
        }

        void Flash()
        {
            if (hitFlashTime <= 0f)
                return;
            foreach (var r in renderers)
                r.SetPropertyBlock(flashBlock);
            flashEndTime = Time.time + hitFlashTime;
        }

        void Update()
        {
            if (flashEndTime > 0f && Time.time >= flashEndTime)
            {
                foreach (var r in renderers)
                    r.SetPropertyBlock(tintTargets != null && System.Array.IndexOf(tintTargets, r) >= 0 ? tintBlock : null);
                flashEndTime = 0f;
            }
        }
    }
}
