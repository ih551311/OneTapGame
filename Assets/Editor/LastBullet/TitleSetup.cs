using System.Linq;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 타이틀 메인 메뉴 씬(0_Title)을 만든다: 배경, 로고, 시작/도움말/옵션 버튼,
    // 도움말 팝업, 옵션 팝업(소리 크기, 마우스 감도, 게임 나가기), 그림 보기(갤러리) 팝업 틀.
    // 빌드 목록 맨 앞(게임 시작 씬)에 넣는다.
    // 메뉴: LastBullet > 타이틀 메인 메뉴 만들기
    public static class TitleSetup
    {
        internal const string ScenePath = "Assets/Scenes/0_Title.unity";
        const float SliderBorder = 12f;

        // 타이틀 액자 크기: 정사각형 고정 (그림 종이가 정사각형이라서)
        internal const float TitleFrameSize = 360f;

        // 메뉴 버튼 위치 (화면 가운데 기준): 시작, 그림 보기, 도움말, 옵션
        internal static readonly Vector2[] MenuButtonPositions =
        {
            new Vector2(0f, -20f), new Vector2(0f, -150f), new Vector2(0f, -280f), new Vector2(0f, -410f),
        };

        [MenuItem("LastBullet/타이틀 메인 메뉴 만들기")]
        static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 0_Title 씬이 있어요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라 (UI 뒤 배경색)
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x1C, 0x1C, 0x22, 0xFF);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // 캔버스 (모든 UI는 여기)
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var canvas = canvasObject.transform;
            var center = new Vector2(0.5f, 0.5f);

            // 배경과 로고
            var background = Phase4StageSelectSetup.CreateImage("Background", canvas, "Assets/UI/Title_Background.png");
            background.GetComponent<Image>().preserveAspect = false;
            Phase4StageSelectSetup.Stretch(background.GetComponent<RectTransform>());
            var logo = Phase4StageSelectSetup.CreateImage("Logo", canvas, "Assets/UI/Title_Logo.png");
            Phase4StageSelectSetup.SetRect(logo, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1000f, 312f));

            // 양쪽 액자 (그림 보기에서 고른 그림이 걸린다)
            var titleFrames = CreateTitleFrames(canvas);

            // 메뉴 버튼: 시작 / 그림 보기 / 도움말 / 옵션
            var menu = new GameObject("Menu", typeof(RectTransform));
            menu.transform.SetParent(canvas, false);
            Phase4StageSelectSetup.Stretch(menu.GetComponent<RectTransform>());
            var startButton = Phase4StageSelectSetup.CreateStageButton("StartButton", menu.transform, "Assets/UI/ButtonLabel_Start.png", MenuButtonPositions[0]);
            var galleryButton = Phase4StageSelectSetup.CreateStageButton("GalleryButton", menu.transform, "Assets/UI/ButtonLabel_Gallery.png", MenuButtonPositions[1]);
            var helpButton = Phase4StageSelectSetup.CreateStageButton("HelpButton", menu.transform, "Assets/UI/ButtonLabel_Help.png", MenuButtonPositions[2]);
            var optionsButton = Phase4StageSelectSetup.CreateStageButton("OptionsButton", menu.transform, "Assets/UI/ButtonLabel_Options.png", MenuButtonPositions[3]);

            // 도움말 팝업
            var help = CreatePopup<HelpPopup>("HelpPopup", canvas, new Vector2(1160f, 820f), "Assets/UI/Help_Title.png", out var helpPanel);
            var helpContent = Phase4StageSelectSetup.CreateImage("Content", helpPanel, "Assets/UI/Help_Content.png");
            Phase4StageSelectSetup.SetRect(helpContent, center, new Vector2(0f, -50f), new Vector2(1024f, 576f));

            // 옵션 팝업
            var options = CreatePopup<OptionsPopup>("OptionsPopup", canvas, new Vector2(900f, 620f), "Assets/UI/Options_Title.png", out var optionsPanel);
            var volumeLabel = Phase4StageSelectSetup.CreateImage("VolumeLabel", optionsPanel, "Assets/UI/Label_Volume.png");
            Phase4StageSelectSetup.SetRect(volumeLabel, center, new Vector2(-220f, 90f), new Vector2(260f, 64f));
            var volumeSlider = CreateSlider("VolumeSlider", optionsPanel, new Vector2(140f, 90f));
            var sensitivityLabel = Phase4StageSelectSetup.CreateImage("SensitivityLabel", optionsPanel, "Assets/UI/Label_Sensitivity.png");
            Phase4StageSelectSetup.SetRect(sensitivityLabel, center, new Vector2(-220f, -10f), new Vector2(260f, 64f));
            var sensitivitySlider = CreateSlider("SensitivitySlider", optionsPanel, new Vector2(140f, -10f));
            var quitButton = Phase4StageSelectSetup.CreateStageButton("QuitButton", optionsPanel, "Assets/UI/ButtonLabel_Quit.png", new Vector2(0f, -170f));
            var optionsSo = new SerializedObject(options);
            optionsSo.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
            optionsSo.FindProperty("sensitivitySlider").objectReferenceValue = sensitivitySlider;
            optionsSo.FindProperty("quitButton").objectReferenceValue = quitButton;
            optionsSo.ApplyModifiedPropertiesWithoutUndo();

            // 그림 보기 팝업: 그림을 골라 왼쪽/오른쪽 액자에 건다
            var gallery = CreateGallery(canvas, titleFrames);

            var titleMenu = menu.AddComponent<TitleMenu>();
            var menuSo = new SerializedObject(titleMenu);
            menuSo.FindProperty("startButton").objectReferenceValue = startButton;
            menuSo.FindProperty("galleryButton").objectReferenceValue = galleryButton;
            menuSo.FindProperty("helpButton").objectReferenceValue = helpButton;
            menuSo.FindProperty("optionsButton").objectReferenceValue = optionsButton;
            menuSo.FindProperty("helpPopup").objectReferenceValue = help;
            menuSo.FindProperty("optionsPopup").objectReferenceValue = options;
            menuSo.FindProperty("galleryPopup").objectReferenceValue = gallery;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // 빌드 목록 맨 앞에 넣는다 (게임이 타이틀에서 시작)
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 타이틀 메인 메뉴 씬(0_Title)을 만들고 빌드 목록 맨 앞에 넣었어요.");
        }

        // 이미 만든 0_Title의 양쪽 액자를 정사각형으로 맞춘다.
        [MenuItem("LastBullet/타이틀 액자 정사각형으로 맞추기")]
        static void MakeTitleFramesSquare()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var frames = ArtSlots.FindCanvas().transform.Find("TitleFrames");
            if (frames == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "TitleFrames가 없어요. '타이틀에 그림 보기·액자 추가'를 먼저 실행하세요.", "확인");
                return;
            }
            foreach (Transform frame in frames)
                frame.GetComponent<RectTransform>().sizeDelta = Vector2.one * TitleFrameSize;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 타이틀 액자를 정사각형({TitleFrameSize}×{TitleFrameSize})으로 맞췄어요.");
        }

        // 이미 만든 0_Title에 "그림 보기" 버튼, 양쪽 액자, 새 그림 보기 팝업(고르기·걸기)을 더한다.
        [MenuItem("LastBullet/타이틀에 그림 보기·액자 추가")]
        static void AddGalleryToTitle()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var canvas = ArtSlots.FindCanvas().transform;
            if (canvas.Find("TitleFrames") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 TitleFrames가 있어요.", "확인");
                return;
            }

            // 1. 양쪽 액자 (로고 바로 뒤 = 버튼과 팝업보다 뒤에 그려진다)
            var frames = CreateTitleFrames(canvas);
            frames.transform.SetSiblingIndex(canvas.Find("Logo").GetSiblingIndex() + 1);

            // 2. 메뉴 버튼 4개로 다시 배치
            var menu = canvas.Find("Menu");
            var galleryButton = Phase4StageSelectSetup.CreateStageButton("GalleryButton", menu, "Assets/UI/ButtonLabel_Gallery.png", MenuButtonPositions[1]);
            galleryButton.transform.SetSiblingIndex(1);
            menu.Find("StartButton").GetComponent<RectTransform>().anchoredPosition = MenuButtonPositions[0];
            menu.Find("HelpButton").GetComponent<RectTransform>().anchoredPosition = MenuButtonPositions[2];
            menu.Find("OptionsButton").GetComponent<RectTransform>().anchoredPosition = MenuButtonPositions[3];

            // 3. 예전 그림 보기 팝업(틀만 있던 것)을 새것으로 바꾼다
            var oldGallery = canvas.Find("GalleryPopup");
            if (oldGallery != null)
                Object.DestroyImmediate(oldGallery.gameObject);
            var gallery = CreateGallery(canvas, frames);

            // 팝업들은 맨 위에 그려지게 마지막 순서로
            foreach (var popupName in new[] { "HelpPopup", "OptionsPopup", "GalleryPopup" })
                canvas.Find(popupName)?.SetAsLastSibling();

            var so = new SerializedObject(menu.GetComponent<TitleMenu>());
            so.FindProperty("galleryButton").objectReferenceValue = galleryButton;
            so.FindProperty("galleryPopup").objectReferenceValue = gallery;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 타이틀에 그림 보기 버튼, 양쪽 액자, 그림 고르기·걸기 팝업을 추가했어요.");
        }

        // 팝업 공통 모양: 어두운 막 + 9-slice 창 + 제목 + 닫기 버튼. 팝업은 꺼진 상태로 만든다.
        static T CreatePopup<T>(string name, Transform canvas, Vector2 size, string titlePath, out Transform panelTransform) where T : PopupBase
        {
            var popup = new GameObject(name, typeof(RectTransform));
            popup.transform.SetParent(canvas, false);
            Phase4StageSelectSetup.Stretch(popup.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(popup.transform, false);
            Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var panel = Phase4StageSelectSetup.CreateImage("Panel", popup.transform, "Assets/UI/Popup_Panel.png", Phase4StageSelectSetup.PanelBorder);
            panel.GetComponent<Image>().type = Image.Type.Sliced;
            panel.GetComponent<Image>().preserveAspect = false;
            Phase4StageSelectSetup.SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, size);

            var title = Phase4StageSelectSetup.CreateImage("Title", panel.transform, titlePath);
            Phase4StageSelectSetup.SetRect(title, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 100f));

            var close = Phase4StageSelectSetup.CreateImage("CloseButton", panel.transform, "Assets/UI/Button_Close.png");
            Phase4StageSelectSetup.SetRect(close, new Vector2(1f, 1f), new Vector2(-48f, -48f), new Vector2(64f, 64f));
            close.GetComponent<Image>().raycastTarget = true;
            var closeButton = close.AddComponent<Button>();

            var script = popup.AddComponent<T>();
            var so = new SerializedObject(script);
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.SetActive(false);

            panelTransform = panel.transform;
            return script;
        }

        // 가로 슬라이더: 배경 / 채움 / 손잡이 (모두 Assets/UI 그림)
        static Slider CreateSlider(string name, Transform parent, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            Phase4StageSelectSetup.SetRect(go, new Vector2(0.5f, 0.5f), position, new Vector2(420f, 36f));

            var background = SlicedImage("Background", go.transform, "Assets/UI/Slider_Background.png");
            Phase4StageSelectSetup.Stretch(background.GetComponent<RectTransform>());

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Phase4StageSelectSetup.Stretch(fillArea.GetComponent<RectTransform>());
            var fill = SlicedImage("Fill", fillArea.transform, "Assets/UI/Slider_Fill.png");
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.sizeDelta = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            Phase4StageSelectSetup.Stretch(handleAreaRect);
            handleAreaRect.offsetMin = new Vector2(18f, 0f); // 손잡이가 끝에서 잘리지 않게
            handleAreaRect.offsetMax = new Vector2(-18f, 0f);
            var handle = Phase4StageSelectSetup.CreateImage("Handle", handleArea.transform, "Assets/UI/Slider_Handle.png");
            handle.GetComponent<Image>().raycastTarget = true;
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.sizeDelta = new Vector2(52f, 52f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 1f;
            return slider;
        }

        static GameObject SlicedImage(string name, Transform parent, string spritePath)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, parent, spritePath, SliderBorder);
            var image = go.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            return go;
        }

        // 타이틀 양쪽 액자 2개: 금색 액자(9-slice) 안에 그림(RawImage)
        internal static TitleFrames CreateTitleFrames(Transform canvas)
        {
            var root = new GameObject("TitleFrames", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            Phase4StageSelectSetup.Stretch(root.GetComponent<RectTransform>());

            var pictures = new RawImage[2];
            string[] names = { "FrameLeft", "FrameRight" };
            float[] xs = { -640f, 640f };
            for (int i = 0; i < 2; i++)
            {
                var frame = Phase4StageSelectSetup.CreateImage(names[i], root.transform, "Assets/UI/TitleFrame.png", 32f);
                frame.GetComponent<Image>().type = Image.Type.Sliced;
                frame.GetComponent<Image>().preserveAspect = false;
                Phase4StageSelectSetup.SetRect(frame, new Vector2(0.5f, 0.5f), new Vector2(xs[i], 20f), Vector2.one * TitleFrameSize);

                var picture = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
                picture.transform.SetParent(frame.transform, false);
                var rect = picture.GetComponent<RectTransform>();
                Phase4StageSelectSetup.Stretch(rect);
                rect.offsetMin = new Vector2(30f, 30f); // 액자 테두리 안쪽
                rect.offsetMax = new Vector2(-30f, -30f);
                pictures[i] = picture.GetComponent<RawImage>();
                pictures[i].raycastTarget = false;
                pictures[i].enabled = false;
            }

            var frames = root.AddComponent<TitleFrames>();
            var so = new SerializedObject(frames);
            var array = so.FindProperty("pictures");
            array.arraySize = 2;
            for (int i = 0; i < 2; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = pictures[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return frames;
        }

        internal static GalleryPopup CreateGallery(Transform canvas, TitleFrames titleFrames)
        {
            var gallery = CreatePopup<GalleryPopup>("GalleryPopup", canvas, new Vector2(1400f, 920f), "Assets/UI/Gallery_Title.png", out var panel);

            // 스크롤 영역: 보이는 창(Viewport, 넘치는 부분은 가림) 안에 그림 칸들(Content)
            var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scroll.transform.SetParent(panel, false);
            // 2026-10-31: 두 줄(칸 280 × 2 + 간격 30 + 위아래 여백 10 × 2 = 610)이 잘리지 않게 높이 620 (예전 600은 아래가 잘렸다)
            Phase4StageSelectSetup.SetRect(scroll, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1300f, 620f));
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scroll.transform, false);
            Phase4StageSelectSetup.Stretch(viewport.GetComponent<RectTransform>());
            var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(280f, 280f);
            grid.spacing = new Vector2(30f, 30f);
            grid.padding = new RectOffset(20, 20, 10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;

            // 그림 한 칸의 틀: 누를 수 있는 액자(9-slice) 안에 그림(RawImage), 고르면 노란 테두리(Selected)
            var item = Phase4StageSelectSetup.CreateImage("Item", content.transform, "Assets/UI/Gallery_Frame.png", 24f);
            item.GetComponent<Image>().type = Image.Type.Sliced;
            item.GetComponent<Image>().preserveAspect = false;
            item.GetComponent<Image>().raycastTarget = true;
            item.AddComponent<Button>();
            var picture = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
            picture.transform.SetParent(item.transform, false);
            var pictureRect = picture.GetComponent<RectTransform>();
            Phase4StageSelectSetup.Stretch(pictureRect);
            pictureRect.offsetMin = new Vector2(26f, 26f);
            pictureRect.offsetMax = new Vector2(-26f, -26f);
            picture.GetComponent<RawImage>().raycastTarget = false;
            var selected = Phase4StageSelectSetup.CreateImage("Selected", item.transform, "Assets/UI/Gallery_Selected.png", 24f);
            selected.GetComponent<Image>().type = Image.Type.Sliced;
            selected.GetComponent<Image>().preserveAspect = false;
            Phase4StageSelectSetup.Stretch(selected.GetComponent<RectTransform>());
            selected.SetActive(false);
            item.SetActive(false);

            var empty = Phase4StageSelectSetup.CreateImage("EmptyMessage", panel, "Assets/UI/Gallery_Empty.png");
            Phase4StageSelectSetup.SetRect(empty, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(600f, 100f));

            // 고른 그림을 타이틀 액자에 거는 버튼
            var hangLeft = Phase4StageSelectSetup.CreateStageButton("HangLeftButton", panel, "Assets/UI/ButtonLabel_HangLeft.png", new Vector2(-240f, -360f));
            var hangRight = Phase4StageSelectSetup.CreateStageButton("HangRightButton", panel, "Assets/UI/ButtonLabel_HangRight.png", new Vector2(240f, -360f));
            hangLeft.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 100f);
            hangRight.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 100f);

            var so = new SerializedObject(gallery);
            so.FindProperty("content").objectReferenceValue = contentRect;
            so.FindProperty("itemTemplate").objectReferenceValue = item;
            so.FindProperty("emptyMessage").objectReferenceValue = empty;
            so.FindProperty("hangLeftButton").objectReferenceValue = hangLeft;
            so.FindProperty("hangRightButton").objectReferenceValue = hangRight;
            so.FindProperty("titleFrames").objectReferenceValue = titleFrames;
            so.ApplyModifiedPropertiesWithoutUndo();
            return gallery;
        }
    }
}
