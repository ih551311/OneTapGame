using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Player
{
    // 마우스 휠로 카메라를 캐릭터 쪽으로 당기거나 밀어서 3인칭 ↔ 1인칭을 바꾼다 (2026-10-31 사용자 요청).
    //  - 휠을 위로 굴리면 가까워지고(1인칭 쪽), 아래로 굴리면 멀어진다(기본 3인칭). 한 칸에 20%씩, 부드럽게 움직인다.
    //  - 거의 끝까지 가까워지면(1인칭) 내 캐릭터 모습을 숨긴다 (그림자는 남는다). 카메라가 머리 높이에 놓인다.
    //  - 1인칭에서는 캐릭터가 늘 카메라가 보는 쪽을 본다 (AimController가 처리한다).
    //  - 카메라 거리·어깨 위치는 AimController가 이 값(Zoom)을 읽어 함께 계산한다. 그래서 조준(우클릭)과 겹쳐도 어긋나지 않는다.
    //  - 마우스 커서가 풀려 있을 때(메뉴·팝업·일시정지)와 죽었을 때는 휠이 먹지 않는다. 죽으면 3인칭으로 돌아가 쓰러진 모습을 보여 준다.
    // 붙일 곳: Player (메뉴 LastBullet > Phase 42 카메라 줌 넣기)
    public class CameraZoom : MonoBehaviour
    {
        [Tooltip("휠 한 칸에 움직이는 양 (0~1)")]
        [SerializeField] float stepPerNotch = 0.2f;

        [Tooltip("움직이는 빠르기 (클수록 빠름)")]
        [SerializeField] float smoothSpeed = 4f;

        [Tooltip("이 값 이상 가까워지면 1인칭으로 보고 캐릭터 모습을 숨긴다")]
        [Range(0.5f, 1f)]
        [SerializeField] float firstPersonThreshold = 0.92f;

        // 맵을 옮겨도 마지막으로 정한 거리를 기억한다
        static float savedTarget;

        // 0 = 기본 3인칭, 1 = 1인칭 (부드럽게 움직이는 현재 값)
        public float Zoom { get; private set; }
        public bool IsFirstPerson => Zoom >= firstPersonThreshold;

        float target;
        PlayerHealth health;
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly List<UnityEngine.Rendering.ShadowCastingMode> originalShadows = new List<UnityEngine.Rendering.ShadowCastingMode>();
        bool hidden;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                    continue; // 모습(몸·옷·무기)만 숨긴다. 총알 선 같은 효과는 그대로
                renderers.Add(r);
                originalShadows.Add(r.shadowCastingMode);
            }
        }

        void Start()
        {
            target = savedTarget;
            Zoom = target;
            ApplyVisibility();
        }

        void OnDisable()
        {
            SetHidden(false);
        }

        void Update()
        {
            bool dead = health != null && health.IsDead;
            if (dead)
                target = 0f; // 쓰러지면 3인칭으로 돌아가 쓰러진 모습을 보여 준다

            var mouse = Mouse.current;
            if (!dead && mouse != null && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0f)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    target = Mathf.Clamp01(target + (scroll > 0f ? stepPerNotch : -stepPerNotch));
                    savedTarget = target;
                }
            }

            Zoom = Mathf.MoveTowards(Zoom, target, smoothSpeed * Time.unscaledDeltaTime);
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            SetHidden(IsFirstPerson);
        }

        void SetHidden(bool hide)
        {
            if (hide == hidden)
                return;
            hidden = hide;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null)
                    continue;
                renderers[i].shadowCastingMode = hide ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : originalShadows[i];
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            savedTarget = 0f;
        }
    }
}
