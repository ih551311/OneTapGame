using LastBullet.Core;
using LastBullet.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 장소 선택 팝업 (미술관의 지도 탁자에서 E로 연다).
    // 열려 있는 동안 플레이어 조작을 막고 커서를 보여 준다. ESC나 닫기 버튼으로 닫는다.
    // 숲은 훈련장을 클리어하면 열린다 (기획서 2.6). 해금 저장은 Phase 5, 숲 씬은 Phase 10이라 지금은 잠겨 있다.
    // 붙일 곳: Canvas/StageSelectPopup (평소에는 꺼져 있다)
    public class StageSelectPopup : MonoBehaviour
    {
        [SerializeField] Button trainingButton;
        [SerializeField] Button forestButton;
        [SerializeField] Button closeButton;

        public bool IsOpen => gameObject.activeSelf;

        PlayerControlLock playerLock;
        bool initialized;

        void Initialize()
        {
            if (initialized)
                return;
            initialized = true;
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                playerLock = player.GetComponent<PlayerControlLock>();
            trainingButton.onClick.AddListener(() => LoadStage(SceneNames.Training));
            forestButton.interactable = false; // 숲: 아직 잠김
            closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            Initialize();
            gameObject.SetActive(true);
            if (playerLock != null)
                playerLock.SetLocked(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            if (playerLock != null)
                playerLock.SetLocked(false);
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        void LoadStage(string sceneName)
        {
            Debug.Log($"[StageSelectPopup] {sceneName}(으)로 이동");
            SceneManager.LoadScene(sceneName);
        }
    }
}
