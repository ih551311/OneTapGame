using System;
using LastBullet.Stage;
using UnityEngine;

namespace LastBullet.Player
{
    // 적을 처치하면 체력이 조금씩 찬다 (2026-10-31 사용자 요청. 버튼으로 쓰는 힐 스킬(HealSkill)을 없애고 이걸로 바꿨다).
    // 기획서 1.2의 "전투 중 체력 회복" 제외를 사용자가 바꾼 것이다.
    // 헤드샷 처치는 headshotHeal, 몸샷 처치는 bodyHeal만큼 찬다 (기본값). 체력이 가득이면 아무 일도 없다.
    // 전투 맵(전투 가방이 있는 씬)에서만 동작한다. 적의 EnemyDrop이 처치 때 OnEnemyKilled를 부른다.
    // 체력바(BattleUI)는 찬 만큼을 다른 색으로 1초쯤 보여 주고, HealEffect가 소리를 낸다.
    // 붙일 곳: Player (메뉴 LastBullet > Phase 45 처치 회복 넣기)
    public class KillHeal : MonoBehaviour
    {
        [Tooltip("Health restored on a headshot kill (default)")]
        [SerializeField] int headshotHeal = 10;

        [Tooltip("Health restored on a body shot kill (default)")]
        [SerializeField] int bodyHeal = 5;

        public static KillHeal Current { get; private set; }

        // 체력이 실제로 찬 순간 (찬 양). 효과·소리를 붙일 자리
        public event Action<int> Healed;

        PlayerHealth health;

        void Awake()
        {
            Current = this;
            health = GetComponent<PlayerHealth>();
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        public void OnEnemyKilled(bool headshot)
        {
            if (health == null || health.IsDead || RunBag.Current == null)
                return;
            int healed = health.Heal(headshot ? headshotHeal : bodyHeal);
            if (healed > 0)
                Healed?.Invoke(healed);
        }
    }
}
