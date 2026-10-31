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

        [Tooltip("Random = random on each entry, the others are fixed (for testing)")]
        [SerializeField] Mode mode = Mode.Random;

        [Tooltip("Chance of night when random")]
        [Range(0f, 1f)]
        [SerializeField] float nightChance = 0.5f;

        [Tooltip("Night sky (6-sided cubemap Skybox material). The day sky uses what the scene already has.")]
        [SerializeField] Material nightSky;

        [Tooltip("Directional Light used as sun (moon) light")]
        [SerializeField] Light sun;

        [SerializeField] Color nightSunColor = new Color(0.55f, 0.65f, 1f);
        [SerializeField] float nightSunIntensity = 0.5f;

        [Tooltip("Multiplier for the ambient color at night (darker and bluer)")]
        [SerializeField] Color nightAmbientTint = new Color(0.45f, 0.55f, 0.85f);

        [Tooltip("Night strength when ambient light comes from the sky (Skybox)")]
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
