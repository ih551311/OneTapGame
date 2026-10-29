using LastBullet.Player;
using LastBullet.Stage;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 미술관(Hideout)에 지도 탁자를 놓고, E로 여는 "전투 장소 선택" 팝업을 만든다.
    // 팝업의 그림(창, 버튼 상태, 글자)은 모두 Assets/UI의 PNG라서 직접 그린 그림으로 바꿀 수 있다.
    // 메뉴: LastBullet > Phase 4-5 미술관 전투 장소 선택 만들기
    public static class Phase4StageSelectSetup
    {
        const float PanelBorder = 32f;   // Popup_Panel.png 9-slice 모서리
        const float ButtonBorder = 24f;  // Button_*.png 9-slice 모서리

        [MenuItem("LastBullet/Phase 4-5 미술관 전투 장소 선택 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Hideout.unity");

            var player = GameObject.Find("Player");
            var canvas = ArtSlots.FindCanvas();
            if (player == null || canvas == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 4-4 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (GameObject.Find("StageMap") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 StageMap이 있어요.", "확인");
                return;
            }

            // 1. 팝업이 열리면 조작을 막는 스크립트
            if (player.GetComponent<PlayerControlLock>() == null)
                player.AddComponent<PlayerControlLock>();

            // 2. 버튼을 마우스로 누르려면 EventSystem이 필요하다 (New Input System용 모듈)
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // 3. 지도 탁자 (임시 상자, 전용 머티리얼)
            var map = GameObject.CreatePrimitive(PrimitiveType.Cube);
            map.name = "StageMap";
            map.transform.position = new Vector3(0f, 0.5f, 5f);
            map.transform.localScale = new Vector3(1.6f, 1f, 1f);
            map.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("StageMap", new Color32(0x8B, 0x5A, 0x2B, 0xFF));
            var device = map.AddComponent<StageSelectDevice>();

            // 4. 가까이 가면 뜨는 안내 (화면 아래 가운데)
            var prompt = CreateImage("Prompt_StageSelect", canvas.transform, "Assets/UI/Prompt_StageSelect.png");
            SetRect(prompt, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(480f, 60f));
            prompt.SetActive(false);

            // 5. 팝업: 화면 전체(어둡게) + 가운데 창
            var popup = new GameObject("StageSelectPopup", typeof(RectTransform));
            popup.transform.SetParent(canvas.transform, false);
            Stretch(popup.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(popup.transform, false);
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f); // 뒤쪽 클릭을 막는 반투명 막

            var panel = CreateImage("Panel", popup.transform, "Assets/UI/Popup_Panel.png", PanelBorder);
            panel.GetComponent<Image>().type = Image.Type.Sliced;
            panel.GetComponent<Image>().preserveAspect = false;
            SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 560f));

            var title = CreateImage("Title", panel.transform, "Assets/UI/StageSelect_Title.png");
            SetRect(title, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 100f));

            var training = CreateStageButton("TrainingButton", panel.transform, "Assets/UI/StageLabel_Training.png", new Vector2(0f, 20f));
            var forest = CreateStageButton("ForestButton", panel.transform, "Assets/UI/StageLabel_Forest.png", new Vector2(0f, -130f));

            var close = CreateImage("CloseButton", panel.transform, "Assets/UI/Button_Close.png");
            SetRect(close, new Vector2(1f, 1f), new Vector2(-48f, -48f), new Vector2(64f, 64f));
            close.GetComponent<Image>().raycastTarget = true;
            var closeButton = close.AddComponent<Button>(); // 마우스를 올리면 살짝 어두워진다

            var popupScript = popup.AddComponent<StageSelectPopup>();
            var popupSo = new SerializedObject(popupScript);
            popupSo.FindProperty("trainingButton").objectReferenceValue = training;
            popupSo.FindProperty("forestButton").objectReferenceValue = forest;
            popupSo.FindProperty("closeButton").objectReferenceValue = closeButton;
            popupSo.ApplyModifiedPropertiesWithoutUndo();
            popup.SetActive(false);

            var deviceSo = new SerializedObject(device);
            deviceSo.FindProperty("popup").objectReferenceValue = popupScript;
            deviceSo.FindProperty("prompt").objectReferenceValue = prompt;
            deviceSo.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = map;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 4-5: 미술관에 StageMap(지도 탁자)과 전투 장소 선택 팝업을 만들고 Hideout 씬을 저장했어요.");
        }

        // 버튼: 상태별 그림(보통/마우스 올림/누름/잠김) + 글자 그림
        static Button CreateStageButton(string name, Transform parent, string labelPath, Vector2 position)
        {
            var go = CreateImage(name, parent, "Assets/UI/Button_Normal.png", ButtonBorder);
            var image = go.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.raycastTarget = true;
            SetRect(go, new Vector2(0.5f, 0.5f), position, new Vector2(600f, 120f));

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = ArtSlots.LoadSprite("Assets/UI/Button_Highlighted.png", ButtonBorder),
                pressedSprite = ArtSlots.LoadSprite("Assets/UI/Button_Pressed.png", ButtonBorder),
                selectedSprite = ArtSlots.LoadSprite("Assets/UI/Button_Normal.png", ButtonBorder),
                disabledSprite = ArtSlots.LoadSprite("Assets/UI/Button_Disabled.png", ButtonBorder),
            };

            var label = CreateImage("Label", go.transform, labelPath);
            SetRect(label, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 100f));
            return button;
        }

        static GameObject CreateImage(string name, Transform parent, string spritePath, float border = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = ArtSlots.LoadSprite(spritePath, border);
            image.color = Color.white; // 그림 색 그대로
            image.preserveAspect = true;
            image.raycastTarget = false;
            return go;
        }

        static void SetRect(GameObject go, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
