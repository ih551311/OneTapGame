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

        [SerializeField] float flashFadeTime = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] float flashStrength = 0.6f;

        float flashAlpha;

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
            flashAlpha = flashStrength;
        }

        void Update()
        {
            if (flashAlpha <= 0f)
                return;
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * flashStrength / flashFadeTime);
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
