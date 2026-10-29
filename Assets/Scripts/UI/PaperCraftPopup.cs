using LastBullet.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 종이 합성 팝업 (미술관의 종이 합성대에서 E로 연다, 기획서 2.9).
    // "종이조각 5개 → 그림용 종이 1장". 합성 버튼을 누를 때마다 한 장씩 만든다.
    // 종이조각이 5개보다 적으면 합성 버튼이 잠긴다.
    // 붙일 곳: Canvas/PaperCraftPopup (평소에는 꺼져 있다)
    public class PaperCraftPopup : PopupBase
    {
        [SerializeField] Button craftButton;
        [SerializeField] TMP_Text paperScrapCount; // 가진 종이조각
        [SerializeField] TMP_Text drawingPaperCount; // 가진 그림용 종이

        protected override void OnFirstOpen()
        {
            craftButton.onClick.AddListener(() => InventoryManager.Instance.CraftPaper());
            InventoryManager.Instance.Changed += Refresh;
        }

        protected override void OnOpened()
        {
            Refresh();
        }

        void OnDestroy()
        {
            if (InventoryManager.Exists)
                InventoryManager.Instance.Changed -= Refresh;
        }

        void Refresh()
        {
            var inventory = InventoryManager.Instance;
            paperScrapCount.text = $"{inventory.PaperScraps}";
            drawingPaperCount.text = $"{inventory.DrawingPaper}";
            craftButton.interactable = inventory.CanCraftPaper;
        }
    }
}
