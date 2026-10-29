using LastBullet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 타이틀 메인 메뉴 (0_Title 씬). 버튼: 시작 / 그림 보기 / 도움말 / 옵션.
    // 기획서 3.2의 MainMenu. 이어하기·새 게임·갤러리 보기 구분은 Phase 9에서 더한다.
    // 붙일 곳: 0_Title의 Canvas/Menu
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button helpButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button galleryButton;
        [SerializeField] PopupBase helpPopup;
        [SerializeField] PopupBase optionsPopup;
        [SerializeField] PopupBase galleryPopup;

        void Start()
        {
            // 다른 씬에서 잠갔던 마우스 커서를 보이게 한다
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            startButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Museum));
            helpButton.onClick.AddListener(helpPopup.Open);
            optionsButton.onClick.AddListener(optionsPopup.Open);
            if (galleryButton != null)
                galleryButton.onClick.AddListener(galleryPopup.Open);
        }
    }
}
