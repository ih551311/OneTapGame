using LastBullet.Enemy;
using UnityEngine;

namespace LastBullet.Combat
{
    public enum HitPart { Body, Head }

    // 총알이 적에게 맞았을 때의 결과.
    //   Killed     머리(크리티컬)에 맞아 처치됨
    //   KilledBody 몸에 맞아 처치됨
    //   Hurt       맞았지만 아직 살아 있음 (또는 체력이 없는 허수아비)
    public enum HitResult { Hurt, Killed, KilledBody }

    // 적의 맞는 부위. 적의 몸 콜라이더와 머리 콜라이더에 각각 붙인다.
    // 총알이 Hitbox가 붙은 콜라이더에 맞으면 "적에게 명중"으로 보고, 부모의 EnemyHealth에 피해를 준다.
    // 머리(Head)는 크리티컬이라 피해가 headMultiplier배가 된다 (2026-11-04 데스티니식 전투).
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] HitPart part = HitPart.Body;

        public HitPart Part => part;

        EnemyHealth health;

        void Awake()
        {
            health = GetComponentInParent<EnemyHealth>(); // 허수아비처럼 체력이 없으면 null
        }

        // 총이 부른다. damage는 몸에 맞았을 때의 피해, headMultiplier는 머리 배수, point는 맞은 자리.
        public HitResult TakeHit(float damage, float headMultiplier, Vector3 point)
        {
            bool critical = part == HitPart.Head;
            if (health != null)
                return health.ApplyDamage(critical ? damage * headMultiplier : damage, critical, point);
            return critical ? HitResult.Killed : HitResult.Hurt; // 허수아비
        }
    }
}
