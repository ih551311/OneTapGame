using LastBullet.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 무기 슬롯 표시 (2026-11-11 사용자 요청: 지금 어떤 무기가 있는지 화면에서 보이게). 전투 화면 왼쪽 아래, 탄약 숫자 위의 작은 칸 두 개.
    //  - 칸 1 = 소총(키 1), 칸 2 = 바주카(키 2). 칸마다 무기 그림과 키 번호가 있고, 지금 들고 있는 무기의 칸은 밝고 조금 크며 "들고 있음" 테두리 그림으로 바뀐다.
    //  - 무기를 바꾸면 새로 고른 칸이 통통 튄다.
    //  - 칸 그림은 Assets/UI/Slot_Back.png(보통), Slot_Selected.png(들고 있음) — 9-slice라서 늘려도 모서리가 안 깨지고, 같은 이름으로 덮어써서 바꿀 수 있다.
    //    무기 그림은 소총 Weapon_Icon.png, 바주카 Weapon_Bazooka.png (탄약 칸 옆 그림과 같은 것).
    //  - 무기가 늘어나면 칸을 복사해서 slots 목록에 넣고 AutoGun 쪽 무기 번호만 맞추면 된다 (지금은 소총·바주카 둘뿐이라 IsBazooka로 가른다).
    // 붙일 곳: Canvas/WeaponSlots (메뉴 LastBullet > Phase 97 무기 슬롯 표시 만들기)
    public class WeaponSlotsUI : MonoBehaviour
    {
        [System.Serializable]
        public class Slot
        {
            [Tooltip("칸 하나의 맨 위 오브젝트 (크기 변화용)")]
            public RectTransform root;
            [Tooltip("칸 바탕 (9-slice)")]
            public Image back;
            [Tooltip("무기 그림")]
            public Image icon;
        }

        [SerializeField] Slot[] slots;
        [SerializeField] Sprite normalBack;
        [SerializeField] Sprite selectedBack;

        [Tooltip("들고 있지 않은 칸의 크기·투명도")]
        [SerializeField] float idleScale = 0.92f;
        [SerializeField] float idleAlpha = 0.6f;

        [Tooltip("들고 있는 칸의 크기")]
        [SerializeField] float selectedScale = 1.06f;

        AutoGun gun;
        int selected = -1;
        float popStart = -10f;

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                gun = player.GetComponent<AutoGun>();
            Refresh(true);
        }

        void Update()
        {
            Refresh(false);
        }

        void Refresh(bool immediate)
        {
            if (gun == null || slots == null)
                return;
            int now = gun.IsBazooka ? 1 : 0;
            if (now != selected)
            {
                selected = now;
                if (!immediate)
                    popStart = Time.unscaledTime;
            }
            float k = Mathf.Clamp01((Time.unscaledTime - popStart) / 0.28f);
            float pop = k < 1f ? 1f + 0.22f * Mathf.Sin(k * Mathf.PI) : 1f; // 새로 고른 칸이 통통 튄다
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null)
                    continue;
                bool on = i == selected;
                slot.root.localScale = Vector3.one * ((on ? selectedScale * pop : idleScale));
                if (slot.back != null)
                    slot.back.sprite = on ? selectedBack : normalBack;
                if (slot.icon != null)
                    slot.icon.color = new Color(1f, 1f, 1f, on ? 1f : idleAlpha);
            }
        }
    }
}
