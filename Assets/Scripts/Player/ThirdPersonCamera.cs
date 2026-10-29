using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Player
{
    // 마우스로 3인칭 카메라를 돌린다.
    // CameraTarget(플레이어 머리 높이의 빈 오브젝트)을 회전시키면
    // Cinemachine 카메라(PlayerFollowCamera)가 그 뒤를 따라간다.
    // 붙일 곳: Player
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Tooltip("카메라가 따라갈 대상. Player 아래의 CameraTarget")]
        [SerializeField] Transform cameraTarget;

        [Tooltip("마우스 감도 (마우스 1픽셀당 회전 각도)")]
        [SerializeField] float sensitivity = 0.1f;

        [Tooltip("위아래로 볼 수 있는 각도 제한")]
        [SerializeField] float minPitch = -40f;
        [SerializeField] float maxPitch = 70f;

        float yaw;   // 좌우 각도
        float pitch; // 위아래 각도

        // 카메라가 보는 좌우 방향. 이동 스크립트가 "카메라 기준 앞"을 정할 때 쓴다.
        public float Yaw => yaw;

        void Start()
        {
            yaw = transform.eulerAngles.y;
            SetCursorLocked(true);
        }

        void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            // 커서가 풀려 있으면(에디터에서 ESC를 눌렀을 때 등) 화면을 클릭해서 다시 잠근다.
            // ESC는 팝업 닫기와 일시정지(Phase 9)에 쓰므로 여기서는 쓰지 않는다.
            if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * sensitivity;
                pitch -= delta.y * sensitivity; // 마우스를 위로 올리면 위를 본다
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // 플레이어 몸이 돌아도 카메라 방향은 마우스로만 정해지도록 월드 기준으로 설정한다.
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
