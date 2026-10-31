using LastBullet.Drawing;
using LastBullet.Inventory;
using LastBullet.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastBullet.Drawing
{
    // 그림판 팝업 (미술관의 그림 작업대에서 E로 연다, 기획서 2.8).
    // 6-1: 도구(펜 굵기 4단계, 지우개)와 색(9색)을 고르고, 종이 수와 물감 잔량을 보여 준다.
    // 6-2: 마우스로 그린다. 물감은 선을 그은 길이만큼 쓰고, 0이면 그리기가 막힌다. 지우개는 물감을 쓰지 않는다.
    // 7-1: 완성 버튼 = PNG 저장 → 작품 등록 → 종이 1장 사용. 취소 확인창은 7-2.
    // 닫기 버튼 칸(PopupBase.closeButton)에는 "취소" 버튼을 연결한다.
    // 붙일 곳: Canvas/PaintBoardPopup (평소에는 꺼져 있다)
    public class PaintBoardPopup : PopupBase
    {
        [Tooltip("펜 굵기 4단계 버튼(가는 것부터) 다음에 지우개 버튼")]
        [SerializeField] Button[] toolButtons;

        [Tooltip("선택된 도구에 겹쳐 보이는 테두리")]
        [SerializeField] RectTransform toolSelected;

        [Tooltip("9색 버튼 (빨강, 주황, 노랑, 초록, 파랑, 보라, 핑크, 하양, 검정 순서)")]
        [SerializeField] Button[] colorButtons;

        [Tooltip("선택된 색에 겹쳐 보이는 테두리")]
        [SerializeField] RectTransform colorSelected;

        [Tooltip("색 버튼 아래 물감 잔량 글자 (9개)")]
        [SerializeField] TMP_Text[] paintAmounts;

        [Tooltip("캔버스를 보여 주는 칸")]
        [SerializeField] RawImage canvasImage;

        [SerializeField] TMP_Text paperCount;

        [Tooltip("완성 버튼. 종이가 있을 때만 눌린다")]
        [SerializeField] Button doneButton;

        [Tooltip("\"내 그림\" 버튼. 완성한 그림들을 보는 팝업을 연다")]
        [SerializeField] Button galleryButton;

        [Tooltip("완성한 그림을 보는 팝업 (보기 전용 GalleryPopup)")]
        [SerializeField] PopupBase galleryPopup;

        [Tooltip("취소할 때 뜨는 확인창 (평소에는 꺼져 있다)")]
        [SerializeField] GameObject confirmDialog;

        [Tooltip("확인창의 '그만두기' 버튼. 그림을 버리고 닫는다")]
        [SerializeField] Button confirmDiscardButton;

        [Tooltip("확인창의 '계속 그리기' 버튼")]
        [SerializeField] Button confirmContinueButton;

        // 펜 굵기 (px, 기획서 2.8)
        public static readonly int[] PenSizes = { 2, 5, 10, 20 };

        public int ToolIndex { get; private set; }
        public bool IsEraser => ToolIndex >= PenSizes.Length;
        public int PenSize => PenSizes[Mathf.Min(ToolIndex, PenSizes.Length - 1)];
        public PaintColor CurrentColor { get; private set; } = PaintColor.Black;

        public Texture2D Canvas { get; private set; }

        // 물감 소비: 선 길이(px) × 펜 굵기(px) × 0.01 (기획서 2.7). 소수 누적값은 여기 보관하고 정수 단위로 차감한다.
        const float PaintPerPixelArea = 0.01f;
        const float AutoSaveSeconds = 5f; // 그리는 동안 물감 잔량을 저장하는 간격 (기획서 3.7)

        Color32[] pixels;
        float paintDebt;
        bool drawing;
        bool blocked;     // 물감이 떨어져 이번 획은 더 못 그린다
        bool canvasDirty;
        Vector2 lastPoint;
        float nextSaveTime;

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
            doneButton.onClick.AddListener(Complete);
            galleryButton.onClick.AddListener(() =>
            {
                drawing = false;
                galleryPopup.Open();
            });
            confirmDiscardButton.onClick.AddListener(Close); // 미완성 그림은 저장된 적이 없으니 닫기만 하면 버려진다
            confirmContinueButton.onClick.AddListener(() => confirmDialog.SetActive(false));
            InventoryManager.Instance.Changed += RefreshInventory;
        }

        // 취소 버튼·ESC: 바로 닫지 않고 확인창을 띄운다 (기획서 2.8). 확인창이 떠 있을 때 ESC는 "계속 그리기".
        // 쓴 물감은 돌려주지 않고, 종이는 쓰지 않는다.
        protected override void RequestClose()
        {
            drawing = false;
            confirmDialog.SetActive(!confirmDialog.activeSelf);
        }

        // 완성: PNG로 저장하고 작품 목록에 등록한 뒤에만 종이 1장을 쓴다. 저장에 실패하면 종이는 그대로다.
        // 내용은 상관없다 (빈 종이도 된다, 기획서 2.8).
        void Complete()
        {
            var inventory = InventoryManager.Instance;
            if (inventory.DrawingPaper <= 0)
                return;

            Canvas.SetPixels32(pixels); // 아직 화면에 반영되지 않은 마지막 획까지 담는다
            Canvas.Apply();
            var info = PaintingLibrary.Add(Canvas);
            if (info == null)
                return; // 저장 실패: 종이를 쓰지 않고 그림판을 그대로 둔다

            inventory.UseDrawingPaper();
            Debug.Log($"[PaintBoard] 그림 완성: {info.id}");
            Close();
        }

        protected override void OnOpened()
        {
            confirmDialog.SetActive(false);
            NewCanvas();
            SelectTool(1);
            SelectColor(PaintColor.Black);
            RefreshInventory();
        }

        void OnDisable()
        {
            drawing = false;
            if (InventoryManager.Exists)
                InventoryManager.Instance.CommitToSave(); // 닫을 때 물감 잔량 저장
        }

        void OnDestroy()
        {
            if (InventoryManager.Exists)
                InventoryManager.Instance.Changed -= RefreshInventory;
            if (Canvas != null)
                Destroy(Canvas);
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen)
                return;

            HandleMouse();

            if (canvasDirty)
            {
                Canvas.SetPixels32(pixels);
                Canvas.Apply(); // 프레임당 한 번
                canvasDirty = false;
            }
            if (Time.unscaledTime >= nextSaveTime)
            {
                nextSaveTime = Time.unscaledTime + AutoSaveSeconds;
                InventoryManager.Instance.CommitToSave();
            }
        }

        // 고른 색의 물감이 0이면 그릴 수 없다. 지우개는 물감을 쓰지 않으므로 항상 쓸 수 있다.
        bool OutOfPaint => !IsEraser && InventoryManager.Instance.GetPaint(CurrentColor) <= 0;

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null || confirmDialog.activeSelf || galleryPopup.IsOpen)
                return; // 확인창이나 "내 그림"이 떠 있는 동안은 그릴 수 없다

            bool hasPoint = TryGetCanvasPoint(mouse.position.ReadValue(), out Vector2 point, out bool inside);
            if (mouse.leftButton.wasPressedThisFrame && hasPoint && inside)
            {
                drawing = true;
                blocked = false;
                lastPoint = point;
                if (OutOfPaint)
                    blocked = true; // 물감이 0이면 점도 찍을 수 없다
                else
                    StampAlong(point, point); // 점 하나 (선 길이 0이라 물감은 쓰지 않는다)
            }
            else if (drawing && mouse.leftButton.isPressed && hasPoint)
            {
                if (!blocked)
                    StampAlong(lastPoint, point);
                lastPoint = point;
            }

            if (!mouse.leftButton.isPressed)
                drawing = false;
        }

        // 화면 좌표 → 캔버스 픽셀 좌표 (0 ~ 256). 캔버스 밖이어도 좌표는 돌려준다.
        bool TryGetCanvasPoint(Vector2 screenPosition, out Vector2 point, out bool inside)
        {
            var rect = canvasImage.rectTransform;
            point = default;
            inside = false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, null, out Vector2 local))
                return false;
            Vector2 normalized = (local - rect.rect.min) / rect.rect.size;
            point = normalized * PaintingLibrary.CanvasSize;
            inside = normalized.x >= 0f && normalized.x <= 1f && normalized.y >= 0f && normalized.y <= 1f;
            return true;
        }

        // from → to 선을 1px 간격으로 따라가며 찍는다. 같은 선이면 마우스가 빠르든 느리든 소비량이 같다.
        void StampAlong(Vector2 from, Vector2 to)
        {
            var inventory = InventoryManager.Instance;
            float length = Vector2.Distance(from, to);
            Color32 color = IsEraser ? new Color32(255, 255, 255, 255) : PaintColors.Colors[(int)CurrentColor];

            if (length < 0.0001f)
            {
                Stamp(from, color);
                return;
            }

            float travelled = 0f;
            while (travelled < length)
            {
                float step = Mathf.Min(1f, length - travelled);
                travelled += step;
                if (OutOfPaint)
                {
                    blocked = true;
                    return;
                }
                if (!IsEraser)
                {
                    float debt = paintDebt + step * PenSize * PaintPerPixelArea;
                    if (debt >= 1f)
                    {
                        if (!inventory.SpendPaint(CurrentColor, 1))
                        {
                            blocked = true; // 물감이 없으면 그리기 차단
                            return;
                        }
                        debt -= 1f;
                    }
                    paintDebt = debt;
                }
                Stamp(Vector2.Lerp(from, to, travelled / length), color);
            }
        }

        // 펜 굵기만큼의 동그란 점을 찍는다
        void Stamp(Vector2 center, Color32 color)
        {
            int size = PaintingLibrary.CanvasSize;
            float radius = PenSize * 0.5f;
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius));
            float radiusSquared = radius * radius;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x + 0.5f - center.x;
                    float dy = y + 0.5f - center.y;
                    if (dx * dx + dy * dy <= radiusSquared)
                        pixels[y * size + x] = color;
                }
            }
            canvasDirty = true;
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
            pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            Canvas.SetPixels32(pixels);
            Canvas.Apply();
            paintDebt = 0f;
            drawing = false;
            canvasDirty = false;
            nextSaveTime = Time.unscaledTime + AutoSaveSeconds;
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
            doneButton.interactable = inventory.DrawingPaper > 0; // 종이가 없으면 완성할 수 없다
        }
    }
}
