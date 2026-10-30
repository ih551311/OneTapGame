using LastBullet.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 화면 왼쪽 위에 항상 보이는 내 프로필: 사진 액자와 닉네임 (2026-10-31 사용자 요청).
    // 미술관에서는 프로필만, 전투 맵에서는 이 프로필 아래에 체력바가 붙는다 (체력바는 따로 HealthBar 오브젝트).
    // 사진·닉네임은 타이틀에서 바꾼 것(ProfileStore)을 그대로 보여 준다. 사진이 없으면 빈 사람 모양.
    // 붙일 곳: 미술관·숲·성 마을·사막 씬의 Canvas/HudProfile (메뉴 LastBullet > Phase 33 화면에 프로필 넣기)
    public class HudProfile : MonoBehaviour
    {
        [SerializeField] RawImage picture;

        [Tooltip("사진이 없을 때 보이는 빈 사람 모양")]
        [SerializeField] GameObject emptyShape;

        [SerializeField] TMP_Text nickname;

        void Start()
        {
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
