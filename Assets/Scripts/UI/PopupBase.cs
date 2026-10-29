using LastBullet.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 미술관 팝업(전투 장소 선택, 종이 합성 등)의 공통 부분.
    // 열려 있는 동안 플레이어 조작을 막고 커서를 보여 준다. ESC나 닫기 버튼으로 닫는다.
    // 팝업 오브젝트는 평소에 꺼져 있다.
    public abstract class PopupBase : MonoBehaviour
    {
        [SerializeField] Button closeButton;

        public bool IsOpen => gameObject.activeSelf;

        PlayerControlLock playerLock;
        bool initialized;

        // 처음 열릴 때 한 번: 버튼 연결 등
        protected virtual void OnFirstOpen() { }

        // 열릴 때마다: 숫자 새로 고치기 등
        protected virtual void OnOpened() { }

        public void Open()
        {
            if (!initialized)
            {
                initialized = true;
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    playerLock = player.GetComponent<PlayerControlLock>();
                if (closeButton != null)
                    closeButton.onClick.AddListener(Close);
                OnFirstOpen();
            }
            gameObject.SetActive(true);
            if (playerLock != null)
                playerLock.SetLocked(true);
            OnOpened();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            if (playerLock != null)
                playerLock.SetLocked(false);
        }

        protected virtual void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }
    }
}
