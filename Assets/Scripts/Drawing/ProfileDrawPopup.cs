using LastBullet.Inventory;
using LastBullet.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastBullet.Drawing
{
    // 프로필 그림 그리기 창 (타이틀, 2026-10-31 사용자 요청). 미술관 그림판(PaintBoardPopup)과 같은 모양이지만
    //  - 물감과 종이를 쓰지 않는다 (색 9개를 마음껏 쓴다)
    //  - 완성하면 작품 목록이 아니라 프로필 사진(ProfileStore)으로 저장된다 (256×256 정사각형)
    // 도구: 펜 굵기 4단계 + 지우개. 취소(닫기·ESC)는 그린 것을 바로 버린다.
    // 붙일 곳: 0_Title의 Canvas/ProfileDrawPopup (평소에는 꺼져 있다)
    public class ProfileDrawPopup : PopupBase
    {
        [Tooltip("4 pen size buttons (thinnest first), then the eraser button")]
        [SerializeField] Button[] toolButtons;
        [SerializeField] RectTransform toolSelected;

        [Tooltip("9 color buttons (order: Red, Orange, Yellow, Green, Blue, Purple, Pink, White, Black)")]
        [SerializeField] Button[] colorButtons;
        [SerializeField] RectTransform colorSelected;

        [Tooltip("Area that shows the canvas")]
        [SerializeField] RawImage canvasImage;

        [SerializeField] Button doneButton;

        int toolIndex;
        PaintColor color = PaintColor.Black;
        bool IsEraser => toolIndex >= PaintBoardPopup.PenSizes.Length;
        int PenSize => PaintBoardPopup.PenSizes[Mathf.Min(toolIndex, PaintBoardPopup.PenSizes.Length - 1)];

        Texture2D canvas;
        Color32[] pixels;
        bool drawing;
        bool canvasDirty;
        Vector2 lastPoint;

        protected override void OnFirstOpen()
        {
            for (int i = 0; i < toolButtons.Length; i++)
            {
                int index = i;
                toolButtons[i].onClick.AddListener(() => SelectTool(index));
            }
            for (int i = 0; i < colorButtons.Length; i++)
            {
                var c = (PaintColor)i;
                colorButtons[i].onClick.AddListener(() => SelectColor(c));
            }
            doneButton.onClick.AddListener(Complete);
        }

        protected override void OnOpened()
        {
            NewCanvas();
            SelectTool(1);
            SelectColor(PaintColor.Black);
        }

        void OnDisable()
        {
            drawing = false;
        }

        void OnDestroy()
        {
            if (canvas != null)
                Destroy(canvas);
        }

        // 완성: 그린 그림을 프로필 사진으로 저장하고 닫는다. 저장에 실패하면 그대로 둔다.
        void Complete()
        {
            canvas.SetPixels32(pixels); // 아직 화면에 반영되지 않은 마지막 획까지 담는다
            canvas.Apply();
            if (ProfileStore.SetDrawn(canvas))
                Close();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen)
                return;
            HandleMouse();
            if (canvasDirty)
            {
                canvas.SetPixels32(pixels);
                canvas.Apply(); // 프레임당 한 번
                canvasDirty = false;
            }
        }

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            bool hasPoint = TryGetCanvasPoint(mouse.position.ReadValue(), out Vector2 point, out bool inside);
            if (mouse.leftButton.wasPressedThisFrame && hasPoint && inside)
            {
                drawing = true;
                lastPoint = point;
                Stamp(point);
            }
            else if (drawing && mouse.leftButton.isPressed && hasPoint)
            {
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

        // from → to 선을 1px 간격으로 따라가며 찍는다
        void StampAlong(Vector2 from, Vector2 to)
        {
            float length = Vector2.Distance(from, to);
            if (length < 0.0001f)
            {
                Stamp(from);
                return;
            }
            float travelled = 0f;
            while (travelled < length)
            {
                travelled += Mathf.Min(1f, length - travelled);
                Stamp(Vector2.Lerp(from, to, travelled / length));
            }
        }

        // 펜 굵기만큼의 동그란 점을 찍는다 (지우개는 흰색)
        void Stamp(Vector2 center)
        {
            Color32 paint = IsEraser ? new Color32(255, 255, 255, 255) : PaintColors.Colors[(int)color];
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
                        pixels[y * size + x] = paint;
                }
            }
            canvasDirty = true;
        }

        // 열 때마다 새 흰 종이로 시작한다 (취소하면 그린 것은 버려진다)
        void NewCanvas()
        {
            int size = PaintingLibrary.CanvasSize;
            if (canvas == null)
            {
                canvas = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                canvasImage.texture = canvas;
            }
            pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            canvas.SetPixels32(pixels);
            canvas.Apply();
            drawing = false;
            canvasDirty = false;
        }

        void SelectTool(int index)
        {
            toolIndex = index;
            Mark(toolSelected, toolButtons[index].transform);
        }

        void SelectColor(PaintColor c)
        {
            color = c;
            Mark(colorSelected, colorButtons[(int)c].transform);
        }

        static void Mark(RectTransform marker, Transform target)
        {
            marker.SetParent(target, false);
            marker.anchorMin = Vector2.zero;
            marker.anchorMax = Vector2.one;
            marker.offsetMin = marker.offsetMax = Vector2.zero;
            marker.gameObject.SetActive(true);
        }
    }
}
