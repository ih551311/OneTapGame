using LastBullet.Audio;
using UnityEngine;

namespace LastBullet.Player
{
    // 힐 스킬을 쓴 순간의 효과음과 연출 (2026-10-31 사용자 요청).
    //  1) 맑은 종소리 (Assets/Audio/Heal)
    //  2) (꺼 둠) 플레이어 주위에서 떠오르는 초록 반짝이. Player 아래 HealParticles가 있으면 켠다.
    // 화면 가장자리 번쩍은 없앴다. 체력이 찬 만큼은 체력바(BattleUI)가 다른 색으로 1초쯤 보여 준다.
    // 붙일 곳: Player (메뉴 LastBullet > Phase 37 힐 효과 넣기)
    public class HealEffect : MonoBehaviour
    {
        [SerializeField] HealSkill skill;
        [SerializeField] SfxPlayer sound;
        [SerializeField] ParticleSystem particles;

        void Start()
        {
            if (skill != null)
                skill.Used += OnUsed;
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
        }
    }
}
