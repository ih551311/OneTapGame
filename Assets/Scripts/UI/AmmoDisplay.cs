using LastBullet.Combat;
using TMPro;
using UnityEngine;

namespace LastBullet.UI
{
    // 화면 왼쪽 아래의 탄약 표시 (2026-11-04, 데스티니식). 총 아이콘 + 큰 숫자(탄창에 남은 탄) + 작은 ∞(예비 탄은 무한).
    //  - 탄이 적으면(탄창의 25% 이하) 빨갛게, 재장전 중에는 숫자 아래에 가는 진행 막대가 차오른다 (글자 없음. 예전에는 "재장전 62%" 글자였다).
    //  - 글자는 TextMeshPro(글꼴 font1). 판 그림은 Assets/UI/Hud_Name.png라서 바꿔 끼울 수 있다.
    // 붙일 곳: Canvas/AmmoDisplay (메뉴 LastBullet > Phase 63 데스티니식 전투 화면 만들기)
    public class AmmoDisplay : MonoBehaviour
    {
        [Tooltip("남은 탄 / 탄창 크기 글자")]
        [SerializeField] TMP_Text ammoText;

        [Tooltip("(옛 방식) 재장전 중에만 보이는 글자. 진행 막대(reloadBarRoot)를 연결하면 쓰지 않는다")]
        [SerializeField] TMP_Text statusText;

        [Tooltip("재장전 중에만 보이는 진행 막대 판 (숫자 아래의 가는 막대. 2026-11-08 사용자 요청: 글자 대신 막대)")]
        [SerializeField] GameObject reloadBarRoot;

        [Tooltip("막대가 차오르는 채움 (Image.Type.Filled, 가로)")]
        [SerializeField] UnityEngine.UI.Image reloadFill;

        [SerializeField] Color normalColor = new Color(0.93f, 0.9f, 0.85f, 1f);
        [SerializeField] Color lowColor = new Color(1f, 0.35f, 0.3f, 1f);

        AutoGun gun;

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                gun = player.GetComponent<AutoGun>();
            Refresh();
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            if (gun == null)
                return;
            // 큰 숫자 = 탄창에 남은 탄, 작은 ∞ = 예비 탄(무한)
            ammoText.text = $"{gun.Ammo}<size=40%> ∞</size>";
            bool low = gun.Ammo <= Mathf.Max(1, gun.MagazineSize / 4);
            ammoText.color = low ? lowColor : normalColor;

            if (reloadBarRoot != null)
            {
                // 재장전 중에만 숫자 아래 막대가 0 → 100%로 차오른다 (글자 없음)
                reloadBarRoot.SetActive(gun.IsReloading);
                if (gun.IsReloading && reloadFill != null)
                    reloadFill.fillAmount = Mathf.Clamp01(gun.ReloadProgress);
            }
            else if (statusText != null)
            {
                statusText.enabled = gun.IsReloading;
                if (gun.IsReloading)
                    statusText.text = $"재장전 {Mathf.RoundToInt(gun.ReloadProgress * 100f)}%";
            }
        }
    }
}
