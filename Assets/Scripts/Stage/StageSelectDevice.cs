using LastBullet.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Stage
{
    // 미술관의 스테이지 선택 장치 (기획서 2.9, 지금은 지도 탁자).
    // 가까이 가면 "[E] 전투 장소 선택" 안내가 뜨고, E를 누르면 선택 팝업이 열린다.
    // 붙일 곳: 미술관(1_Museum)의 StageMap 오브젝트
    public class StageSelectDevice : MonoBehaviour
    {
        [Tooltip("이 거리 안에서 E로 열 수 있다 (기본값)")]
        [SerializeField] float range = 2.5f;

        [SerializeField] StageSelectPopup popup;

        [Tooltip("가까이 가면 켜지는 안내 UI (Canvas/Prompt_StageSelect)")]
        [SerializeField] GameObject prompt;

        Transform player;

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            prompt.SetActive(false);
        }

        void Update()
        {
            if (player == null)
                return;

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            bool near = offset.magnitude <= range && !popup.IsOpen;
            prompt.SetActive(near);

            if (near && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                popup.Open();
        }
    }
}
