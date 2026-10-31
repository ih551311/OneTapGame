using LastBullet.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 피격 효과 (2026-10-31 사용자 요청).
    //  1) 맞으면 화면 가장자리가 빨갛게 번쩍이고 서서히 사라진다. 많이 맞을수록 더 세게 번쩍인다.
    //  2) 맞으면 카메라가 살짝 흔들린다 (ThirdPersonCamera.Shake).
    //  3) 체력이 낮으면(기본 35% 아래) 화면 테두리가 어둡고 붉게 계속 덮이고, 체력이 낮을수록 짙어지며 심장 뛰듯 살짝 맥동한다.
    // 그림: Assets/UI/Vignette_Hit.png(번쩍), Vignette_LowHealth.png(저체력 테두리). 둘 다 화면 전체에 늘려 쓴다.
    // 붙일 곳: 전투 맵 Canvas의 HitFeedback (자식 Flash, LowHealth 이미지)
    public class HitFeedback : MonoBehaviour
    {
        [SerializeField] PlayerHealth playerHealth;
        [SerializeField] ThirdPersonCamera cameraRig;

        [Tooltip("Red border that flashes when hit")]
        [SerializeField] Image flash;

        [Tooltip("Dark border that stays visible when health is low")]
        [SerializeField] Image lowHealth;

        [Header("Flash")]
        [SerializeField] float flashFadeTime = 0.5f;
        [Tooltip("Flash strength for 20 damage (0 to 1)")]
        [SerializeField] float flashStrength = 0.65f;

        [Header("Camera shake")]
        [Tooltip("Shake size = base + damage x multiplier (degrees)")]
        [SerializeField] float shakeBase = 0.8f;
        [SerializeField] float shakePerDamage = 0.08f;
        [SerializeField] float shakeTime = 0.3f;

        [Header("Low health border")]
        [Tooltip("The border appears when the health ratio is below this value")]
        [Range(0f, 1f)]
        [SerializeField] float lowHealthThreshold = 0.35f;
        [Tooltip("Maximum border opacity at the lowest health (near 0)")]
        [Range(0f, 1f)]
        [SerializeField] float lowHealthMaxAlpha = 0.85f;
        [Tooltip("Pulse speed (rotations per second x 2pi)")]
        [SerializeField] float pulseSpeed = 4f;

        float flashAlpha;

        void Start()
        {
            playerHealth.Damaged += OnDamaged;
            SetAlpha(flash, 0f);
            SetAlpha(lowHealth, 0f);
        }

        void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.Damaged -= OnDamaged;
        }

        void OnDamaged(int damage)
        {
            flashAlpha = Mathf.Max(flashAlpha, Mathf.Clamp01(flashStrength * (0.55f + damage / 20f * 0.45f)));
            if (cameraRig != null)
                cameraRig.Shake(shakeBase + damage * shakePerDamage, shakeTime);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt / flashFadeTime);
            SetAlpha(flash, flashAlpha);

            // 체력이 낮을수록 짙다: 임계값 아래로 내려가면 바로 눈에 띄는 정도(50%)로 나타나고, 체력 0에서 최대
            float ratio = (float)playerHealth.Hp / playerHealth.MaxHp;
            float need = ratio >= lowHealthThreshold ? 0f : Mathf.Lerp(0.5f, 1f, 1f - ratio / lowHealthThreshold);
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            SetAlpha(lowHealth, need * lowHealthMaxAlpha * pulse);
        }

        static void SetAlpha(Image image, float alpha)
        {
            if (image == null)
                return;
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
