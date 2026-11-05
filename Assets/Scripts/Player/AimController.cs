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

        [Header("움직임 느낌 (2026-11-06 사용자 요청: 미술관)")]
        [Tooltip("켜면 걸을 때 시점이 살짝 흔들리고(1인칭에서 가장 크게), 달리면 시야가 넓어진다. 미술관만 켠다")]
        [SerializeField] bool movementFeel;
        [Tooltip("달릴 때 시야각이 더 넓어지는 양 (도)")]
        [SerializeField] float runFovBonus = 9f;
        [Tooltip("걸을 때 시점이 위아래로 흔들리는 폭 (m, 1인칭 기준)")]
        [SerializeField] float bobHeight = 0.045f;
        [Tooltip("걸을 때 시점이 좌우로 흔들리는 폭 (m, 1인칭 기준)")]
        [SerializeField] float bobSway = 0.03f;
        [Tooltip("한 걸음 거리(1m)당 흔들림 박자 수. 클수록 빠르게 흔들린다")]
        [SerializeField] float bobCyclesPerMeter = 0.37f;

        float normalFov, normalDistance, normalShoulderX, normalShoulderY, normalShoulderZ, normalArm;
        float amount; // 0 = 평소, 1 = 완전히 조준
        CameraZoom zoom; // 휠로 3인칭 ↔ 1인칭 (없으면 늘 3인칭)
        PlayerMovement movement;
        CharacterController controller;
        float bobPhase, moveAmount, runBlend;

        public bool IsAiming => amount > 0.5f;

        void Start()
        {
            normalFov = followCamera.Lens.FieldOfView;
            normalDistance = follow.CameraDistance;
            normalShoulderX = follow.ShoulderOffset.x;
            normalShoulderY = follow.ShoulderOffset.y;
            normalShoulderZ = follow.ShoulderOffset.z;
            normalArm = follow.VerticalArmLength;
            zoom = GetComponent<CameraZoom>();
            movement = GetComponent<PlayerMovement>();
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            bool want = WantsToAim();
            amount = Mathf.MoveTowards(amount, want ? 1f : 0f, Time.unscaledDeltaTime * blendSpeed * 0.25f);
            float eased = amount * amount * (3f - 2f * amount); // 부드럽게 시작하고 끝난다

            // 휠로 정한 카메라 자리(0 = 기본, 1 = 머리 높이의 1인칭)가 바탕이고, 조준은 그 위에 더해진다.
            // 1인칭에서는 조준해도 카메라가 뒤로 물러나지 않는다.
            float z = zoom != null ? zoom.Zoom : 0f;
            float baseDistance = Mathf.Lerp(normalDistance, 0f, z);
            float baseShoulderX = Mathf.Lerp(normalShoulderX, 0f, z);

            // 움직임 느낌: 걸음 박자에 맞춘 흔들림, 달릴 때 넓어지는 시야 (조준 중에는 줄인다)
            float bobX = 0f, bobY = 0f, runFov = 0f;
            if (movementFeel && controller != null)
            {
                var velocity = controller.velocity;
                velocity.y = 0f;
                float speed = controller.isGrounded && Time.timeScale > 0f ? velocity.magnitude : 0f;
                moveAmount = Mathf.MoveTowards(moveAmount, Mathf.Clamp01(speed / 3f), Time.unscaledDeltaTime * 5f);
                bobPhase += speed * bobCyclesPerMeter * 6.2832f * Time.deltaTime;
                float scale = Mathf.Lerp(0.25f, 1f, z) * (1f - eased * 0.8f); // 3인칭은 아주 약하게, 1인칭에서 가장 크게
                bobY = Mathf.Sin(bobPhase * 2f) * bobHeight * moveAmount * scale; // 한 걸음마다 한 번 출렁
                bobX = Mathf.Sin(bobPhase) * bobSway * moveAmount * scale;        // 좌우는 두 걸음에 한 번
                bool running = movement != null && movement.IsRunning && Time.timeScale > 0f;
                runBlend = Mathf.MoveTowards(runBlend, running ? 1f : 0f, Time.unscaledDeltaTime * 3f);
                float smoothRun = runBlend * runBlend * (3f - 2f * runBlend);
                runFov = runFovBonus * smoothRun * (1f - eased);
            }

            var lens = followCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(normalFov + runFov, aimFieldOfView, eased);
            followCamera.Lens = lens;
            follow.CameraDistance = Mathf.Lerp(baseDistance, Mathf.Min(baseDistance, aimDistance), eased);
            follow.VerticalArmLength = Mathf.Lerp(normalArm, 0f, z);
            var shoulder = follow.ShoulderOffset;
            shoulder.x = Mathf.Lerp(baseShoulderX, Mathf.Lerp(aimShoulderX, 0f, z), eased) + bobX;
            shoulder.y = Mathf.Lerp(normalShoulderY, 0f, z) + bobY;
            shoulder.z = Mathf.Lerp(normalShoulderZ, 0f, z);
            follow.ShoulderOffset = shoulder;

            look.SensitivityMultiplier = Mathf.Lerp(1f, aimSensitivity, eased);
        }

        // 조준 중이거나 1인칭이면 캐릭터가 카메라가 보는 쪽을 본다 (이동 스크립트가 돌린 뒤에 덮어쓴다)
        void LateUpdate()
        {
            bool firstPerson = zoom != null && zoom.IsFirstPerson && Time.timeScale > 0f && (health == null || !health.IsDead);
            if (!firstPerson && (amount <= 0.05f || !WantsToAim()))
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
            follow.VerticalArmLength = normalArm;
            follow.ShoulderOffset = new Vector3(normalShoulderX, normalShoulderY, normalShoulderZ);
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
