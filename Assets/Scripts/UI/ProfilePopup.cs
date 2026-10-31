using LastBullet.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 프로필 창 (타이틀의 프로필 카드를 누르면 열린다): 지금 프로필 사진을 보여 주고
    //   [직접 그리기] 타이틀에서 새로 그린다 (물감·종이를 쓰지 않는다)
    //   [그림에서 고르기] 미술관에서 그린 그림 중 하나를 고른다
    // 닉네임은 옵션에서 바꾼다.
    // 붙일 곳: 0_Title의 Canvas/ProfilePopup (평소에는 꺼져 있다)
    public class ProfilePopup : PopupBase
    {
        [SerializeField] Button drawButton;
        [SerializeField] Button pickButton;
        [SerializeField] PopupBase drawPopup;
        [SerializeField] PopupBase pickPopup;

        [Tooltip("Preview of the current profile picture")]
        [SerializeField] RawImage preview;

        [Tooltip("Empty person shape shown when there is no photo")]
        [SerializeField] GameObject emptyShape;

        protected override void OnFirstOpen()
        {
            drawButton.onClick.AddListener(drawPopup.Open);
            pickButton.onClick.AddListener(pickPopup.Open);
            ProfileStore.Changed += Refresh;
        }

        void OnDestroy()
        {
            ProfileStore.Changed -= Refresh;
        }

        protected override void OnOpened()
        {
            Refresh();
        }

        void Refresh()
        {
            var texture = ProfileStore.LoadTexture();
            preview.texture = texture;
            preview.enabled = texture != null;
            emptyShape.SetActive(texture == null);
        }
    }
}
