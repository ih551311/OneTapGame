using LastBullet.Player;
using UnityEngine;

namespace LastBullet.Combat
{
    // 총구 불꽃과 탄피 (2026-11-04, 데스티니식 전투). 총을 쏠 때마다 총구에서 불꽃이 번쩍이고, 총 오른쪽 위에서 탄피가 튀어 나간다.
    //  - 불꽃 = 파티클 한 장(Assets/Effects/MuzzleFlash.png, 별 모양) + 아주 짧게 켜지는 주황 불빛.
    //  - 탄피 = 작은 황동색 막대(Assets/Materials/ShellCasing.mat)가 날아가 중력으로 떨어진다.
    //  - 1인칭이면 1인칭 총(FirstPersonGun/Model/Ak47Holder)의 것을, 아니면 3인칭 캐릭터 총의 것을 쓴다. (3인칭 효과가 1인칭 화면에 겹쳐 보이지 않게)
    // 붙일 곳: Player (메뉴 LastBullet > Phase 64 총구 불꽃·탄피·맞은 소리 만들기)
    public class GunEffects : MonoBehaviour
    {
        [System.Serializable]
        public class Set
        {
            [Tooltip("총구 불꽃 파티클")]
            public ParticleSystem flash;
            [Tooltip("탄피 파티클")]
            public ParticleSystem casing;
            [Tooltip("쏠 때 잠깐 켜지는 불빛")]
            public Light light;
        }

        [SerializeField] AutoGun gun;
        [SerializeField] CameraZoom zoom;
        [SerializeField] Set third;
        [SerializeField] Set first;

        [Tooltip("총구 불빛이 켜져 있는 시간 (초)")]
        [SerializeField] float lightTime = 0.04f;

        Light activeLight;
        float lightEnd;

        // 지금 보이는 총의 총구 자리 (1인칭이면 1인칭 총, 아니면 3인칭 캐릭터 총). 총알과 탄도선이 여기서 나간다. 없으면 null.
        public Transform ActiveMuzzle
        {
            get
            {
                bool firstPerson = zoom != null && zoom.IsFirstPerson;
                var set = firstPerson ? first : third;
                return set != null && set.flash != null ? set.flash.transform : null;
            }
        }

        void OnEnable()
        {
            if (gun != null)
                gun.Fired += OnFired;
        }

        void OnDisable()
        {
            if (gun != null)
                gun.Fired -= OnFired;
        }

        void OnFired()
        {
            bool firstPerson = zoom != null && zoom.IsFirstPerson;
            var set = firstPerson ? first : third;
            if (set == null)
                return;
            if (set.flash != null)
                set.flash.Emit(1);
            if (set.casing != null)
                set.casing.Emit(1);
            if (set.light != null)
            {
                set.light.enabled = true;
                activeLight = set.light;
                lightEnd = Time.time + lightTime;
            }
        }

        void Update()
        {
            if (activeLight != null && Time.time >= lightEnd)
            {
                activeLight.enabled = false;
                activeLight = null;
            }
        }
    }
}
