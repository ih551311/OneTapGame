using System;
using System.Collections;
using LastBullet.Combat;
using LastBullet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBullet.Player
{
    // 플레이어 체력 (기획서 2.3). 최대 100, 전투 중 회복 없음.
    // 회피 무적 중에는 피해를 받지 않는다.
    // 사망하면 조작을 막고 쓰러진 뒤, 잠시 뒤 미술관(1_Museum)으로 돌아간다 (기획서 2.6).
    // Phase 5에서 더할 부분: 사망 시 전투 가방 비우기(RunBag.Clear).
    // 붙일 곳: Player
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] int maxHp = 100;
        [SerializeField] float restartDelay = 2f;

        public int Hp { get; private set; }
        public int MaxHp => maxHp;
        public bool IsDead { get; private set; }

        public event Action Changed; // 체력바가 듣는다
        public event Action Died;

        PlayerMovement movement;

        void Awake()
        {
            Hp = maxHp;
            movement = GetComponent<PlayerMovement>();
        }

        // 피해를 받았으면 true. 회피 무적이거나 이미 죽었으면 false.
        public bool TakeDamage(int damage)
        {
            if (IsDead)
                return false;
            if (movement != null && movement.IsInvincible)
            {
                Debug.Log("[PlayerHealth] 회피 무적으로 피함");
                return false;
            }

            Hp = Mathf.Max(0, Hp - damage);
            Debug.Log($"[PlayerHealth] 피해 {damage} → 체력 {Hp}/{maxHp}");
            Changed?.Invoke();
            if (Hp == 0)
                Die();
            return true;
        }

        void Die()
        {
            IsDead = true;
            Debug.Log("[PlayerHealth] 사망");

            // 조작을 막는다 (카메라는 계속 돌릴 수 있다)
            if (movement != null)
                movement.enabled = false;
            var gun = GetComponent<OneBulletGun>();
            if (gun != null)
                gun.enabled = false;

            // 쓰러진다 (임시 연출)
            transform.rotation *= Quaternion.Euler(-80f, 0f, 0f);

            Died?.Invoke();
            StartCoroutine(RestartAfterDelay());
        }

        IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneNames.Museum);
        }
    }
}
