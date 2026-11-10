using LastBullet.Drawing;
using LastBullet.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Stage
{
    // 결말 보기 (2026-11-11 사용자 결정): 미술관 액자 9칸을 서로 다른 그림으로 다 채우면, 할아버지 일기장 책상 앞에서 "[F] 결말 보기"가 따로 뜬다.
    //  - F를 누르면 결말 창(EndingPopup, 직접 쪽을 넘기는 이야기 창 StoryPopup)이 열린다. 일기장 읽기([E])와는 따로다.
    //  - 9칸을 다 채우지 못했으면 안내도 안 뜨고 F도 듣지 않는다. 그림을 내려서 9개가 안 되면 다시 사라진다.
    //  - 결말 글은 Assets/UI/0_EndingPages.txt 를 메모장으로 고친다 (줄에 --- 만 쓰면 쪽이 나뉜다).
    // 붙일 곳: 미술관 GrandpaDesk (메뉴 LastBullet > Phase 94 결말 보기 만들기)
    public class EndingDevice : MonoBehaviour
    {
        [Tooltip("이 거리 안에서 F로 열 수 있다 (일기장 [E]와 같은 거리)")]
        [SerializeField] float range = 2.5f;

        [Tooltip("결말 창 (Canvas/EndingPopup)")]
        [SerializeField] PopupBase popup;

        [Tooltip("가까이 가면 켜지는 안내 (Canvas/Prompt_Ending, \"[F] 결말 보기\")")]
        [SerializeField] GameObject prompt;

        [Tooltip("같은 책상의 다른 창(일기장)이 열려 있으면 안내를 숨긴다")]
        [SerializeField] PopupBase otherPopup;

        Transform player;

        // 결말을 볼 수 있는가: 액자 9칸에 서로 다른 그림이 다 걸렸을 때
        public static bool Unlocked => GalleryStore.DistinctCount() >= GalleryStore.SlotCount;

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            if (prompt != null)
                prompt.SetActive(false);
        }

        void Update()
        {
            if (player == null || popup == null || prompt == null)
                return;
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            bool busy = popup.IsOpen || (otherPopup != null && otherPopup.IsOpen) || PopupBase.AnyOpen;
            bool near = offset.magnitude <= range && !busy && Unlocked;
            prompt.SetActive(near);

            // 일시정지 중(Time.timeScale 0)에는 열리지 않게 한다
            if (near && Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                popup.Open();
        }
    }
}
