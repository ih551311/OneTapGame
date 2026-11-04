using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 이야기 보기 팝업 (2026-11-05 사용자 요청: 미술관 안내판을 스토리 보기로. 버튼을 누르면 다음 쪽으로 넘어간다).
    //  - 쪽마다 그림 한 장(Assets/UI/Story_01.png …)을 보여 준다. 그림을 같은 이름으로 덮어쓰거나, 인스펙터의 Pages 목록에 그림을 더 넣으면
    //    쪽이 늘어난다 (쪽 수 "n / 전체"는 저절로 맞춰진다).
    //  - [이전] [다음] 버튼, 키보드 ← → Enter로 넘긴다. 마지막 쪽에서 [다음]은 [닫기]가 되어 팝업을 닫는다. 열 때마다 첫 쪽부터 시작한다.
    //  - 한 번이라도 열면 "봤다"고 기억해서(PlayerPrefs) 그 뒤로는 저절로 열리지 않는다 (StorySign이 처음 입장 때 한 번 자동으로 연다).
    // 붙일 곳: Canvas/StoryPopup (평소에는 꺼져 있다. 메뉴 LastBullet > Phase 74 이야기 안내판 만들기)
    public class StoryPopup : PopupBase
    {
        const string SeenKey = "ReChroma.StorySeen";

        [SerializeField] Image pageImage;

        [Tooltip("쪽 그림들 (순서대로). 쪽을 늘리려면 여기에 그림을 더 넣는다")]
        [SerializeField] Sprite[] pages;

        [SerializeField] Button prevButton;
        [SerializeField] Button nextButton;

        [Tooltip("다음 버튼의 글자 (마지막 쪽에서는 '닫기'로 바뀐다)")]
        [SerializeField] TMP_Text nextLabel;

        [Tooltip("'1 / 6' 같은 쪽 번호 글자")]
        [SerializeField] TMP_Text pageNumber;

        int index;

        // 이야기를 한 번이라도 열어 봤는지
        public static bool Seen
        {
            get
            {
                try { return PlayerPrefs.GetInt(SeenKey, 0) == 1; }
                catch { return true; } // 저장소를 못 읽으면 자꾸 뜨지 않게 본 것으로 친다
            }
        }

        int LastIndex => Mathf.Max(0, (pages != null ? pages.Length : 0) - 1);

        protected override void OnFirstOpen()
        {
            if (prevButton != null)
                prevButton.onClick.AddListener(Prev);
            if (nextButton != null)
                nextButton.onClick.AddListener(Next);
        }

        protected override void OnOpened()
        {
            index = 0;
            Show();
            try
            {
                PlayerPrefs.SetInt(SeenKey, 1);
                PlayerPrefs.Save();
            }
            catch { }
        }

        protected override void Update()
        {
            base.Update(); // ESC로 닫기
            if (!IsOpen || Keyboard.current == null)
                return;
            var keyboard = Keyboard.current;
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                Next();
            else if (keyboard.leftArrowKey.wasPressedThisFrame)
                Prev();
        }

        void Next()
        {
            if (index >= LastIndex)
            {
                Close();
                return;
            }
            index++;
            Show();
        }

        void Prev()
        {
            if (index <= 0)
                return;
            index--;
            Show();
        }

        void Show()
        {
            int count = pages != null ? pages.Length : 0;
            if (pageImage != null)
            {
                pageImage.sprite = count > 0 ? pages[Mathf.Clamp(index, 0, count - 1)] : null;
                pageImage.enabled = pageImage.sprite != null;
            }
            if (pageNumber != null)
                pageNumber.text = $"{Mathf.Min(index + 1, Mathf.Max(1, count))} / {Mathf.Max(1, count)}";
            if (prevButton != null)
                prevButton.interactable = index > 0;
            if (nextLabel != null)
                nextLabel.text = index >= LastIndex ? "닫기" : "다음";
        }
    }
}
