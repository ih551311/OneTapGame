using LastBullet.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Stage
{
    // 미술관의 상호작용 장치 (지도 탁자, 종이 합성대 등).
    // 가까이 가면 안내("[E] ...")가 뜨고, E를 누르면 연결된 팝업이 열린다.
    // 붙일 곳: 장치 오브젝트 (popup, prompt 연결)
    public class InteractDevice : MonoBehaviour
    {
        [Tooltip("Can be opened with E within this distance (default)")]
        [SerializeField] float range = 2.5f;

        [SerializeField] PopupBase popup;

        [Tooltip("Prompt UI shown when close (Canvas/Prompt_...)")]
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

            // 일시정지 중(Time.timeScale 0)에는 E로 열리지 않게 한다
            if (near && Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                popup.Open();
        }
    }
}
