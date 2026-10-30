using LastBullet.Core;
using LastBullet.Inventory;
using LastBullet.Player;
using LastBullet.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // "이번 판 결과" 창 (2026-10-31 사용자 요청). 전투 맵에서 미술관으로 돌아가기 전에 한 번 보여 준다.
    //  - 귀환([미술관으로 귀환]): 제목 "이번 판 결과", 얻은 재료 (창고로 정산된 양)
    //  - 사망: 제목 "쓰러졌어요", 잃은 재료 (사라진 양)
    // 처치 수, 종이조각, 물감 9색 양이 0에서부터 차오르듯 올라간다. [미술관으로]를 누르면 미술관으로 간다.
    // 창이 떠 있는 동안 게임 시간은 멈춘다. ESC로는 닫히지 않는다 (일시정지 창도 안 뜬다).
    // 글자(제목·이름)는 그림(PNG), 숫자만 TextMeshPro다: Assets/UI/ResultTitle_*, ResultCaption_*, ResultLabel_Kills 등.
    // 붙일 곳: 전투 맵 Canvas/RunResultPopup (자식 Window가 창이고 평소에는 꺼져 있다)
    public class RunResultPopup : MonoBehaviour
    {
        public static RunResultPopup Current { get; private set; }

        // 결과 창이 떠 있는지. 일시정지 창이 위에 뜨지 않게 PauseMenu가 본다.
        public static bool IsShowing => Current != null && Current.window.activeSelf;

        [Tooltip("결과 창 전체 (평소에는 꺼져 있다). 이 스크립트가 붙은 오브젝트는 항상 켜 둔다 (꺼진 오브젝트는 Awake가 안 불려 Current를 못 정한다)")]
        [SerializeField] GameObject window;

        [Header("제목·설명 그림 (귀환/사망에 따라 바뀐다)")]
        [SerializeField] GameObject titleClear;
        [SerializeField] GameObject titleDead;
        [SerializeField] GameObject captionGained;
        [SerializeField] GameObject captionLost;

        [Header("숫자")]
        [SerializeField] TMP_Text killsCount;
        [SerializeField] TMP_Text paperScrapCount;

        [Tooltip("물감 9색 순서: 빨강, 주황, 노랑, 초록, 파랑, 남색, 보라, 하양, 검정")]
        [SerializeField] TMP_Text[] paintCounts = new TMP_Text[PaintColors.Count];

        [SerializeField] Button toMuseumButton;

        [Tooltip("숫자가 0에서 올라가는 시간 (초)")]
        [SerializeField] float countUpTime = 0.7f;

        RunSummary summary;
        float elapsed;

        void Awake()
        {
            Current = this;
            window.SetActive(false);
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        void Start()
        {
            toMuseumButton.onClick.AddListener(GoToMuseum);
        }

        // 결과를 보여 준다. died가 true면 쓰러진 결과(잃은 재료).
        public void Show(RunSummary result, bool died)
        {
            summary = result;
            elapsed = 0f;
            titleClear.SetActive(!died);
            titleDead.SetActive(died);
            captionGained.SetActive(!died);
            captionLost.SetActive(died);
            SetNumbers(0f);

            window.SetActive(true);
            transform.SetAsLastSibling(); // 다른 UI 위에

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var playerLock = player.GetComponent<PlayerControlLock>();
                if (playerLock != null)
                    playerLock.SetLocked(true); // 커서를 풀고 조작을 막는다
            }
            Time.timeScale = 0f;
        }

        void Update()
        {
            if (!window.activeSelf || elapsed >= countUpTime)
                return;
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            SetNumbers(Mathf.Clamp01(elapsed / countUpTime));
        }

        // k(0~1)만큼 올라간 숫자를 보여 준다 (끝에서는 정확한 값)
        void SetNumbers(float k)
        {
            float eased = 1f - (1f - k) * (1f - k); // 처음엔 빠르게, 끝에서 천천히
            killsCount.text = Mathf.RoundToInt(summary.kills * eased).ToString();
            paperScrapCount.text = Mathf.RoundToInt(summary.paperScraps * eased).ToString();
            for (int i = 0; i < paintCounts.Length; i++)
            {
                int value = summary.paints != null && i < summary.paints.Length ? summary.paints[i] : 0;
                paintCounts[i].text = Mathf.RoundToInt(value * eased).ToString();
            }
        }

        void GoToMuseum()
        {
            Time.timeScale = 1f; // 멈춘 채 씬을 옮기면 다음 씬도 멈춘다
            SceneManager.LoadScene(SceneNames.Museum);
        }
    }
}
