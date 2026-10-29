using System;
using UnityEngine;

namespace LastBullet.Inventory
{
    // 창고 (기획서 2.7). 씬을 옮겨도 유지된다 (DontDestroyOnLoad).
    // 종이조각, 그림용 종이, 물감 9색. 물감은 색마다 최대 500, 넘치는 양은 버리고 PaintOverflow를 알린다.
    // 처음 InventoryManager.Instance를 부를 때 자동으로 만들어진다 (씬에 놓을 필요 없음).
    // Phase 5-3: 종이 합성, Phase 5-4: 저장·불러오기.
    public class InventoryManager : MonoBehaviour
    {
        public const int PaintMax = 500;

        static InventoryManager instance;
        static bool quitting;

        public static InventoryManager Instance
        {
            get
            {
                if (instance == null && !quitting)
                {
                    var go = new GameObject("InventoryManager");
                    instance = go.AddComponent<InventoryManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // 이미 만들어져 있는지 (UI가 사라질 때 괜히 새로 만들지 않도록)
        public static bool Exists => instance != null;

        public int PaperScraps { get; private set; }
        public int DrawingPaper { get; private set; }
        readonly int[] paints = new int[PaintColors.Count];

        public event Action Changed;                       // 화면 표시가 듣는다
        public event Action<PaintColor, int> PaintOverflow; // (색, 버린 양) "가득 참" 알림이 듣는다

        public int GetPaint(PaintColor color) => paints[(int)color];

        public void AddPaperScraps(int amount)
        {
            PaperScraps += amount;
            Changed?.Invoke();
        }

        // 넣고 남아서 버린 양을 돌려준다
        public int AddPaint(PaintColor color, int amount)
        {
            int index = (int)color;
            int added = Mathf.Min(amount, PaintMax - paints[index]);
            paints[index] += added;
            int discarded = amount - added;
            if (discarded > 0)
            {
                Debug.Log($"[Inventory] {PaintColors.KoreanNames[index]} 물감 가득 참: {discarded} 버림");
                PaintOverflow?.Invoke(color, discarded);
            }
            Changed?.Invoke();
            return discarded;
        }

        void OnApplicationQuit()
        {
            quitting = true;
        }

#if UNITY_EDITOR
        // 테스트용: 모든 물감 양을 정한다 (메뉴 LastBullet > 테스트)
        public void DebugSetAllPaints(int amount)
        {
            for (int i = 0; i < paints.Length; i++)
                paints[i] = Mathf.Clamp(amount, 0, PaintMax);
            Changed?.Invoke();
        }
#endif
    }
}
