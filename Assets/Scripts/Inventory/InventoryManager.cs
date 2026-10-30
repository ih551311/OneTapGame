using System;
using LastBullet.Core;
using UnityEngine;

namespace LastBullet.Inventory
{
    // 창고 (기획서 2.7). 씬을 옮겨도 유지된다 (DontDestroyOnLoad).
    // 종이조각, 그림용 종이, 물감 9색. 물감은 색마다 최대 500, 넘치는 양은 버리고 PaintOverflow를 알린다.
    // 처음 InventoryManager.Instance를 부를 때 자동으로 만들어지고, save.json에서 불러온다.
    // 수량이 바뀔 때마다 save.json에 저장한다 (SaveManager).
    public class InventoryManager : MonoBehaviour
    {
        public const int PaintMax = 500;
        public const int ScrapsPerPaper = 5; // 종이조각 5개 → 그림용 종이 1장 (기획서 2.7)

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

        void Awake()
        {
            // 저장된 수량 불러오기
            var save = SaveManager.Data;
            PaperScraps = save.paperScraps;
            DrawingPaper = save.drawingPaper;
            for (int i = 0; i < paints.Length; i++)
                paints[i] = Mathf.Clamp(save.paints[i], 0, PaintMax);
        }

        public void AddPaperScraps(int amount)
        {
            PaperScraps += amount;
            SaveAndNotify();
        }

        public bool CanCraftPaper => PaperScraps >= ScrapsPerPaper;

        // 종이 합성대: 종이조각 5개로 그림용 종이 1장을 만든다. 조각이 모자라면 false.
        public bool CraftPaper()
        {
            if (!CanCraftPaper)
                return false;
            PaperScraps -= ScrapsPerPaper;
            DrawingPaper++;
            Debug.Log($"[Inventory] 종이 합성: 종이조각 {PaperScraps}개 남음, 그림용 종이 {DrawingPaper}장");
            SaveAndNotify();
            return true;
        }

        // 그림판 "완성": 그림용 종이 1장을 쓴다. 종이가 없으면 false.
        // 호출하는 쪽이 PNG 저장에 성공한 뒤에만 불러야 한다 (기획서 2.8).
        public bool UseDrawingPaper()
        {
            if (DrawingPaper <= 0)
                return false;
            DrawingPaper--;
            Debug.Log($"[Inventory] 그림용 종이 사용: {DrawingPaper}장 남음");
            SaveAndNotify();
            return true;
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
            SaveAndNotify();
            return discarded;
        }

        // 그림판: 물감을 정수 단위로 쓴다. 모자라면 false.
        // 마우스를 움직이는 동안 자주 불리므로 파일에는 쓰지 않고 화면에만 알린다. 저장은 CommitToSave()가 한다.
        public bool SpendPaint(PaintColor color, int amount)
        {
            int index = (int)color;
            if (amount > paints[index])
                return false;
            paints[index] -= amount;
            Changed?.Invoke();
            return true;
        }

        // 지금 수량을 save.json에 쓴다 (그림판이 5초마다, 닫을 때 부른다)
        public void CommitToSave()
        {
            CopyToSaveData();
            SaveManager.Save();
        }

        // 바뀐 수량을 save.json에 쓰고 화면에 알린다
        void SaveAndNotify()
        {
            CommitToSave();
            Changed?.Invoke();
        }

        void CopyToSaveData()
        {
            var save = SaveManager.Data;
            save.paperScraps = PaperScraps;
            save.drawingPaper = DrawingPaper;
            for (int i = 0; i < paints.Length; i++)
                save.paints[i] = paints[i];
        }

        void OnApplicationQuit()
        {
            quitting = true;
        }

        // 재생을 시작할 때 이전 재생의 값이 남지 않게 한다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            instance = null;
            quitting = false;
        }

#if UNITY_EDITOR
        // 테스트용: 모든 물감 양을 정한다 (메뉴 LastBullet > 테스트)
        public void DebugSetAllPaints(int amount)
        {
            for (int i = 0; i < paints.Length; i++)
                paints[i] = Mathf.Clamp(amount, 0, PaintMax);
            SaveAndNotify();
        }

        // 테스트용: 종이조각을 더한다
        public void DebugAddPaperScraps(int amount)
        {
            AddPaperScraps(amount);
        }
#endif
    }
}
