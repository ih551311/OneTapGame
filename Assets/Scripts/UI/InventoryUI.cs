using LastBullet.Inventory;
using TMPro;
using UnityEngine;

namespace LastBullet.UI
{
    // 미술관 화면 오른쪽 위에 창고 재료(종이조각, 그림용 종이, 물감 9색)를 보여 준다.
    // 그림: Assets/UI/BagPanel.png, Icon_PaperScrap.png, Icon_Paper.png, Icon_Paint_*.png (docs/ART_SLOTS.md)
    // 붙일 곳: 1_Museum의 Canvas/InventoryPanel
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] TMP_Text paperScrapCount;
        [SerializeField] TMP_Text drawingPaperCount;

        [Tooltip("Paint color order (9): Red, Orange, Yellow, Green, Blue, Purple, Pink, White, Black")]
        [SerializeField] TMP_Text[] paintCounts = new TMP_Text[PaintColors.Count];

        void Start()
        {
            InventoryManager.Instance.Changed += Refresh;
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
            paperScrapCount.text = inventory.PaperScraps.ToString();
            drawingPaperCount.text = inventory.DrawingPaper.ToString();
            for (int i = 0; i < PaintColors.Count; i++)
                paintCounts[i].text = $"{inventory.GetPaint((PaintColor)i)}";
        }
    }
}
