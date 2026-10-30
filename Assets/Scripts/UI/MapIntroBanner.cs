using LastBullet.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 맵에 들어가면 화면 위쪽에 "사막 · 밤 / 얻는 물감 ● 보라 ● 하양 ● 검정"을 잠깐 보여 준다 (2026-10-31 사용자 요청).
    // 서서히 나타나고 → 잠깐 머물고 → 서서히 사라진다. 낮/밤 글자는 DayNight가 정한 값을 따른다.
    // 글자와 색 점은 모두 그림(PNG)이다: Assets/UI/StageTitle_<맵>_<Day|Night>.png, StageColors_<맵>.png
    // 붙일 곳: 숲·성 마을·사막 씬의 Canvas/MapIntro (CanvasGroup 필요)
    [RequireComponent(typeof(CanvasGroup))]
    public class MapIntroBanner : MonoBehaviour
    {
        [Tooltip("맵 이름 + 낮/밤 그림이 들어가는 Image")]
        [SerializeField] Image titleImage;

        [SerializeField] Sprite daySprite;
        [SerializeField] Sprite nightSprite;

        [Header("시간 (초)")]
        [SerializeField] float fadeIn = 0.6f;
        [SerializeField] float hold = 3.5f;
        [SerializeField] float fadeOut = 1.0f;

        CanvasGroup group;
        float elapsed;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        void Start()
        {
            titleImage.sprite = DayNight.IsNight ? nightSprite : daySprite;
        }

        // 일시정지 중에도 흐르게 realtime으로 센다.
        // 맵을 불러온 직후 첫 프레임은 시간이 크게 튀어서(몇 초) 안내가 바로 지나가 버리므로 한 프레임에 0.1초까지만 센다.
        void Update()
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (elapsed < fadeIn)
                group.alpha = elapsed / fadeIn;
            else if (elapsed < fadeIn + hold)
                group.alpha = 1f;
            else if (elapsed < fadeIn + hold + fadeOut)
                group.alpha = 1f - (elapsed - fadeIn - hold) / fadeOut;
            else
            {
                group.alpha = 0f;
                gameObject.SetActive(false);
            }
        }
    }
}
