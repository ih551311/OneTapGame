using LastBullet.Combat;
using UnityEngine;

namespace LastBullet.Player
{
    // 1인칭 총 (2026-10-31 사용자 요청): 마우스 휠로 1인칭이 되면 화면 오른쪽 아래에 내 총이 보인다.
    //  - 3인칭에서는 숨겨져 있고, 1인칭에 가까워지면(Zoom 0.75~0.95) 아래에서 올라오며 나타난다. 죽으면 숨는다.
    //  - 쏘면 뒤로 튕겼다가 돌아온다 (반동). 걸으면 살짝 흔들린다. 우클릭 조준 중에는 화면 가운데 쪽으로 모인다.
    //  - 모양은 Model 아래의 도형(임시 신호총)이고, 겉모습은 이 오브젝트의 자식 Model만 바꾸면 된다 (모델 교체 자리).
    // 카메라(Main Camera)의 자식이라서 카메라가 움직이는 대로 정확히 따라다닌다. 그림자는 만들지 않는다.
    // 붙일 곳: Main Camera 아래의 FirstPersonGun (메뉴 LastBullet > Phase 48 1인칭 총 만들기)
    public class FirstPersonGun : MonoBehaviour
    {
        [SerializeField] CameraZoom zoom;
        [SerializeField] OneBulletGun gun;
        [SerializeField] PlayerHealth health;
        [SerializeField] AimController aim;
        [SerializeField] CharacterController controller;

        [Tooltip("겉모습 (Model 자식). 1인칭이 아닐 때 이 아래의 렌더러를 끈다")]
        [SerializeField] Transform model;

        [Header("자리 (카메라 기준, 기본값)")]
        [SerializeField] Vector3 restPosition = new Vector3(0.2f, -0.21f, 0.6f);
        [SerializeField] Vector3 aimPosition = new Vector3(0.09f, -0.17f, 0.55f);
        [SerializeField] float restYaw = -4f;

        [Header("움직임 (기본값)")]
        [Tooltip("쏠 때 뒤로 밀리는 거리 (m)")]
        [SerializeField] float recoilBack = 0.07f;
        [Tooltip("쏠 때 총구가 들리는 각도 (도)")]
        [SerializeField] float recoilPitch = 7f;
        [Tooltip("반동이 돌아오는 빠르기 (클수록 빠름)")]
        [SerializeField] float recoilRecover = 14f;
        [Tooltip("걸을 때 흔들리는 크기 (m)")]
        [SerializeField] float bobAmount = 0.006f;
        [Tooltip("자리를 옮기는 빠르기 (조준 전환)")]
        [SerializeField] float moveSpeed = 12f;

        Renderer[] renderers;
        float recoil;       // 1 = 방금 쏨, 0 = 제자리
        float bobPhase;
        float aimBlend;     // 0 = 평소, 1 = 조준
        bool shown = true;

        void Awake()
        {
            renderers = model != null ? model.GetComponentsInChildren<Renderer>(true) : GetComponentsInChildren<Renderer>(true);
        }

        void OnEnable()
        {
            if (gun != null)
                gun.Fired += OnFired;
        }

        void OnDisable()
        {
            if (gun != null)
                gun.Fired -= OnFired;
        }

        void OnFired()
        {
            recoil = 1f;
        }

        void LateUpdate()
        {
            bool dead = health != null && health.IsDead;
            // 1인칭에 가까워질수록 0 → 1 (올라오며 나타난다)
            float t = dead || zoom == null ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 0.95f, zoom.Zoom));
            SetShown(t > 0.001f);
            if (!shown)
                return;

            aimBlend = Mathf.MoveTowards(aimBlend, aim != null && aim.IsAiming ? 1f : 0f, Time.unscaledDeltaTime * moveSpeed * 0.5f);
            recoil = Mathf.MoveTowards(recoil, 0f, Time.unscaledDeltaTime * recoilRecover * Mathf.Max(0.3f, recoil));

            float speed = 0f;
            if (controller != null)
            {
                var velocity = controller.velocity;
                velocity.y = 0f;
                speed = velocity.magnitude;
            }
            bobPhase += speed * Time.deltaTime * 1.6f;
            float bob = Mathf.Clamp01(speed / 5f) * (1f - aimBlend * 0.7f);
            var sway = new Vector3(Mathf.Sin(bobPhase) * bobAmount, -Mathf.Abs(Mathf.Cos(bobPhase)) * bobAmount, 0f) * bob;

            Vector3 position = Vector3.Lerp(restPosition, aimPosition, aimBlend) + sway;
            position += new Vector3(0f, -(1f - t) * 0.35f, -recoil * recoilBack);
            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(-recoil * recoilPitch, restYaw * (1f - aimBlend), 0f);
        }

        void SetShown(bool show)
        {
            if (show == shown)
                return;
            shown = show;
            foreach (var r in renderers)
                if (r != null)
                    r.enabled = show;
        }
    }
}
