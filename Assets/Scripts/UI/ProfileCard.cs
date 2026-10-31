using LastBullet.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 타이틀 화면 왼쪽의 "내 프로필" 카드: 프로필 사진(액자)과 닉네임. 누르면 프로필 창(사진 그리기·고르기)이 열린다.
    // 사진이 없으면 빈 사람 모양이 보인다. 닉네임·사진이 바뀌면(옵션에서 이름 변경, 프로필 창에서 사진 변경) 바로 새로 그려진다.
    // 붙일 곳: 0_Title의 Canvas/ProfileCard
    public class ProfileCard : MonoBehaviour
    {
        [Tooltip("누르면 프로필 창이 열리는 액자")]
        [SerializeField] Button button;

        [Tooltip("프로필 사진이 보이는 칸")]
        [SerializeField] RawImage picture;

        [Tooltip("사진이 없을 때 보이는 빈 사람 모양")]
        [SerializeField] GameObject emptyShape;

        [SerializeField] TMP_Text nickname;

        [SerializeField] PopupBase profilePopup;

        void Start()
        {
            button.onClick.AddListener(profilePopup.Open);
            ProfileStore.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            ProfileStore.Changed -= Refresh;
        }

        void Refresh()
        {
            var texture = ProfileStore.LoadTexture();
            picture.texture = texture;
            picture.enabled = texture != null;
            emptyShape.SetActive(texture == null);
            nickname.text = ProfileStore.Nickname;
        }
    }
}
