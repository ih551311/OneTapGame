using System;
using LastBullet.Inventory;
using UnityEngine;

namespace LastBullet.Stage
{
    // 전투 가방 (기획서 2.1, 2.6). 이번 판에서 얻은 재료를 임시로 담는다.
    // 미술관으로 귀환하면 창고로 정산(Settle)하고, 사망하면 전부 사라진다(Clear).
    // (2026-10-30: 스테이지 클리어 시스템을 없애고 자유 파밍으로 바꿨다. 귀환은 ESC 메뉴의 [미술관으로 귀환].)
    // 전투 씬마다 하나. 적의 EnemyDrop이 RunBag.Current에 넣는다.
    // 붙일 곳: 전투 씬의 RunBag 오브젝트
    public class RunBag : MonoBehaviour
    {
        public static RunBag Current { get; private set; }

        public int PaperScraps { get; private set; }
        public int Kills { get; private set; } // 이번 판에 처치한 적 수 (적을 잡을 때마다 Add가 불린다)
        readonly int[] paints = new int[PaintColors.Count];

        // 이번 판 요약 (결과 창이 보여 준다). 정산·삭제(Settle, Clear)로 가방이 비워지기 전에 받아 둔다.
        public RunSummary Summary()
        {
            return new RunSummary { kills = Kills, paperScraps = PaperScraps, paints = (int[])paints.Clone() };
        }

        public event Action Changed; // 화면 표시(RunBagUI)가 듣는다

        public int GetPaint(PaintColor color) => paints[(int)color];

        void Awake()
        {
            Current = this;
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        public void Add(int paperScraps, PaintColor color, int paintAmount)
        {
            PaperScraps += paperScraps;
            Kills++;
            paints[(int)color] += paintAmount;
            Debug.Log($"[RunBag] 종이조각 +{paperScraps} (합계 {PaperScraps}), {PaintColors.KoreanNames[(int)color]} 물감 +{paintAmount} (합계 {paints[(int)color]})");
            Changed?.Invoke();
        }

        // 클리어: 가방의 재료를 창고로 옮긴다. 물감이 500을 넘으면 창고가 넘친 양을 버린다.
        public void Settle()
        {
            var inventory = InventoryManager.Instance;
            inventory.AddPaperScraps(PaperScraps);
            for (int i = 0; i < paints.Length; i++)
            {
                if (paints[i] > 0)
                    inventory.AddPaint((PaintColor)i, paints[i]);
            }
            Debug.Log($"[RunBag] 정산: 종이조각 {PaperScraps}개와 물감을 창고로 옮김 → 창고 종이조각 {inventory.PaperScraps}");
            Empty();
        }

        // 사망: 이번 판 재료를 모두 잃는다. 창고는 그대로다.
        public void Clear()
        {
            Debug.Log($"[RunBag] 사망: 이번 판 재료를 잃음 (종이조각 {PaperScraps}개)");
            Empty();
        }

        void Empty()
        {
            PaperScraps = 0;
            Kills = 0;
            Array.Clear(paints, 0, paints.Length);
            Changed?.Invoke();
        }
    }

    // 한 판의 요약: 처치 수, 종이조각, 물감 9색 양
    public struct RunSummary
    {
        public int kills;
        public int paperScraps;
        public int[] paints; // PaintColor 순서
    }
}
