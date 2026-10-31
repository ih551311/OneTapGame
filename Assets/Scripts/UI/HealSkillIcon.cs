using LastBullet.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 화면의 힐 스킬 칸. 대기 중에는 어두운 막이 시계 방향으로 걷히고 남은 초가 보인다. 쓸 수 있으면 막과 숫자가 사라진다.
    // 그림은 Assets/UI/Skill_Heal.png(칸), Skill_Heal_Cool.png(대기 막)이라 바꿔 끼울 수 있다.
    // 붙일 곳: Canvas/HealSkill (메뉴 LastBullet > Phase 36 힐 스킬 만들기)
    public class HealSkillIcon : MonoBehaviour
    {
        [SerializeField] Image cooldownFill;
        [SerializeField] TMP_Text secondsText;

        HealSkill skill;

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                skill = player.GetComponent<HealSkill>();
            Refresh();
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            float remaining = skill != null ? skill.RemainingCooldown : 0f;
            bool waiting = remaining > 0f;
            cooldownFill.enabled = waiting;
            secondsText.enabled = waiting;
            if (waiting)
            {
                cooldownFill.fillAmount = remaining / skill.Cooldown;
                secondsText.text = Mathf.CeilToInt(remaining).ToString();
            }
        }
    }
}
