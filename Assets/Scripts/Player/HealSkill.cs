using LastBullet.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastBullet.Player
{
    // 힐 스킬 (2026-10-31 사용자 요청. 기획서 1.2의 "전투 중 체력 회복" 제외를 사용자가 바꿈).
    // F키를 누르면 체력이 healAmount만큼 바로 차고, 다시 쓰려면 cooldown초를 기다려야 한다.
    // 체력이 가득이면 쓰지 않는다 (대기시간을 낭비하지 않게). 전투 맵(전투 가방이 있는 씬)에서만 동작한다.
    // 화면 표시(HealSkillIcon)가 RemainingCooldown을 읽는다.
    // 붙일 곳: Player (메뉴 LastBullet > Phase 36 힐 스킬 만들기)
    public class HealSkill : MonoBehaviour
    {
        [Tooltip("한 번에 채우는 체력 (기본값)")]
        [SerializeField] int healAmount = 30;

        [Tooltip("다시 쓸 수 있을 때까지 걸리는 시간 (초, 기본값)")]
        [SerializeField] float cooldown = 30f;

        public float Cooldown => cooldown;
        public int HealAmount => healAmount;

        // 다시 쓸 수 있을 때까지 남은 시간 (0이면 쓸 수 있다)
        public float RemainingCooldown => Mathf.Max(0f, readyTime - Time.time);

        // 힐을 쓴 순간 (효과·소리를 붙일 자리)
        public event System.Action Used;

        PlayerHealth health;
        float readyTime;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || health == null || health.IsDead)
                return;
            if (RunBag.Current == null || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
                return;

            if (keyboard.fKey.wasPressedThisFrame)
                TryUse();
        }

        public bool TryUse()
        {
            if (RemainingCooldown > 0f)
            {
                Debug.Log($"[HealSkill] 아직 못 써요 ({RemainingCooldown:0.0}초 남음)");
                return false;
            }
            if (health.Heal(healAmount) <= 0)
            {
                Debug.Log("[HealSkill] 체력이 가득이라 쓰지 않음");
                return false;
            }
            readyTime = Time.time + cooldown;
            Used?.Invoke();
            return true;
        }
    }
}
