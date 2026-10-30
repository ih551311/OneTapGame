using LastBullet.Drawing;
using LastBullet.Inventory;
using LastBullet.Stage;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 6-1: 미술관에 그림 작업대를 놓고, E로 여는 그림판 팝업(껍데기)을 만든다.
    // 팝업 모양은 사용자가 준 참고 그림을 따른다: 크림색 창 / 제목·종이 수 / 왼쪽 도구 막대 / 정사각형 종이 / 색 9개 / 취소·완성 버튼
    // 위치는 패널의 왼쪽 위를 (0,0)으로 한 값이다 (X는 오른쪽, Y는 아래쪽으로 늘어난다).
    // 메뉴: LastBullet > Phase 6-1 그림 작업대와 그림판 만들기
    public static class Phase6PaintBoardSetup
    {
        const float PanelWidth = 760f, PanelHeight = 900f;
        const float FrameSize = 532f;   // 종이 테두리 (정사각형)
        const float CanvasShown = 512f; // 화면에 보이는 종이 크기 (256 × 2배)

        [MenuItem("LastBullet/Phase 6-1 그림 작업대와 그림판 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            if (GameObject.Find("PaintWorkbench") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 PaintWorkbench가 있어요.", "확인");
                return;
            }

            // 1. 그림 작업대 (임시 상자, 전용 머티리얼) - 지도 탁자 오른쪽
            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "PaintWorkbench";
            bench.transform.position = new Vector3(4f, 0.5f, 3f);
            bench.transform.localScale = new Vector3(1.4f, 1f, 0.9f);
            bench.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("PaintWorkbench", new Color32(0xEB, 0xB8, 0xC2, 0xFF));
            var device = bench.AddComponent<InteractDevice>();

            // 2. 가까이 가면 뜨는 안내
            var prompt = Phase4StageSelectSetup.CreateImage("Prompt_Paint", canvas.transform, "Assets/UI/Prompt_Paint.png");
            Phase4StageSelectSetup.SetRect(prompt, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(480f, 60f));
            prompt.SetActive(false);

            // 3. 팝업
            var popup = new GameObject("PaintBoardPopup", typeof(RectTransform));
            popup.transform.SetParent(canvas.transform, false);
            Phase4StageSelectSetup.Stretch(popup.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(popup.transform, false);
            Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var panel = Sliced("Panel", popup.transform, "Assets/UI/Paint_Panel.png", 40f);
            Phase4StageSelectSetup.SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            var p = panel.transform;

            // 제목, 종이 수
            Place(Phase4StageSelectSetup.CreateImage("Title", p, "Assets/UI/Paint_Title.png"), 296f, 60f, 512f, 96f);
            Place(Phase4StageSelectSetup.CreateImage("PaperLabel", p, "Assets/UI/Paint_PaperLabel.png"), 460f, 60f, 128f, 64f);
            var paperCount = Text(p, "PaperCount", "0", 560f, 60f, 80f, 56f, 36f);
            Place(Phase4StageSelectSetup.CreateImage("PaperUnit", p, "Assets/UI/Paint_PaperUnit.png"), 636f, 60f, 64f, 64f);

            // 도구 막대: 펜 굵기 4단계 + 지우개
            Place(Sliced("Toolbar", p, "Assets/UI/Paint_Toolbar.png", 28f), 104f, 386f, 100f, FrameSize);
            var toolPaths = new[] { "Paint_Tool_Pen1", "Paint_Tool_Pen2", "Paint_Tool_Pen3", "Paint_Tool_Pen4", "Paint_Tool_Eraser" };
            var toolButtons = new Button[toolPaths.Length];
            for (int i = 0; i < toolPaths.Length; i++)
                toolButtons[i] = IconButton(toolPaths[i], p, $"Assets/UI/{toolPaths[i]}.png", 104f, 176f + i * 94f, 80f);
            var toolSelected = Marker("ToolSelected", p, "Assets/UI/Paint_Tool_Selected.png");

            // 종이: 테두리 + 캔버스 (흰 종이는 코드가 만든 Texture2D가 채운다)
            Place(Sliced("CanvasFrame", p, "Assets/UI/Paint_CanvasFrame.png", 24f), 440f, 386f, FrameSize, FrameSize);
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(RawImage));
            canvasObject.transform.SetParent(p, false);
            Place(canvasObject, 440f, 386f, CanvasShown, CanvasShown);
            var canvasImage = canvasObject.GetComponent<RawImage>();
            canvasImage.color = Color.white;
            canvasImage.raycastTarget = true; // 6-2에서 마우스로 그린다

            // 색 9개 + 물감 잔량
            var colorButtons = new Button[PaintColors.Count];
            var amounts = new TMP_Text[PaintColors.Count];
            for (int i = 0; i < PaintColors.Count; i++)
            {
                string colorName = ((PaintColor)i).ToString();
                float x = 54f + 36.2f + i * 72.4f;
                colorButtons[i] = IconButton($"Color_{colorName}", p, $"Assets/UI/Paint_Color_{colorName}.png", x, 700f, 56f);
                amounts[i] = Text(p, $"Amount_{colorName}", "0", x, 748f, 70f, 32f, 22f);
            }
            var colorSelected = Marker("ColorSelected", p, "Assets/UI/Paint_Color_Selected.png");

            // 취소 / 완성 버튼
            var cancel = PaintButton("CancelButton", p, "Cancel", false, 210f, 820f);
            var done = PaintButton("DoneButton", p, "Done", true, 550f, 820f);

            var popupScript = popup.AddComponent<PaintBoardPopup>();
            var so = new SerializedObject(popupScript);
            SetArray(so.FindProperty("toolButtons"), toolButtons);
            so.FindProperty("toolSelected").objectReferenceValue = toolSelected;
            SetArray(so.FindProperty("colorButtons"), colorButtons);
            so.FindProperty("colorSelected").objectReferenceValue = colorSelected;
            SetArray(so.FindProperty("paintAmounts"), amounts);
            so.FindProperty("canvasImage").objectReferenceValue = canvasImage;
            so.FindProperty("paperCount").objectReferenceValue = paperCount;
            so.FindProperty("doneButton").objectReferenceValue = done;
            so.FindProperty("closeButton").objectReferenceValue = cancel; // 취소 = 닫기 (확인창은 Phase 7)
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.SetActive(false);

            var deviceSo = new SerializedObject(device);
            deviceSo.FindProperty("popup").objectReferenceValue = popupScript;
            deviceSo.FindProperty("prompt").objectReferenceValue = prompt;
            deviceSo.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = bench;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 6-1: 미술관에 그림 작업대(PaintWorkbench)와 그림판 팝업을 만들고 1_Museum 씬을 저장했어요.");
        }

        // 위쪽 왼쪽 모서리 기준 위치(x, y)로 놓는다 (패널 안)
        internal static void Place(GameObject go, float x, float y, float width, float height)
        {
            Phase4StageSelectSetup.SetRect(go, new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height));
        }

        internal static GameObject Sliced(string name, Transform parent, string path, float border)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, parent, path, border);
            var image = go.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            return go;
        }

        static Button IconButton(string name, Transform parent, string path, float x, float y, float size)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, parent, path);
            go.GetComponent<Image>().raycastTarget = true;
            Place(go, x, y, size, size);
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        static RectTransform Marker(string name, Transform parent, string path)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, parent, path);
            go.SetActive(false); // 처음 열릴 때 선택한 버튼 위로 옮겨진다
            return go.GetComponent<RectTransform>();
        }

        // 상태별 그림(보통/마우스 올림/누름/잠김)과 글자 그림이 있는 버튼
        static Button PaintButton(string name, Transform parent, string kind, bool hasDisabled, float x, float y)
        {
            return PaintButton(name, parent, kind, kind, hasDisabled, x, y);
        }

        // 버튼 배경은 kind(Cancel/Done)의 그림, 글자는 labelKind의 그림
        internal static Button PaintButton(string name, Transform parent, string kind, string labelKind, bool hasDisabled, float x, float y)
        {
            const float border = 32f;
            var go = Sliced(name, parent, $"Assets/UI/Paint_Button_{kind}_Normal.png", border);
            go.GetComponent<Image>().raycastTarget = true;
            Place(go, x, y, 312f, 90f);
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = ArtSlots.LoadSprite($"Assets/UI/Paint_Button_{kind}_Highlighted.png", border),
                pressedSprite = ArtSlots.LoadSprite($"Assets/UI/Paint_Button_{kind}_Pressed.png", border),
                selectedSprite = ArtSlots.LoadSprite($"Assets/UI/Paint_Button_{kind}_Normal.png", border),
                disabledSprite = hasDisabled ? ArtSlots.LoadSprite($"Assets/UI/Paint_Button_{kind}_Disabled.png", border) : null,
            };
            var label = Phase4StageSelectSetup.CreateImage("Label", go.transform, $"Assets/UI/Paint_Label_{labelKind}.png");
            Phase4StageSelectSetup.SetRect(label, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 90f));
            return button;
        }

        static TMP_Text Text(Transform parent, string name, string value, float x, float y, float width, float height, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Place(go, x, y, width, height);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color32(0x4F, 0x4A, 0x55, 0xFF);
            text.raycastTarget = false;
            return text;
        }

        static void SetArray(SerializedProperty property, Object[] items)
        {
            property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
