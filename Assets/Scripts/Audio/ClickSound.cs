using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Audio
{
    // 마우스로 클릭할 때마다 클릭 소리 (2026-10-31 사용자 요청). 총을 쏠 때는 나지 않는다.
    // 구분 방법: 게임 중에는 마우스 커서가 잠겨 있고 좌클릭이 사격이다. 커서가 풀려 있을 때(메뉴, 팝업, 그림판, 결과 창,
    // 타이틀 등) 좌클릭만 클릭 소리를 낸다. 그래서 어떤 버튼·창을 눌러도 소리가 나고, 사격에는 소리가 겹치지 않는다.
    // 소리는 같은 오브젝트의 SfxPlayer(변형 중 무작위 + 음높이 살짝 흔들기)가 낸다.
    // 붙일 곳: 모든 씬의 ClickSound 오브젝트 (SfxPlayer, AudioSource와 함께. 메뉴 LastBullet > Phase 22 클릭 소리 넣기)
    [RequireComponent(typeof(SfxPlayer))]
    public class ClickSound : MonoBehaviour
    {
        SfxPlayer sfx;

        void Awake()
        {
            sfx = GetComponent<SfxPlayer>();
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;
            if (Cursor.lockState == CursorLockMode.Locked)
                return; // 커서가 잠겨 있으면 사격이다
            sfx.Play();
        }
    }
}
