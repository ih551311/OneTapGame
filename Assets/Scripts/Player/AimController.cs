using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Player
{
    // 우클릭 조준 (2026-10-31 사용자 요청). 우클릭을 누르고 있는 동안:
    //  - 카메라가 부드럽게 확대된다 (시야각 줄이고, 캐릭터 쪽으로 다가간다)
    //  - 마우스 감도가 낮아져 정밀하게 겨눌 수 있다
    //  - 캐릭터가 카메라가 보는 방향을 바라본다 (걸어도 앞을 보며 움직인다)
    // 놓으면 원래대로 돌아온다. 마우스 커서가 풀려 있을 때(메뉴·팝업·일시정지)와 죽었을 때는 조준하지 않는다.
    // 쏘는 방식(화면 가운데로 겨눔)은 그대로라서, 확대되면 멀리 있는 적의 머리를 맞히기 쉬워진다.
    // 붙일 곳: Player (메뉴 LastBullet > Phase 30 우클릭 조준 넣기)
    public class AimController : MonoBehaviour
    {
        [SerializeField] CinemachineCamera followCamera;
        [SerializeField] CinemachineThirdPersonFollow follow;
        [SerializeField] ThirdPersonCamera look;
        [SerializeField] PlayerHealth health;

        [Header("조준할 때")]
        [Tooltip("시야각 (평소 60)")]
        [SerializeField] float aimFieldOfView = 38f;
        [Tooltip("캐릭터와 카메라 사이 거리 (평소 3.5)")]
        [SerializeField] float aimDistance = 2.9f;
        [Tooltip("어깨 옆 거리 (평소 0.6). 조준점이 가운데에 오도록 조금 더 옆으로")]
        [SerializeField] float aimShoulderX = 1.15f;
        [Tooltip("마우스 감도 배율 (1 = 평소)")]
        [Range(0.1f, 1f)]
        [SerializeField] float aimSensitivity = 0.5f;
        [Tooltip("확대·복귀 빠르기 (클수록 빠름)")]
        [SerializeField] float blendSpeed = 12f;
        [Tooltip("조준 중 캐릭터가 카메라 방향으로 도는 속도 (도/초)")]
        [SerializeField] float turnSpeed = 720f;

        float normalFov, normalDistance, normalShoulderX;
        float amount; // 0 = 평소, 1 = 완전히 조준

        public bool IsAiming => amount > 0.5f;

        void Start()
        {
            normalFov = followCamera.Lens.FieldOfView;
            normalDistance = follow.CameraDistance;
            normalShoulderX = follow.ShoulderOffset.x;
        }

        void Update()
        {
            bool want = WantsToAim();
            amount = Mathf.MoveTowards(amount, want ? 1f : 0f, Time.unscaledDeltaTime * blendSpeed * 0.25f);
            float eased = amount * amount * (3f - 2f * amount); // 부드럽게 시작하고 끝난다

            var lens = followCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(normalFov, aimFieldOfView, eased);
            followCamera.Lens = lens;
            follow.CameraDistance = Mathf.Lerp(normalDistance, aimDistance, eased);
            var shoulder = follow.ShoulderOffset;
            shoulder.x = Mathf.Lerp(normalShoulderX, aimShoulderX, eased);
            follow.ShoulderOffset = shoulder;

            look.SensitivityMultiplier = Mathf.Lerp(1f, aimSensitivity, eased);
        }

        // 조준 중에는 캐릭터가 카메라가 보는 쪽을 본다 (이동 스크립트가 돌린 뒤에 덮어쓴다)
        void LateUpdate()
        {
            if (amount <= 0.05f || !WantsToAim())
                return;
            var target = Quaternion.Euler(0f, look.Yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.unscaledDeltaTime);
        }

        void OnDisable()
        {
            if (followCamera == null)
                return;
            var lens = followCamera.Lens;
            lens.FieldOfView = normalFov;
            followCamera.Lens = lens;
            follow.CameraDistance = normalDistance;
            var shoulder = follow.ShoulderOffset;
            shoulder.x = normalShoulderX;
            follow.ShoulderOffset = shoulder;
            if (look != null)
                look.SensitivityMultiplier = 1f;
        }

        bool WantsToAim()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed)
                return false;
            if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f)
                return false; // 메뉴·팝업·일시정지
            return health == null || !health.IsDead;
        }
    }
}
