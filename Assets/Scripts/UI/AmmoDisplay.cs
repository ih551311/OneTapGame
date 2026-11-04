using LastBullet.Combat;
using TMPro;
using UnityEngine;

namespace LastBullet.UI
{
    // 화면 왼쪽 아래의 탄약 표시 (2026-11-04, 데스티니식). 총 아이콘 + 큰 숫자(탄창에 남은 탄) + 작은 ∞(예비 탄은 무한).
    //  - 탄이 적으면(탄창의 25% 이하) 빨갛게, 재장전 중에는 "재장전" 글자와 진행 %를 보여 준다.
    //  - 글자는 TextMeshPro(글꼴 font1). 판 그림은 Assets/UI/Hud_Name.png라서 바꿔 끼울 수 있다.
    // 붙일 곳: Canvas/AmmoDisplay (메뉴 LastBullet > Phase 59 연사 총으로 바꾸기)
    public class AmmoDisplay : MonoBehaviour
    {
        [Tooltip("남은 탄 / 탄창 크기 글자")]
        [SerializeField] TMP_Text ammoText;

        [Tooltip("재장전 중에만 보이는 글자")]
        [SerializeField] TMP_Text statusText;

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

            if (statusText != null)
            {
                statusText.enabled = gun.IsReloading;
                if (gun.IsReloading)
                    statusText.text = $"재장전 {Mathf.RoundToInt(gun.ReloadProgress * 100f)}%";
            }
        }
    }
}
