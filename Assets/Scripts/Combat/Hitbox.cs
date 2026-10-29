using LastBullet.Enemy;
using UnityEngine;

namespace LastBullet.Combat
{
    public enum HitPart { Body, Head }

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

        // 총이 부른다. 체력이 있는 적이면 피해를 준다.
        public void TakeHit(int bodyDamage)
        {
            if (health != null)
                health.TakeHit(part, bodyDamage);
        }
    }
}
