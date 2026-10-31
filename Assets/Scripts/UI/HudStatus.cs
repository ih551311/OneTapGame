using LastBullet.Drawing;
using LastBullet.Inventory;
using LastBullet.Stage;
using TMPro;
using UnityEngine;

namespace LastBullet.UI
{
    // 화면 왼쪽 위 상태창(HudProfile)의 숫자들 (2026-10-31 사용자 요청).
    //  - 미술관: "그림 N장"(내가 그린 작품 수), "종이 N장"(남은 그림용 종이)
    //  - 전투 맵: "잡은 적 N"(이번 판), 이번 판에 얻은 물감(그 맵에서 나오는 3색의 양)
    // 이번 판 값은 전투 가방(RunBag)에서 읽는다. 귀환·사망으로 가방이 비워지면 0으로 돌아간다.
    // 붙일 곳: 상태창 HudProfile (메뉴 LastBullet > Phase 34 상태창에 숫자 넣기)
    public class HudStatus : MonoBehaviour
    {
        public enum Mode { Museum, Battle }

        [SerializeField] Mode mode;

        [Tooltip("First line text: museum \"pictures N\" / battle \"enemies killed N\"")]
        [SerializeField] TMP_Text line1;

        [Tooltip("Second line text (museum only): \"paper N\"")]
        [SerializeField] TMP_Text line2;

        [Header("Battle: paint gained this run (the 3 colors of that map)")]
        [SerializeField] PaintColor[] paintColors = new PaintColor[0];
        [SerializeField] TMP_Text[] paintCounts = new TMP_Text[0];

        RunBag bag;

        void Start()
        {
            if (mode == Mode.Museum)
            {
                PaintingLibrary.Changed += Refresh;
                if (InventoryManager.Exists)
                    InventoryManager.Instance.Changed += Refresh;
            }
            else
            {
                // RunBag.Awake가 Current를 정한 뒤라 Start에서 찾는다 (RunBagUI와 같다)
                bag = RunBag.Current;
                if (bag != null)
                    bag.Changed += Refresh;
            }
            Refresh();
        }

        void OnDestroy()
        {
            PaintingLibrary.Changed -= Refresh;
            if (InventoryManager.Exists)
                InventoryManager.Instance.Changed -= Refresh;
            if (bag != null)
                bag.Changed -= Refresh;
        }

        void Refresh()
        {
            if (mode == Mode.Museum)
            {
                line1.text = $"그림 {PaintingLibrary.LoadList().Count}장";
                line2.text = $"종이 {(InventoryManager.Exists ? InventoryManager.Instance.DrawingPaper : 0)}장";
                return;
            }

            line1.text = $"잡은 적 {(bag != null ? bag.Kills : 0)}";
            for (int i = 0; i < paintCounts.Length && i < paintColors.Length; i++)
                paintCounts[i].text = bag != null ? bag.GetPaint(paintColors[i]).ToString() : "0";
        }
    }
}
