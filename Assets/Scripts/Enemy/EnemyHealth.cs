using System;
using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Enemy
{
    // 적의 처치 (기획서 2.4, 2.5 — 2026-10-30 변경: 헤드샷 원탭만. 2026-10-31 변경: 몸샷도 처치, 단 물감은 헤드샷만).
    // 머리에 맞으면 즉사 + 물감. 몸에 맞아도 즉사지만 물감은 없다 (bodyShotKills를 끄면 예전처럼 빨갛게 깜빡이며 멈칫만).
    // 처치는 한 번만 일어나고, 그때 Died 이벤트를 보낸다 (Phase 5에서 드롭이 여기에 연결된다).
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (Hitbox는 자식 콜라이더에)
    public class EnemyHealth : MonoBehaviour
    {
        [Tooltip("How long it flashes red when hit (seconds)")]
        [SerializeField] float hitFlashTime = 0.1f;

        [Tooltip("Whether a body shot also kills (user decision 2026-10-31). If on, a body kill gives no paint (the bullet returns immediately). If off, only headshots kill.")]
        [SerializeField] bool bodyShotKills = true;

        public bool IsDead { get; private set; }

        // 머리에 맞아 처치됐는지 (EnemyDrop이 물감을 줄지 가리는 데 쓴다). Died가 불리는 시점에 정해져 있다.
        public bool KilledByHeadshot { get; private set; }

        // 처치되는 순간 한 번 불린다.
        public event Action<EnemyHealth> Died;

        // 몸에 맞았을 때 불린다. EnemyAI가 잠깐 멈칫(Hit 상태)하는 데 쓴다.
        public event Action<EnemyHealth> Damaged;

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
            renderers = GetComponentsInChildren<Renderer>();
            flashBlock = new MaterialPropertyBlock();
            flashBlock.SetColor("_BaseColor", new Color(1f, 0.3f, 0.3f));
        }

        // 총알이 Hitbox에 맞았을 때 부른다. shooterPosition은 쏜 위치.
        public HitResult TakeHit(HitPart part, Vector3 shooterPosition)
        {
            if (IsDead)
                return HitResult.Hurt;

            if (part == HitPart.Head)
            {
                Debug.Log($"[EnemyHealth] {name} 헤드샷!");
                KilledByHeadshot = true;
                Die();
                return HitResult.Killed;
            }

            if (bodyShotKills)
            {
                Debug.Log($"[EnemyHealth] {name} 몸샷 처치 (물감 없음)");
                Die();
                return HitResult.KilledBody;
            }

            Debug.Log($"[EnemyHealth] {name} 몸 명중 (피해 없음)");
            Flash();
            Damaged?.Invoke(this);
            return HitResult.Hurt;
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
                    r.SetPropertyBlock(tintTargets != null && System.Array.IndexOf(tintTargets, r) >= 0 ? tintBlock : null);
                flashEndTime = 0f;
            }
        }
    }
}
