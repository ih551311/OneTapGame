using LastBullet.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // E 스킬(물감 폭탄)의 쿨다운 표시 (2026-11-08). 화면 아래 탄약 옆의 둥근 아이콘.
    //  - 쓸 수 있으면 아이콘이 밝고 "E" 글자가 보인다. 쓰고 나면 어두운 막이 시계 방향으로 걷히며 남은 초가 보인다.
    //  - 다시 쓸 수 있게 되는 순간 아이콘이 잠깐 통통 튄다.
    //  - 그림은 Assets/UI/Skill_PaintBomb.png(아이콘), Skill_Cooldown.png(어두운 막, 흰색 원이라 색은 코드가 입힌다)라서 바꿔 끼울 수 있다.
    // 붙일 곳: 전투 맵 Canvas/SkillE (메뉴 LastBullet > Phase 92 물감 폭탄 스킬 만들기)
    public class SkillCooldownUI : MonoBehaviour
    {
        [SerializeField] Image icon;
        [Tooltip("쿨다운 동안 차오르는/걷히는 어두운 막 (Image.Type.Filled, Radial 360)")]
        [SerializeField] Image overlay;
        [SerializeField] TMP_Text secondsText;
        [Tooltip("쓸 수 있을 때만 보이는 키 글자 (E)")]
        [SerializeField] GameObject keyLabel;

        PaintBombSkill skill;
        bool wasReady = true;
        float bounceStart = -10f;

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                skill = player.GetComponent<PaintBombSkill>();
            Refresh();
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            if (skill == null)
                return;
            bool ready = skill.Ready;
            if (ready && !wasReady)
                bounceStart = Time.unscaledTime; // 다시 쓸 수 있게 된 순간
            wasReady = ready;

            if (overlay != null)
            {
                overlay.enabled = !ready;
                overlay.fillAmount = skill.Cooldown > 0f ? skill.Remaining / skill.Cooldown : 0f;
            }
            if (secondsText != null)
            {
                secondsText.enabled = !ready;
                if (!ready)
                    secondsText.text = Mathf.CeilToInt(skill.Remaining).ToString();
            }
            if (keyLabel != null)
                keyLabel.SetActive(ready);
            if (icon != null)
            {
                icon.color = ready ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
                float k = Mathf.Clamp01((Time.unscaledTime - bounceStart) / 0.35f);
                float bounce = k < 1f ? 1f + 0.18f * Mathf.Sin(k * Mathf.PI) : 1f;
                icon.rectTransform.localScale = Vector3.one * bounce;
            }
        }
    }
}
