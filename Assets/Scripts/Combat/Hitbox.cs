using UnityEngine;

namespace LastBullet.Combat
{
    public enum HitPart { Body, Head }

    // 적의 맞는 부위. 적의 몸 콜라이더와 머리 콜라이더에 각각 붙인다.
    // 총알이 Hitbox가 붙은 콜라이더에 맞으면 "적에게 명중"으로 본다.
    // 피해 처리(헤드샷 즉사, 몸 15)는 Phase 4에서 EnemyHealth와 연결한다.
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] HitPart part = HitPart.Body;

        public HitPart Part => part;
    }
}
