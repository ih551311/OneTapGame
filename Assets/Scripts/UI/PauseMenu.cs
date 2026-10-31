using LastBullet.Core;
using LastBullet.Player;
using LastBullet.Stage;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // ESC 일시정지 창 (미술관, 숲). 열려 있는 팝업이 없을 때 ESC를 누르면 뜬다.
    //   [계속하기]  게임으로 돌아간다 (창이 떠 있을 때 ESC도 같다)
    //   [미술관으로 귀환]  (숲에서만) 이번 판 재료를 창고로 정산하고 미술관으로 돌아간다.
    //   [타이틀로 나가기]  0_Title로 간다. 숲에서는 이번 판 재료(전투 가방)를 잃는다 (사망과 같다).
    // 창이 떠 있는 동안 게임 시간이 멈춘다 (Time.timeScale 0).
    // 팝업(그림판 등)이 열려 있을 때의 ESC는 그 팝업이 처리한다 (PopupBase).
    // 붙일 곳: Canvas/PauseMenu (항상 켜져 있어야 ESC를 받는다. 창은 자식 Window)
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("ESC로 뜨는 창 (평소에는 꺼져 있다)")]
        [SerializeField] GameObject window;

        [SerializeField] Button resumeButton;
        [SerializeField] Button titleButton;

        [Tooltip("[미술관으로 귀환] 버튼. 전투 씬에서만 둔다 (미술관의 일시정지 창에는 없다). 비우면 없는 것으로 본다")]
        [SerializeField] Button homeButton;

        [Tooltip("훈련장처럼 나가면 이번 판 재료를 잃는 곳에서 켠다")]
        [SerializeField] bool losesRunBag;

        [Tooltip("재료를 잃는다는 안내 (losesRunBag일 때만 쓴다)")]
        [SerializeField] GameObject lossWarning;

        PlayerControlLock playerLock;
        PlayerHealth playerHealth;
        PopupBase[] popups;
        bool popupWasOpen; // 지난 프레임 끝에 팝업이 열려 있었는지 (같은 프레임에 팝업이 ESC로 닫힌 직후 바로 뜨지 않게)

        public bool IsOpen => window.activeSelf;

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerLock = player.GetComponent<PlayerControlLock>();
                playerHealth = player.GetComponent<PlayerHealth>();
            }
            popups = FindObjectsByType<PopupBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            resumeButton.onClick.AddListener(Resume);
            titleButton.onClick.AddListener(GoToTitle);
            if (homeButton != null)
                homeButton.onClick.AddListener(GoHome);
            if (lossWarning != null)
                lossWarning.SetActive(losesRunBag);
            window.SetActive(false);
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;
            if (RunResultPopup.IsShowing)
                return; // 결과 창이 떠 있을 때는 [미술관으로]로만 나간다

            if (IsOpen)
                Resume();
            else if (!popupWasOpen && (playerHealth == null || !playerHealth.IsDead))
                Pause();
        }

        void LateUpdate()
        {
            popupWasOpen = AnyPopupOpen();
        }

        bool AnyPopupOpen()
        {
            foreach (var popup in popups)
            {
                if (popup != null && popup.IsOpen)
                    return true;
            }
            return false;
        }

        void Pause()
        {
            window.SetActive(true);
            if (playerLock != null)
                playerLock.SetLocked(true);
            Time.timeScale = 0f;
        }

        void Resume()
        {
            Time.timeScale = 1f;
            window.SetActive(false);
            if (playerLock != null)
                playerLock.SetLocked(false);
        }

        // 미술관으로 귀환: 이번 판에서 얻은 재료(전투 가방)를 창고로 정산하고 미술관으로 돌아간다.
        // 스테이지 클리어가 없는 자유 파밍에서 재료를 가져가는 방법이다 (죽으면 재료를 잃는다).
        // 결과 창(RunResultPopup)이 있으면 이번 판 요약을 보여 주고, [미술관으로]를 누르면 미술관으로 간다.
        void GoHome()
        {
            RunSummary summary = RunBag.Current != null ? RunBag.Current.Summary() : default; // 정산하면 가방이 비므로 먼저 받아 둔다
            if (RunBag.Current != null)
                RunBag.Current.Settle();
            if (RunResultPopup.Current != null)
            {
                window.SetActive(false); // 결과 창이 시간을 멈춘 채 떠 있는다
                RunResultPopup.Current.Show(summary, false);
                return;
            }
            Time.timeScale = 1f; // 멈춘 채 씬을 옮기면 다음 씬도 멈춘다
            SceneManager.LoadScene(SceneNames.Museum);
        }

        void GoToTitle()
        {
            if (losesRunBag && RunBag.Current != null)
                RunBag.Current.Clear();
            Time.timeScale = 1f; // 멈춘 채 씬을 옮기면 다음 씬도 멈춘다
            SceneManager.LoadScene(SceneNames.Title);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
