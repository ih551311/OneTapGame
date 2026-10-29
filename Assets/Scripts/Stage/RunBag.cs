using System;
using LastBullet.Inventory;
using UnityEngine;

namespace LastBullet.Stage
{
    // 전투 가방 (기획서 2.1, 2.6). 이번 판에서 얻은 재료를 임시로 담는다.
    // 클리어하면 창고로 정산하고, 사망하면 전부 사라진다 (정산·삭제는 Phase 5-2).
    // 스테이지 씬마다 하나. 적의 EnemyDrop이 RunBag.Current에 넣는다.
    // 붙일 곳: 스테이지 씬의 StageManager 오브젝트
    public class RunBag : MonoBehaviour
    {
        public static RunBag Current { get; private set; }

        public int PaperScraps { get; private set; }
        readonly int[] paints = new int[PaintColors.Count];

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
            paints[(int)color] += paintAmount;
            Debug.Log($"[RunBag] 종이조각 +{paperScraps} (합계 {PaperScraps}), {PaintColors.KoreanNames[(int)color]} 물감 +{paintAmount} (합계 {paints[(int)color]})");
            Changed?.Invoke();
        }
    }
}
