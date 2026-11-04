using UnityEngine;

namespace LastBullet.UI
{
    // 미술관 입구 안내 팝업 (2026-11-05 사용자 요청: 처음 들어온 사람에게 조작을 알려 주는 튜토리얼).
    //  - 왼쪽: 조작 키, 오른쪽: 게임 진행 순서. 글은 Canvas/GuidePopup/Panel의 Controls·Flow 글자에서 고친다.
    //  - 한 번이라도 열면 "봤다"고 기억해서(PlayerPrefs), 그 뒤로는 저절로 열리지 않는다. 안내판(GuideSign)에서 E로 언제든 다시 볼 수 있다.
    // 붙일 곳: Canvas/GuidePopup (평소에는 꺼져 있다. 메뉴 LastBullet > Phase 72 입구 안내판 만들기)
    public class GuidePopup : PopupBase
    {
        const string SeenKey = "ReChroma.GuideSeen";

        // 안내를 한 번이라도 열어 봤는지
        public static bool Seen
        {
            get
            {
                try { return PlayerPrefs.GetInt(SeenKey, 0) == 1; }
                catch { return true; } // 저장소를 못 읽으면 자꾸 뜨지 않게 본 것으로 친다
            }
        }

        protected override void OnOpened()
        {
            try
            {
                PlayerPrefs.SetInt(SeenKey, 1);
                PlayerPrefs.Save();
            }
            catch { }
        }
    }
}
