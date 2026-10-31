using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.Stage
{
    // 낮과 밤 (2026-10-31 사용자 요청). 전투 맵에 들어갈 때마다 낮이나 밤을 무작위로 정한다.
    // 낮과 밤은 겉모습만 다르고 게임에는 영향이 없다 (적·물감·피해가 똑같다).
    //  - 낮: 씬에 원래 설정된 하늘·햇빛·주변광 그대로
    //  - 밤: 밤하늘 머티리얼, 푸르고 약한 달빛, 어둡고 푸른 주변광 (적이 보일 만큼은 밝게 둔다)
    // 붙일 곳: 숲·성 마을·사막 씬의 DayNight 오브젝트 (메뉴 LastBullet > Phase 16 낮과 밤 넣기)
    public class DayNight : MonoBehaviour
    {
        public enum Mode { Random, AlwaysDay, AlwaysNight }

        [Tooltip("Random = 들어갈 때마다 무작위, 나머지는 고정 (확인용)")]
        [SerializeField] Mode mode = Mode.Random;

        [Tooltip("무작위일 때 밤이 될 확률")]
        [Range(0f, 1f)]
        [SerializeField] float nightChance = 0.5f;

        [Tooltip("밤하늘 (6면 큐브맵 Skybox 머티리얼). 낮 하늘은 씬에 설정된 것을 그대로 쓴다")]
        [SerializeField] Material nightSky;

        [Tooltip("해(달)빛으로 쓸 Directional Light")]
        [SerializeField] Light sun;

        [SerializeField] Color nightSunColor = new Color(0.55f, 0.65f, 1f);
        [SerializeField] float nightSunIntensity = 0.5f;

        [Tooltip("밤의 주변광 색을 곱하는 값 (밝기를 줄이고 푸르게)")]
        [SerializeField] Color nightAmbientTint = new Color(0.45f, 0.55f, 0.85f);

        [Tooltip("주변광이 하늘 기준(Skybox)일 때 밤의 세기")]
        [SerializeField] float nightAmbientIntensity = 0.55f;

        public static bool IsNight { get; private set; }

        void Awake()
        {
            IsNight = mode == Mode.AlwaysNight || (mode == Mode.Random && Random.value < nightChance);
            if (IsNight)
                ApplyNight();
            Debug.Log($"[DayNight] {(IsNight ? "밤" : "낮")}");
        }

        void ApplyNight()
        {
            if (nightSky != null)
                RenderSettings.skybox = nightSky;
            if (sun != null)
            {
                sun.color = nightSunColor;
                sun.intensity = nightSunIntensity;
            }
            if (RenderSettings.ambientMode == AmbientMode.Trilight)
            {
                RenderSettings.ambientSkyColor *= nightAmbientTint;
                RenderSettings.ambientEquatorColor *= nightAmbientTint;
                RenderSettings.ambientGroundColor *= nightAmbientTint;
            }
            else
            {
                RenderSettings.ambientIntensity = nightAmbientIntensity;
            }
            DynamicGI.UpdateEnvironment();
        }
    }
}
