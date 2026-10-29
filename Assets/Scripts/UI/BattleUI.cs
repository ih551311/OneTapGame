using LastBullet.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 전투 화면 UI. 지금은 플레이어 체력바만 있다.
    // (Phase 5에서 전투 가방 표시를 더한다)
    // 그림: Assets/UI/HealthBar_Back.png, HealthBar_Fill.png (docs/ART_SLOTS.md)
    // 붙일 곳: HUD
    public class BattleUI : MonoBehaviour
    {
        [SerializeField] PlayerHealth playerHealth;

        [Tooltip("Filled(Horizontal) 이미지. 체력 비율만큼 채워진다")]
        [SerializeField] Image healthFill;

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
            healthFill.fillAmount = (float)playerHealth.Hp / playerHealth.MaxHp;
        }
    }
}
