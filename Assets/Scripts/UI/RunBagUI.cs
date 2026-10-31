using LastBullet.Inventory;
using LastBullet.Stage;
using TMPro;
using UnityEngine;

namespace LastBullet.UI
{
    // 전투 중 화면 오른쪽 위에 이번 판에서 얻은 재료(전투 가방)를 작게 보여 준다 (기획서 2.6).
    // 그림: Assets/UI/BagPanel.png, Icon_PaperScrap.png, Icon_Paint_*.png (docs/ART_SLOTS.md)
    // 붙일 곳: Canvas/RunBagPanel
    public class RunBagUI : MonoBehaviour
    {
        [SerializeField] TMP_Text paperScrapCount;

        [Tooltip("Paint color order (9): Red, Orange, Yellow, Green, Blue, Purple, Pink, White, Black")]
        [SerializeField] TMP_Text[] paintCounts = new TMP_Text[PaintColors.Count];

        RunBag bag;

        // Start에서 찾는다: RunBag.Awake가 Current를 정한 뒤라야 한다.
        void Start()
        {
            bag = RunBag.Current;
            if (bag != null)
                bag.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (bag != null)
                bag.Changed -= Refresh;
        }

        void Refresh()
        {
            paperScrapCount.text = bag != null ? bag.PaperScraps.ToString() : "0";
            for (int i = 0; i < PaintColors.Count; i++)
                paintCounts[i].text = bag != null ? bag.GetPaint((PaintColor)i).ToString() : "0";
        }
    }
}
