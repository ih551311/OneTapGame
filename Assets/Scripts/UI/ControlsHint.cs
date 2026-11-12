using UnityEngine;

namespace LastBullet.UI
{
    // 전투 시작 때의 조작 안내 (2026-11-12): 전투 맵에 들어오면 화면 아래 가운데에 종이 판으로 조작 키를 알려 준다.
    //  - 처음 세 번 들어올 때만 뜬다 (PlayerPrefs). 위에서 8초 동안 보이다가 서서히 사라진다. 다시하기를 하면 다시 처음부터 센다.
    //  - 글과 판 그림은 Canvas/ControlsHint 에서 고친다 (판 그림은 FrameProgress_Panel.png).
    // 붙일 곳: 전투 맵 Canvas/ControlsHint (메뉴 LastBullet > Phase 101 조작 안내 만들기)
    [RequireComponent(typeof(CanvasGroup))]
    public class ControlsHint : MonoBehaviour
    {
        const string CountKey = "ReChroma.ControlsHintCount";
        const int ShowTimes = 3;

        [SerializeField] float holdSeconds = 8f;
        [SerializeField] float fadeSeconds = 1.2f;

        CanvasGroup group;
        float start;

        public static void Forget()
        {
            try
            {
                PlayerPrefs.DeleteKey(CountKey);
                PlayerPrefs.Save();
            }
            catch { }
        }

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        void Start()
        {
            try
            {
                int count = PlayerPrefs.GetInt(CountKey, 0);
                if (count >= ShowTimes)
                {
                    gameObject.SetActive(false);
                    return;
                }
                PlayerPrefs.SetInt(CountKey, count + 1);
                PlayerPrefs.Save();
            }
            catch
            {
                gameObject.SetActive(false);
                return;
            }
            start = Time.unscaledTime;
        }

        void Update()
        {
            float t = Time.unscaledTime - start;
            if (t < 0.5f)
                group.alpha = t / 0.5f;                      // 서서히 나타난다
            else if (t < holdSeconds)
                group.alpha = 1f;
            else if (t < holdSeconds + fadeSeconds)
                group.alpha = 1f - (t - holdSeconds) / fadeSeconds; // 서서히 사라진다
            else
                gameObject.SetActive(false);
        }
    }
}
