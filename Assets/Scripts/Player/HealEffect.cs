using LastBullet.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.Player
{
    // 힐 스킬을 쓴 순간의 효과음과 연출 (2026-10-31 사용자 요청).
    //  1) 맑은 종소리 (Assets/Audio/Heal)
    //  2) 플레이어 주위에서 초록 반짝이가 위로 떠오른다 (Player 아래 HealParticles)
    //  3) 화면 가장자리가 초록으로 번쩍이고 서서히 사라진다 (Canvas의 HealFlash, 그림 Assets/UI/Vignette_Heal.png)
    // 붙일 곳: Player (메뉴 LastBullet > Phase 37 힐 효과 넣기)
    public class HealEffect : MonoBehaviour
    {
        [SerializeField] HealSkill skill;
        [SerializeField] SfxPlayer sound;
        [SerializeField] ParticleSystem particles;
        [SerializeField] Image flash;

        [Tooltip("번쩍이 가장 진해질 때까지 걸리는 시간 (초). 갑자기 켜지지 않고 부드럽게 올라온다")]
        [SerializeField] float flashRiseTime = 0.18f;
        [Tooltip("진한 상태에서 사라질 때까지 걸리는 시간 (초)")]
        [SerializeField] float flashFadeTime = 1.1f;
        [Range(0f, 1f)]
        [SerializeField] float flashStrength = 0.75f;

        float flashAlpha;
        bool rising;

        void Start()
        {
            if (skill != null)
                skill.Used += OnUsed;
            SetFlash(0f);
        }

        void OnDestroy()
        {
            if (skill != null)
                skill.Used -= OnUsed;
        }

        void OnUsed()
        {
            if (sound != null)
                sound.Play();
            if (particles != null)
                particles.Play();
            rising = true;
        }

        void Update()
        {
            if (!rising && flashAlpha <= 0f)
                return;
            float dt = Time.unscaledDeltaTime;
            if (rising)
            {
                flashAlpha = Mathf.MoveTowards(flashAlpha, flashStrength, dt * flashStrength / flashRiseTime);
                if (flashAlpha >= flashStrength)
                    rising = false;
            }
            else
                flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * flashStrength / flashFadeTime);
            SetFlash(flashAlpha);
        }

        void SetFlash(float alpha)
        {
            if (flash == null)
                return;
            var color = flash.color;
            color.a = alpha;
            flash.color = color;
        }
    }
}
