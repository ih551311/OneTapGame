using LastBullet.Enemy;
using UnityEngine;

namespace LastBullet.Combat
{
    public enum HitPart { Body, Head }

    // 총알이 적에게 맞았을 때의 결과.
    //   Killed     머리에 맞아 처치됨 → 탄환을 바로 다시 쓸 수 있다, 물감을 준다
    //   KilledBody 몸에 맞아 처치됨 (2026-10-31 사용자 결정) → 탄환은 떨어진다, 물감은 없다
    //   Hurt       몸에 맞음 (피해 없음, 깜빡이며 멈칫. 허수아비처럼 몸샷으로 안 죽는 대상) → 탄환이 떨어진다
    //   Blocked    방어형의 방패에 막힘 → "명중으로 처리": 피해는 없고 탄환을 바로 다시 쓸 수 있다 (기획서 2.5)
    public enum HitResult { Hurt, Killed, KilledBody, Blocked }

    // 적의 맞는 부위. 적의 몸 콜라이더와 머리 콜라이더에 각각 붙인다.
    // 총알이 Hitbox가 붙은 콜라이더에 맞으면 "적에게 명중"으로 보고, 부모의 EnemyHealth에 알린다.
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] HitPart part = HitPart.Body;

        public HitPart Part => part;

        EnemyHealth health;

        void Awake()
        {
            health = GetComponentInParent<EnemyHealth>(); // 허수아비처럼 체력이 없으면 null
        }

        // 총이 부른다. shooterPosition은 쏜 사람(총구) 위치 — 방패가 정면에서 온 탄환인지 가리는 데 쓴다.
        public HitResult TakeHit(Vector3 shooterPosition)
        {
            if (health != null)
                return health.TakeHit(part, shooterPosition);
            return part == HitPart.Head ? HitResult.Killed : HitResult.Hurt; // 허수아비
        }
    }
}
