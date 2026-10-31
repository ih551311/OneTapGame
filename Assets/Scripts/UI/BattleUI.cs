using LastBullet.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 화면 UI. 지금은 플레이어 체력바만 있다.
    // 체력이 찰 때(힐)는 늘어난 만큼을 다른 색(HealGain)으로 1초쯤 보여 준 뒤, 원래 체력바가 그 자리까지 차오른다 (2026-10-31 사용자 요청).
    // 그림: Assets/UI/HealthBar_Back.png, HealthBar_Fill.png, HealthBar_Gain.png (docs/ART_SLOTS.md)
    // 붙일 곳: HUD
    public class BattleUI : MonoBehaviour
    {
        [SerializeField] PlayerHealth playerHealth;

        [Tooltip("Filled(Horizontal) 이미지. 체력 비율만큼 채워진다")]
        [SerializeField] Image healthFill;

        [Tooltip("힐로 늘어난 만큼을 다른 색으로 보여 주는 Filled(Horizontal) 이미지. 체력바 채움(Fill) 바로 뒤에 놓는다. 없어도 된다")]
        [SerializeField] Image healGainFill;

        [Tooltip("늘어난 만큼을 다른 색으로 보여 주는 시간 (초)")]
        [SerializeField] float healGainTime = 1f;

        [Tooltip("그 뒤 체력바가 차오르는 빠르기 (1초에 전체의 몇 배)")]
        [SerializeField] float fillSpeed = 1.5f;

        int lastHp = -1;
        float holdTimer;   // 다른 색을 그대로 보여 주는 남은 시간
        bool filling;      // 홀드가 끝나고 체력바가 차오르는 중
        float targetRatio;

        // Start에서 시작한다: PlayerHealth.Awake가 체력을 채운 뒤라야 올바른 값을 읽는다.
        void Start()
        {
            playerHealth.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.Changed -= Refresh;
        }

        void Refresh()
        {
            int hp = playerHealth.Hp;
            float ratio = (float)hp / playerHealth.MaxHp;
            bool healed = lastHp >= 0 && hp > lastHp && healGainFill != null;

            if (healed)
            {
                // 체력바는 예전 체력까지만, 늘어난 만큼은 다른 색으로
                healthFill.fillAmount = (float)lastHp / playerHealth.MaxHp;
                healGainFill.fillAmount = ratio;
                healGainFill.enabled = true;
                targetRatio = ratio;
                holdTimer = healGainTime;
                filling = false;
            }
            else
            {
                healthFill.fillAmount = ratio; // 피해를 받았거나 처음이면 바로 맞춘다
                EndGain();
            }
            lastHp = hp;
        }

        void Update()
        {
            if (healGainFill == null || (holdTimer <= 0f && !filling))
                return;

            float dt = Time.unscaledDeltaTime;
            if (holdTimer > 0f)
            {
                holdTimer -= dt;
                if (holdTimer <= 0f)
                    filling = true;
                return;
            }

            healthFill.fillAmount = Mathf.MoveTowards(healthFill.fillAmount, targetRatio, fillSpeed * dt);
            if (Mathf.Approximately(healthFill.fillAmount, targetRatio))
                EndGain();
        }

        void EndGain()
        {
            holdTimer = 0f;
            filling = false;
            if (healGainFill != null)
                healGainFill.enabled = false;
        }
    }
}
