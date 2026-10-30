using LastBullet.Drawing;
using LastBullet.Inventory;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 타이틀 화면에 "내 프로필"을 넣는다 (2026-10-31 사용자 요청).
    //  - 왼쪽: 프로필 카드(사진 액자 + 닉네임). 누르면 프로필 창이 열린다
    //  - 오른쪽: 걸어 둔 그림 액자 두 개 (예전엔 양쪽에 하나씩이었다)
    //  - 프로필 창: [직접 그리기](물감·종이 안 씀, 완성하면 프로필 사진) / [그림에서 고르기](미술관에서 그린 그림 중)
    //  - 옵션 창: 닉네임 입력 칸 (최대 10글자, 한글 글꼴 font1 SDF)
    // 모든 그림은 Assets/UI의 PNG다 (Profile_*, ProfileDraw_Title, ProfilePick_Title, ButtonLabel_Profile*, Label_Nickname, Input_Background).
    // 이미 있으면 건너뛴다. 메뉴: LastBullet > Phase 32 프로필(사진·닉네임) 넣기
    public static class Phase32ProfileSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";

        [MenuItem("LastBullet/Phase 32 프로필(사진·닉네임) 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(TitleSetup.ScenePath);
            var canvas = ArtSlots.FindCanvas().transform;
            if (canvas.Find("ProfileCard") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 ProfileCard가 있어요.", "확인");
                return;
            }

            MoveFramesToRight(canvas);
            var card = BuildCard(canvas, out var cardScript);
            var profilePopup = BuildProfilePopup(canvas, out var previewFrame);
            var drawPopup = BuildDrawPopup(canvas);
            var pickPopup = BuildPicker(canvas);
            ConnectProfile(cardScript, profilePopup, previewFrame, drawPopup, pickPopup, card);
            AddNicknameToOptions(canvas);

            // 그리는 순서: 카드는 액자 바로 뒤, 팝업은 맨 위
            card.transform.SetSiblingIndex(canvas.Find("TitleFrames").GetSiblingIndex() + 1);
            foreach (var name in new[] { "HelpPopup", "OptionsPopup", "GalleryPopup", "ProfilePopup", "ProfileDrawPopup", "ProfilePickerPopup" })
                canvas.Find(name)?.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 32: 타이틀에 프로필 카드, 프로필 창, 그리기·고르기 창, 옵션의 닉네임 입력을 넣었어요.");
        }

        static TMP_FontAsset Font()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            return font != null ? font : TMP_Settings.defaultFontAsset;
        }

        // ───────────── 오른쪽 액자 두 개 ─────────────

        static void MoveFramesToRight(Transform canvas)
        {
            var frames = canvas.Find("TitleFrames");
            float[] xs = { 500f, 790f };
            string[] names = { "FrameLeft", "FrameRight" };
            for (int i = 0; i < 2; i++)
                Phase4StageSelectSetup.SetRect(frames.Find(names[i]).gameObject, new Vector2(0.5f, 0.5f), new Vector2(xs[i], 40f), Vector2.one * 260f);
        }

        // ───────────── 프로필 카드 (왼쪽) ─────────────

        static GameObject BuildCard(Transform canvas, out ProfileCard script)
        {
            var root = new GameObject("ProfileCard", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            Phase4StageSelectSetup.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(-650f, 70f), new Vector2(340f, 340f));

            var frame = Phase6PaintBoardSetup.Sliced("Frame", root.transform, "Assets/UI/TitleFrame.png", 32f);
            Phase4StageSelectSetup.Stretch(frame.GetComponent<RectTransform>());
            frame.GetComponent<Image>().raycastTarget = true;
            var button = frame.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 0.93f, 0.75f),
                pressedColor = new Color(0.8f, 0.72f, 0.58f),
                selectedColor = Color.white,
                disabledColor = Color.gray,
                colorMultiplier = 1f,
                fadeDuration = 0.1f,
            };

            var picture = Inset(new GameObject("Picture", typeof(RectTransform), typeof(RawImage)), frame.transform, 30f);
            var raw = picture.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.enabled = false;

            var empty = Phase4StageSelectSetup.CreateImage("Empty", frame.transform, "Assets/UI/Profile_Empty.png");
            Inset(empty, frame.transform, 30f);
            empty.GetComponent<Image>().preserveAspect = false;

            // 닉네임 이름표
            var plate = Phase6PaintBoardSetup.Sliced("NamePlate", root.transform, "Assets/UI/Profile_NamePlate.png", 12f);
            Phase4StageSelectSetup.SetRect(plate, new Vector2(0.5f, 0.5f), new Vector2(0f, -235f), new Vector2(340f, 74f));
            var nickname = Text("Nickname", plate.transform, 40f, FontStyles.Bold, Color.white);
            Phase4StageSelectSetup.Stretch(nickname.GetComponent<RectTransform>());
            nickname.GetComponent<RectTransform>().offsetMin = new Vector2(14f, 4f);
            nickname.GetComponent<RectTransform>().offsetMax = new Vector2(-14f, -4f);
            nickname.enableAutoSizing = true; // 긴 이름은 글자가 줄어들어 판 안에 들어온다
            nickname.fontSizeMin = 22f;
            nickname.fontSizeMax = 40f;
            nickname.text = ProfileStore.DefaultNickname;

            var hint = Phase4StageSelectSetup.CreateImage("Hint", root.transform, "Assets/UI/Profile_Hint.png");
            Phase4StageSelectSetup.SetRect(hint, new Vector2(0.5f, 0.5f), new Vector2(0f, -298f), new Vector2(280f, 32f));

            script = root.AddComponent<ProfileCard>();
            var so = new SerializedObject(script);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("picture").objectReferenceValue = raw;
            so.FindProperty("emptyShape").objectReferenceValue = empty;
            so.FindProperty("nickname").objectReferenceValue = nickname;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        // ───────────── 프로필 창 ─────────────

        static ProfilePopup BuildProfilePopup(Transform canvas, out Transform previewFrame)
        {
            var popup = TitleSetup.CreatePopup<ProfilePopup>("ProfilePopup", canvas, new Vector2(900f, 760f), "Assets/UI/Profile_Title.png", out var panel);

            var frame = Phase6PaintBoardSetup.Sliced("PreviewFrame", panel, "Assets/UI/TitleFrame.png", 32f);
            Phase4StageSelectSetup.SetRect(frame, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), Vector2.one * 240f);
            previewFrame = frame.transform;
            var picture = Inset(new GameObject("Picture", typeof(RectTransform), typeof(RawImage)), frame.transform, 22f);
            picture.GetComponent<RawImage>().raycastTarget = false;
            picture.GetComponent<RawImage>().enabled = false;
            var empty = Phase4StageSelectSetup.CreateImage("Empty", frame.transform, "Assets/UI/Profile_Empty.png");
            Inset(empty, frame.transform, 22f);
            empty.GetComponent<Image>().preserveAspect = false;

            var draw = WideButton("DrawButton", panel, "Assets/UI/ButtonLabel_ProfileDraw.png", new Vector2(0f, -110f));
            var pick = WideButton("PickButton", panel, "Assets/UI/ButtonLabel_ProfilePick.png", new Vector2(0f, -250f));

            var so = new SerializedObject(popup);
            so.FindProperty("drawButton").objectReferenceValue = draw;
            so.FindProperty("pickButton").objectReferenceValue = pick;
            so.FindProperty("preview").objectReferenceValue = picture.GetComponent<RawImage>();
            so.FindProperty("emptyShape").objectReferenceValue = empty;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        static Button WideButton(string name, Transform parent, string labelPath, Vector2 position)
        {
            var button = Phase4StageSelectSetup.CreateStageButton(name, parent, labelPath, position);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 110f);
            button.transform.Find("Label").GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 90f);
            return button;
        }

        // ───────────── 프로필 그리기 창 (그림판과 같은 모양, 물감·종이 없음) ─────────────

        static ProfileDrawPopup BuildDrawPopup(Transform canvas)
        {
            var popup = new GameObject("ProfileDrawPopup", typeof(RectTransform));
            popup.transform.SetParent(canvas, false);
            Phase4StageSelectSetup.Stretch(popup.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(popup.transform, false);
            Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var panel = Phase6PaintBoardSetup.Sliced("Panel", popup.transform, "Assets/UI/Paint_Panel.png", 40f);
            Phase4StageSelectSetup.SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 900f));
            var p = panel.transform;

            Phase6PaintBoardSetup.Place(Phase4StageSelectSetup.CreateImage("Title", p, "Assets/UI/ProfileDraw_Title.png"), 380f, 80f, 600f, 100f);

            // 도구 막대: 펜 굵기 4단계 + 지우개
            Phase6PaintBoardSetup.Place(Phase6PaintBoardSetup.Sliced("Toolbar", p, "Assets/UI/Paint_Toolbar.png", 28f), 104f, 386f, 100f, 532f);
            var toolPaths = new[] { "Paint_Tool_Pen1", "Paint_Tool_Pen2", "Paint_Tool_Pen3", "Paint_Tool_Pen4", "Paint_Tool_Eraser" };
            var toolButtons = new Button[toolPaths.Length];
            for (int i = 0; i < toolPaths.Length; i++)
                toolButtons[i] = IconButton(toolPaths[i], p, $"Assets/UI/{toolPaths[i]}.png", 104f, 176f + i * 94f, 80f);
            var toolSelected = Marker("ToolSelected", p, "Assets/UI/Paint_Tool_Selected.png");

            // 종이
            Phase6PaintBoardSetup.Place(Phase6PaintBoardSetup.Sliced("CanvasFrame", p, "Assets/UI/Paint_CanvasFrame.png", 24f), 440f, 386f, 532f, 532f);
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(RawImage));
            canvasObject.transform.SetParent(p, false);
            Phase6PaintBoardSetup.Place(canvasObject, 440f, 386f, 512f, 512f);
            var canvasImage = canvasObject.GetComponent<RawImage>();
            canvasImage.color = Color.white;
            canvasImage.raycastTarget = true;

            // 색 9개 (물감 잔량 없음)
            var colorButtons = new Button[PaintColors.Count];
            for (int i = 0; i < PaintColors.Count; i++)
            {
                string colorName = ((PaintColor)i).ToString();
                float x = 54f + 36.2f + i * 72.4f;
                colorButtons[i] = IconButton($"Color_{colorName}", p, $"Assets/UI/Paint_Color_{colorName}.png", x, 710f, 56f);
            }
            var colorSelected = Marker("ColorSelected", p, "Assets/UI/Paint_Color_Selected.png");

            var cancel = Phase6PaintBoardSetup.PaintButton("CancelButton", p, "Cancel", "Cancel", false, 210f, 810f);
            var done = Phase6PaintBoardSetup.PaintButton("DoneButton", p, "Done", "Done", true, 550f, 810f);

            var script = popup.AddComponent<ProfileDrawPopup>();
            var so = new SerializedObject(script);
            SetArray(so.FindProperty("toolButtons"), toolButtons);
            so.FindProperty("toolSelected").objectReferenceValue = toolSelected;
            SetArray(so.FindProperty("colorButtons"), colorButtons);
            so.FindProperty("colorSelected").objectReferenceValue = colorSelected;
            so.FindProperty("canvasImage").objectReferenceValue = canvasImage;
            so.FindProperty("doneButton").objectReferenceValue = done;
            so.FindProperty("closeButton").objectReferenceValue = cancel; // 취소 = 닫기 (그린 것은 버려진다)
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.SetActive(false);
            return script;
        }

        // ───────────── 그림에서 고르기 창 (그림 보기와 같은 목록, 눌러서 고르면 바로 프로필 사진) ─────────────

        static GalleryPopup BuildPicker(Transform canvas)
        {
            var picker = TitleSetup.CreateGallery(canvas, null);
            picker.name = "ProfilePickerPopup";
            var so = new SerializedObject(picker);
            so.FindProperty("profileMode").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            var title = picker.transform.Find("Panel/Title");
            title.GetComponent<Image>().sprite = ArtSlots.LoadSprite("Assets/UI/ProfilePick_Title.png");
            Phase31ScrollbarSetup.AddScrollbar(picker.transform.Find("Panel/Scroll"));
            return picker;
        }

        static void ConnectProfile(ProfileCard card, ProfilePopup popup, Transform previewFrame, ProfileDrawPopup draw, GalleryPopup picker, GameObject cardObject)
        {
            var cardSo = new SerializedObject(card);
            cardSo.FindProperty("profilePopup").objectReferenceValue = popup;
            cardSo.ApplyModifiedPropertiesWithoutUndo();

            var popupSo = new SerializedObject(popup);
            popupSo.FindProperty("drawPopup").objectReferenceValue = draw;
            popupSo.FindProperty("pickPopup").objectReferenceValue = picker;
            popupSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // ───────────── 옵션: 닉네임 입력 ─────────────

        static void AddNicknameToOptions(Transform canvas)
        {
            var optionsPopup = canvas.Find("OptionsPopup");
            var panel = optionsPopup.Find("Panel");
            var center = new Vector2(0.5f, 0.5f);

            // 줄이 하나 늘어서 창을 키우고 줄 위치를 다시 잡는다 (제목은 위에 붙어 있어 그대로)
            Phase4StageSelectSetup.SetRect(panel.gameObject, center, Vector2.zero, new Vector2(900f, 760f));
            Phase4StageSelectSetup.SetRect(panel.Find("VolumeLabel").gameObject, center, new Vector2(-220f, 70f), new Vector2(260f, 64f));
            panel.Find("VolumeSlider").GetComponent<RectTransform>().anchoredPosition = new Vector2(140f, 70f);
            Phase4StageSelectSetup.SetRect(panel.Find("SensitivityLabel").gameObject, center, new Vector2(-220f, -30f), new Vector2(260f, 64f));
            panel.Find("SensitivitySlider").GetComponent<RectTransform>().anchoredPosition = new Vector2(140f, -30f);
            panel.Find("QuitButton").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -210f);

            var label = Phase4StageSelectSetup.CreateImage("NicknameLabel", panel, "Assets/UI/Label_Nickname.png");
            Phase4StageSelectSetup.SetRect(label, center, new Vector2(-220f, 170f), new Vector2(260f, 64f));
            var input = CreateInputField("NicknameInput", panel, new Vector2(140f, 170f), new Vector2(420f, 64f));

            var so = new SerializedObject(optionsPopup.GetComponent<OptionsPopup>());
            so.FindProperty("nicknameInput").objectReferenceValue = input;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_InputField CreateInputField(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = Phase6PaintBoardSetup.Sliced(name, parent, "Assets/UI/Input_Background.png", 12f);
            Phase4StageSelectSetup.SetRect(go, new Vector2(0.5f, 0.5f), position, size);
            go.GetComponent<Image>().raycastTarget = true;
            var input = go.AddComponent<TMP_InputField>();

            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(go.transform, false);
            var areaRect = area.GetComponent<RectTransform>();
            Phase4StageSelectSetup.Stretch(areaRect);
            areaRect.offsetMin = new Vector2(18f, 6f);
            areaRect.offsetMax = new Vector2(-18f, -6f);

            var placeholder = Text("Placeholder", area.transform, 34f, FontStyles.Italic, new Color(1f, 1f, 1f, 0.4f));
            Phase4StageSelectSetup.Stretch(placeholder.GetComponent<RectTransform>());
            placeholder.text = "닉네임을 입력하세요";
            placeholder.alignment = TextAlignmentOptions.Left;

            var text = Text("Text", area.transform, 34f, FontStyles.Normal, Color.white);
            Phase4StageSelectSetup.Stretch(text.GetComponent<RectTransform>());
            text.alignment = TextAlignmentOptions.Left;
            text.richText = false;

            input.textViewport = areaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = Font();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = ProfileStore.MaxNicknameLength;
            input.caretColor = Color.white;
            input.customCaretColor = true;
            input.selectionColor = new Color(0.4f, 0.6f, 1f, 0.5f);
            input.richText = false;
            return input;
        }

        // ───────────── 도우미 ─────────────

        static TextMeshProUGUI Text(string name, Transform parent, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = Font();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        // 부모 안쪽으로 margin만큼 띄워 꽉 채운다
        static GameObject Inset(GameObject go, Transform parent, float margin)
        {
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return go;
        }

        static Button IconButton(string name, Transform parent, string path, float x, float y, float size)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, parent, path);
            go.GetComponent<Image>().raycastTarget = true;
            Phase6PaintBoardSetup.Place(go, x, y, size, size);
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

        static void SetArray(SerializedProperty property, Object[] items)
        {
            property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
