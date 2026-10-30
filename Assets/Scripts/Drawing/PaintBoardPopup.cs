using LastBullet.Drawing;
using LastBullet.Inventory;
using LastBullet.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.Drawing
{
    // 그림판 팝업 (미술관의 그림 작업대에서 E로 연다, 기획서 2.8).
    // 6-1: 도구(펜 굵기 4단계, 지우개)와 색(9색)을 고르고, 종이 수와 물감 잔량을 보여 준다.
    //      캔버스는 흰 종이(256×256)만 보인다. 그리기는 6-2, 완성·저장은 Phase 7.
    // 닫기 버튼 칸(PopupBase.closeButton)에는 "취소" 버튼을 연결한다.
    // 붙일 곳: Canvas/PaintBoardPopup (평소에는 꺼져 있다)
    public class PaintBoardPopup : PopupBase
    {
        [Tooltip("펜 굵기 4단계 버튼(가는 것부터) 다음에 지우개 버튼")]
        [SerializeField] Button[] toolButtons;

        [Tooltip("선택된 도구에 겹쳐 보이는 테두리")]
        [SerializeField] RectTransform toolSelected;

        [Tooltip("9색 버튼 (빨강, 주황, 노랑, 초록, 파랑, 남색, 보라, 하양, 검정 순서)")]
        [SerializeField] Button[] colorButtons;

        [Tooltip("선택된 색에 겹쳐 보이는 테두리")]
        [SerializeField] RectTransform colorSelected;

        [Tooltip("색 버튼 아래 물감 잔량 글자 (9개)")]
        [SerializeField] TMP_Text[] paintAmounts;

        [Tooltip("캔버스를 보여 주는 칸")]
        [SerializeField] RawImage canvasImage;

        [SerializeField] TMP_Text paperCount;

        [Tooltip("완성 버튼. 저장은 Phase 7에서 연결한다")]
        [SerializeField] Button doneButton;

        // 펜 굵기 (px, 기획서 2.8)
        public static readonly int[] PenSizes = { 2, 5, 10, 20 };

        public int ToolIndex { get; private set; }
        public bool IsEraser => ToolIndex >= PenSizes.Length;
        public int PenSize => PenSizes[Mathf.Min(ToolIndex, PenSizes.Length - 1)];
        public PaintColor CurrentColor { get; private set; } = PaintColor.Black;

        public Texture2D Canvas { get; private set; }

        protected override void OnFirstOpen()
        {
            for (int i = 0; i < toolButtons.Length; i++)
            {
                int index = i;
                toolButtons[i].onClick.AddListener(() => SelectTool(index));
            }
            for (int i = 0; i < colorButtons.Length; i++)
            {
                var color = (PaintColor)i;
                colorButtons[i].onClick.AddListener(() => SelectColor(color));
            }
            InventoryManager.Instance.Changed += RefreshInventory;
        }

        protected override void OnOpened()
        {
            NewCanvas();
            SelectTool(1);
            SelectColor(PaintColor.Black);
            RefreshInventory();
        }

        void OnDestroy()
        {
            if (InventoryManager.Exists)
                InventoryManager.Instance.Changed -= RefreshInventory;
            if (Canvas != null)
                Destroy(Canvas);
        }

        // 열 때마다 새 흰 종이로 시작한다. 이어 그리기는 없다 (기획서 2.8: 취소하면 미완성 그림은 삭제).
        void NewCanvas()
        {
            int size = PaintingLibrary.CanvasSize;
            if (Canvas == null)
            {
                Canvas = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                canvasImage.texture = Canvas;
            }
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            Canvas.SetPixels32(pixels);
            Canvas.Apply();
        }

        void SelectTool(int index)
        {
            ToolIndex = index;
            Mark(toolSelected, toolButtons[index].transform);
        }

        void SelectColor(PaintColor color)
        {
            CurrentColor = color;
            Mark(colorSelected, colorButtons[(int)color].transform);
        }

        static void Mark(RectTransform marker, Transform target)
        {
            marker.SetParent(target, false);
            marker.anchorMin = Vector2.zero;
            marker.anchorMax = Vector2.one;
            marker.offsetMin = marker.offsetMax = Vector2.zero;
            marker.gameObject.SetActive(true);
        }

        void RefreshInventory()
        {
            var inventory = InventoryManager.Instance;
            paperCount.text = $"{inventory.DrawingPaper}";
            for (int i = 0; i < paintAmounts.Length; i++)
                paintAmounts[i].text = $"{inventory.GetPaint((PaintColor)i)}";
            // 완성(저장)은 Phase 7에서 연결한다. 그때까지는 항상 잠가 둔다.
            doneButton.interactable = false;
        }
    }
}
