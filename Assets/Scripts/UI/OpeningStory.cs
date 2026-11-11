using System.Collections;
using UnityEngine;

namespace LastBullet.UI
{
    // 시작 이야기 (2026-11-11 사용자 결정): 새로 시작하고 미술관에 처음 들어오면 "부모님이 열쇠를 건네준 날" 이야기 창(OpeningPopup)이 저절로 한 번 열린다.
    //  - 한 번 열었으면 "봤다"고 기억한다(PlayerPrefs). 다시하기(EndingGalleryPopup.RestartGame)를 하면 기억을 지워서 새로 시작할 때 다시 뜬다.
    //  - 글은 Assets/UI/0_OpeningPages.txt 를 메모장으로 고친다 (줄에 --- 만 쓰면 쪽이 나뉜다).
    //  - 입구 안내판(개발 후기, StorySign)은 더 이상 저절로 열리지 않는다 (사용자 결정). E로 볼 때만 열린다.
    // 붙일 곳: 미술관 Canvas/OpeningStory (평소에도 켜져 있는 빈 오브젝트. popup에 Canvas/OpeningPopup 연결. 메뉴 LastBullet > Phase 98 시작 이야기 만들기)
    public class OpeningStory : MonoBehaviour
    {
        const string SeenKey = "ReChroma.OpeningSeen";

        [SerializeField] PopupBase popup;

        [Tooltip("미술관에 들어온 뒤 이야기가 열리기까지 기다리는 시간 (초)")]
        [SerializeField] float delay = 1.0f;

        public static bool Seen
        {
            get
            {
                try { return PlayerPrefs.GetInt(SeenKey, 0) == 1; }
                catch { return true; } // 저장소를 못 읽으면 자꾸 뜨지 않게 본 것으로 친다
            }
        }

        // '봤다'는 기억을 지운다 (다시하기)
        public static void ForgetSeen()
        {
            try
            {
                PlayerPrefs.DeleteKey(SeenKey);
                PlayerPrefs.Save();
            }
            catch { }
        }

        void Start()
        {
            if (!Seen && popup != null)
                StartCoroutine(OpenAfterDelay());
        }

        IEnumerator OpenAfterDelay()
        {
            yield return new WaitForSecondsRealtime(delay);
            if (popup == null || popup.IsOpen)
                yield break;
            try
            {
                PlayerPrefs.SetInt(SeenKey, 1);
                PlayerPrefs.Save();
            }
            catch { }
            popup.Open();
        }
    }
}
