using UnityEngine;

namespace LastBullet.Stage
{
    // 전투 맵의 분위기 입자 (2026-11-05 사용자 요청: 박물관처럼 맵에 맞는 먼지 같은 요소).
    //  - 숲: 꽃가루와 떨어지는 낙엽 (밤에는 반딧불이) / 성 마을: 먼지와 흩날리는 꽃잎 (밤에는 불씨) / 사막: 모래바람과 먼지 안개.
    //  - 이 오브젝트가 플레이어를 따라다니고 입자는 월드에 남기 때문에, 플레이어 주변에만 입자가 있어도 맵 어디든 분위기가 난다 (가볍다).
    //  - 낮과 밤(DayNight.IsNight)에 맞춰 Day Effects / Night Effects 중 한쪽만 켠다.
    // 붙일 곳: 숲·성 마을·사막 씬의 Atmosphere 오브젝트 (자식에 입자들. 메뉴 LastBullet > Phase 75 전투 맵 분위기 입자 만들기)
    public class MapAtmosphere : MonoBehaviour
    {
        [Tooltip("낮에만 켜는 효과들")]
        [SerializeField] GameObject[] dayEffects;

        [Tooltip("밤에만 켜는 효과들")]
        [SerializeField] GameObject[] nightEffects;

        Transform player;

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            bool night = DayNight.IsNight;
            Toggle(dayEffects, !night);
            Toggle(nightEffects, night);
            Follow();
        }

        void LateUpdate()
        {
            Follow();
        }

        void Follow()
        {
            if (player != null)
                transform.position = player.position;
        }

        static void Toggle(GameObject[] effects, bool on)
        {
            if (effects == null)
                return;
            foreach (var effect in effects)
                if (effect != null)
                    effect.SetActive(on);
        }
    }
}
