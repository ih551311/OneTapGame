using System;
using System.Collections;
using LastBullet.Combat;
using LastBullet.Core;
using LastBullet.Stage;
using LastBullet.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBullet.Player
{
    // 플레이어 체력 (기획서 2.3). 최대 100, 전투 중 회복 없음.
    // 회피 무적 중에는 피해를 받지 않는다.
    // 사망하면 조작을 막고 쓰러진 뒤, 잠시 뒤 결과 창(RunResultPopup)을 보여 주고 [미술관으로]로 미술관(1_Museum)에 돌아간다 (기획서 2.6).
    // 사망하면 이번 판 재료(전투 가방)를 모두 잃는다 (RunBag.Clear).
    // 붙일 곳: Player
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] int maxHp = 100;
        [SerializeField] float restartDelay = 2f;

        public int Hp { get; private set; }
        public int MaxHp => maxHp;
        public bool IsDead { get; private set; }

        public event Action Changed; // 체력바가 듣는다
        public event Action<int> Damaged; // 피해를 받을 때 (피해량). 피격 효과(HitFeedback)가 듣는다
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
            Damaged?.Invoke(damage);
            if (Hp == 0)
                Die();
            return true;
        }

        void Die()
        {
            IsDead = true;
            Debug.Log("[PlayerHealth] 사망");
            // 잃는 양을 결과 창에 보여 주려고 비우기 전에 요약을 받아 둔다
            RunSummary lost = RunBag.Current != null ? RunBag.Current.Summary() : default;
            if (RunBag.Current != null)
                RunBag.Current.Clear();

            // 조작을 막는다 (카메라는 계속 돌릴 수 있다)
            if (movement != null)
                movement.enabled = false;
            var gun = GetComponent<OneBulletGun>();
            if (gun != null)
                gun.enabled = false;

            // 쓰러진다 (임시 연출)
            transform.rotation *= Quaternion.Euler(-80f, 0f, 0f);

            Died?.Invoke();
            StartCoroutine(ShowResultAfterDelay(lost));
        }

        // 쓰러진 뒤 잠시 있다가 "쓰러졌어요" 결과 창(잃은 재료)을 보여 준다. [미술관으로]를 누르면 미술관으로 돌아간다.
        // 결과 창이 없는 씬에서는 예전처럼 바로 미술관으로 간다.
        IEnumerator ShowResultAfterDelay(RunSummary lost)
        {
            yield return new WaitForSeconds(restartDelay);
            if (RunResultPopup.Current != null)
                RunResultPopup.Current.Show(lost, true);
            else
                SceneManager.LoadScene(SceneNames.Museum);
        }
    }
}
