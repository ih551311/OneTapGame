using LastBullet.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Player
{
    // 마우스로 3인칭 카메라를 돌린다.
    // CameraTarget(플레이어 머리 높이의 빈 오브젝트)을 회전시키면
    // Cinemachine 카메라(PlayerFollowCamera)가 그 뒤를 따라간다.
    // 붙일 곳: Player
    // 실행 순서 50: 같은 LateUpdate에서 몸을 돌리는 AimController(0)보다 뒤, 카메라(CinemachineBrain 100)보다 앞에서 카메라 방향을 정한다.
    // (순서가 정해지지 않으면 몸이 돌 때 카메라 방향이 한 프레임씩 어긋나 떨릴 수 있다)
    [DefaultExecutionOrder(50)]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Tooltip("카메라가 따라갈 대상. Player 아래의 CameraTarget")]
        [SerializeField] Transform cameraTarget;

        [Tooltip("기본 마우스 감도 (마우스 1픽셀당 회전 각도). 옵션의 마우스 감도 배율이 곱해진다")]
        [SerializeField] float sensitivity = 0.1f;

        [Tooltip("위아래로 볼 수 있는 각도 제한")]
        [SerializeField] float minPitch = -40f;
        [SerializeField] float maxPitch = 70f;

        float yaw;   // 좌우 각도
        float pitch; // 위아래 각도

        // 조준할 때 마우스 감도를 낮추는 배율 (AimController가 정한다. 1 = 평소)
        public float SensitivityMultiplier { get; set; } = 1f;

        float shakeAmplitude; // 흔들림 크기 (도)
        float shakeTime;      // 남은 흔들림 시간
        float shakeDuration;

        // 카메라를 잠깐 흔든다 (맞았을 때). 흔들림은 시간이 갈수록 약해진다.
        // 이미 흔들리는 중이면 더 센 쪽을 따른다. 마우스로 정한 방향(yaw, pitch)은 바뀌지 않는다.
        public void Shake(float amplitudeDegrees, float duration)
        {
            float remaining = shakeDuration > 0f ? shakeAmplitude * (shakeTime / shakeDuration) : 0f;
            shakeAmplitude = Mathf.Max(amplitudeDegrees, remaining);
            shakeDuration = shakeTime = duration;
        }

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
                Vector2 delta = mouse.delta.ReadValue() * GameSettings.Sensitivity * SensitivityMultiplier;
                yaw += delta.x * sensitivity;
                pitch -= delta.y * sensitivity; // 마우스를 위로 올리면 위를 본다
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // 플레이어 몸이 돌아도 카메라 방향은 마우스로만 정해지도록 월드 기준으로 설정한다.
            Vector3 shake = Vector3.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float strength = shakeAmplitude * Mathf.Clamp01(shakeTime / shakeDuration);
                shake = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f)) * strength;
            }
            cameraTarget.rotation = Quaternion.Euler(pitch + shake.x, yaw + shake.y, shake.z);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
