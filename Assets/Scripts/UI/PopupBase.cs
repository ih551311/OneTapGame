using System.Collections.Generic;
using LastBullet.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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

        // 지금 열려 있는 팝업들 (나중에 연 것이 뒤). 팝업 위에 팝업이 열릴 수 있다 (그림판 위의 "내 그림").
        // 맨 위 팝업만 ESC를 받고, 모두 닫혔을 때만 플레이어 조작을 되돌린다.
        static readonly List<PopupBase> openStack = new List<PopupBase>();
        static int escHandledFrame = -1;

        PlayerControlLock playerLock;
        bool initialized;

        // 처음 열려서 OnFirstOpen이 불린 뒤인지 (한 번도 안 열린 팝업의 OnDisable에서 값을 건드리지 않게)
        protected bool IsInitialized => initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            openStack.Clear();
            escHandledFrame = -1;
            SceneManager.sceneLoaded += (scene, mode) => openStack.Clear(); // 팝업이 열린 채 씬이 바뀌어도 남지 않게
        }

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
                    closeButton.onClick.AddListener(RequestClose);
                OnFirstOpen();
            }
            gameObject.SetActive(true);
            openStack.Remove(this);
            openStack.Add(this);
            if (playerLock != null)
                playerLock.SetLocked(true);
            OnOpened();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            openStack.Remove(this);
            if (playerLock != null && openStack.Count == 0)
                playerLock.SetLocked(false);
        }

        // 닫기 버튼·ESC가 부른다. 닫기 전에 확인이 필요한 팝업(그림판)은 이것을 바꿔 쓴다.
        protected virtual void RequestClose()
        {
            Close();
        }

        protected virtual void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;
            // 맨 위 팝업만 처리한다. 같은 프레임에 아래 팝업이 이어서 반응하지 않게 프레임을 기록한다.
            if (openStack.Count > 0 && openStack[openStack.Count - 1] == this && escHandledFrame != Time.frameCount)
            {
                escHandledFrame = Time.frameCount;
                RequestClose();
            }
        }
    }
}
