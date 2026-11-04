using System.Collections.Generic;
using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관 입구 안내판을 "이야기 보기"로 만든다 (2026-11-05 사용자 요청). 도움말은 메인 화면에 있으니, 이 안내판은 이야기 쪽을 넘겨 보는 곳이다.
    //   Canvas/StoryPopup: 전투 장소 선택 팝업과 같은 모양(어두운 배경 + Popup_Panel 창 + Button_Close)에 쪽 그림 한 장, [이전] [다음] 버튼, "1 / 6" 쪽 번호.
    //                      쪽 그림은 Assets/UI/Story_01.png, Story_02.png … 순서대로 모두 읽어서 넣는다 (임시 이야기 6쪽. 사용자가 같은 이름으로 덮어쓰거나 더 추가).
    //   Canvas/Prompt_Story: 가까이 가면 뜨는 "[E] 이야기 보기".
    //   StorySign: 입구(시작 위치 왼쪽 앞)의 나무 안내판 (3D, 큐브 조합). 판 얼굴 그림은 Assets/Textures/StorySign_Board.png (머티리얼 StorySign_Board의 Base Map).
    //              처음 들어오면 이야기가 저절로 한 번 열린다.
    //   예전 안내판(GuideSign, GuidePopup, Prompt_Guide)은 지운다. PNG·머티리얼은 있으면 덮어쓰지 않는다.
    // 메뉴: LastBullet > Phase 74 이야기 안내판 만들기 (다시 실행하면 안내판·창을 지우고 다시 만든다)
    public static class Phase74StorySignSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";

        [MenuItem("LastBullet/Phase 74 이야기 안내판 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var stagePopup = canvas != null ? canvas.transform.Find("StageSelectPopup") : null;
            var stagePrompt = canvas != null ? canvas.transform.Find("Prompt_StageSelect") : null;
            if (stagePopup == null || stagePrompt == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: 전투 장소 선택 팝업이 없어 건너뜀");
                return;
            }

            // 쪽 그림: Story_01.png부터 순서대로 있는 만큼
            var pages = new List<Sprite>();
            for (int n = 1; n < 100; n++)
            {
                string path = $"Assets/UI/Story_{n:00}.png";
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
                    break;
                pages.Add(ArtSlots.LoadSprite(path));
            }

            // 예전 안내판·창·문구와 지금 것을 지운다
            foreach (var name in new[] { "GuidePopup", "Prompt_Guide", "StoryPopup", "Prompt_Story" })
            {
                var old = canvas.transform.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }
            foreach (var name in new[] { "GuideSign", "StorySign" })
            {
                var old = GameObject.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old);
            }

            // ── 이야기 팝업: 전투 장소 선택 팝업의 배경·창·닫기·버튼을 복사해서 쓴다 ──
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            stagePopup.gameObject.SetActive(true);
            var popupGo = new GameObject("StoryPopup", typeof(RectTransform));
            popupGo.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)popupGo.transform);
            var dim = Object.Instantiate(stagePopup.Find("Dim").gameObject, popupGo.transform, false);
            dim.name = "Dim";
            var panel = Object.Instantiate(stagePopup.Find("Panel").gameObject, popupGo.transform, false);
            panel.name = "Panel";
            stagePopup.gameObject.SetActive(false);

            // 창에서 장소 버튼 3개 중 하나를 [이전]/[다음] 버튼 자리로 쓰려고 복사해 두고, 나머지와 색 글자는 지운다
            var buttonTemplate = Object.Instantiate(panel.transform.Find("ForestButton").gameObject);
            foreach (var child in new[] { "ForestButton", "CastleButton", "DesertButton", "ForestColors", "CastleColors", "DesertColors" })
            {
                var t = panel.transform.Find(child);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }
            ((RectTransform)panel.transform).sizeDelta = new Vector2(1500f, 900f);
            panel.transform.Find("Title").GetComponent<TMP_Text>().text = "이야기";
            ((RectTransform)panel.transform.Find("Title")).anchoredPosition = new Vector2(0f, -70f);

            // 쪽 그림 (1300×560)
            var imageGo = new GameObject("PageImage", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            imageGo.transform.SetParent(panel.transform, false);
            var imageRect = (RectTransform)imageGo.transform;
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = new Vector2(0f, 40f);
            imageRect.sizeDelta = new Vector2(1300f, 560f);
            var pageImage = imageGo.GetComponent<UnityEngine.UI.Image>();
            pageImage.sprite = pages.Count > 0 ? pages[0] : null;
            pageImage.color = Color.white; // 흰색 = 그림 색 그대로
            pageImage.preserveAspect = true;
            pageImage.raycastTarget = false;

            // 쪽 번호
            var numberGo = new GameObject("PageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
            numberGo.transform.SetParent(panel.transform, false);
            var numberRect = (RectTransform)numberGo.transform;
            numberRect.anchorMin = numberRect.anchorMax = new Vector2(0.5f, 0.5f);
            numberRect.anchoredPosition = new Vector2(0f, -360f);
            numberRect.sizeDelta = new Vector2(300f, 70f);
            var number = numberGo.GetComponent<TextMeshProUGUI>();
            number.font = font != null ? font : TMP_Settings.defaultFontAsset;
            number.fontSize = 40f;
            number.color = new Color(0.9f, 0.9f, 0.9f);
            number.alignment = TextAlignmentOptions.Center;
            number.raycastTarget = false;
            number.text = $"1 / {Mathf.Max(1, pages.Count)}";

            // [이전] [다음] 버튼
            var prev = MakeButton(buttonTemplate, panel.transform, "PrevButton", "이전", new Vector2(-480f, -360f));
            var next = MakeButton(buttonTemplate, panel.transform, "NextButton", "다음", new Vector2(480f, -360f));
            Object.DestroyImmediate(buttonTemplate);

            var popup = popupGo.AddComponent<StoryPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("closeButton").objectReferenceValue = panel.transform.Find("CloseButton").GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("pageImage").objectReferenceValue = pageImage;
            so.FindProperty("prevButton").objectReferenceValue = prev.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("nextButton").objectReferenceValue = next.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("nextLabel").objectReferenceValue = next.transform.Find("Label").GetComponent<TMP_Text>();
            so.FindProperty("pageNumber").objectReferenceValue = number;
            var array = so.FindProperty("pages");
            array.arraySize = pages.Count;
            for (int i = 0; i < pages.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            popupGo.SetActive(false);

            // ── 안내 문구 "[E] 이야기 보기" ──
            var prompt = Object.Instantiate(stagePrompt.gameObject, canvas.transform, false);
            prompt.name = "Prompt_Story";
            prompt.GetComponent<TMP_Text>().text = "[E] 이야기 보기";
            prompt.SetActive(false);

            // ── 안내판 (3D) ──
            var wood = ArtSlots.GetOrCreateMaterial("StorySign_Wood", new Color(0.34f, 0.22f, 0.12f));
            var boardMat = ArtSlots.GetOrCreateMaterial("StorySign_Board", Color.white);
            var boardTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/StorySign_Board.png");
            if (boardTexture != null && boardMat.GetTexture("_BaseMap") == null)
            {
                boardMat.SetTexture("_BaseMap", boardTexture); // 이미 그림을 넣어 둔 머티리얼은 그대로 둔다
                boardMat.SetTextureScale("_BaseMap", Vector2.one);
                boardMat.SetTextureOffset("_BaseMap", Vector2.zero);
            }
            var sign = new GameObject("StorySign");
            sign.transform.position = new Vector3(-2.6f, 0f, 0.9f);
            sign.transform.rotation = Quaternion.LookRotation(new Vector3(0.8f, 0f, -0.45f)); // 시작 위치 쪽을 본다 (앞면 +Z)
            Part("PostLeft", sign.transform, new Vector3(-0.62f, 0.8f, 0f), new Vector3(0.1f, 1.6f, 0.1f), wood);
            Part("PostRight", sign.transform, new Vector3(0.62f, 0.8f, 0f), new Vector3(0.1f, 1.6f, 0.1f), wood);
            Part("Frame", sign.transform, new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 0.95f, 0.07f), wood);
            Part("Board", sign.transform, new Vector3(0f, 1.25f, 0.04f), new Vector3(1.36f, 0.81f, 0.03f), boardMat);
            var blocker = sign.AddComponent<BoxCollider>(); // 지나가다 통과하지 않게
            blocker.center = new Vector3(0f, 0.8f, 0f);
            blocker.size = new Vector3(1.5f, 1.6f, 0.3f);

            var device = sign.AddComponent<StorySign>();
            var deviceSo = new SerializedObject(device);
            deviceSo.FindProperty("popup").objectReferenceValue = popup;
            deviceSo.FindProperty("prompt").objectReferenceValue = prompt;
            deviceSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 1_Museum: 이야기 안내판과 이야기 팝업({pages.Count}쪽)을 만들었어요.");
        }

        // 복사해 둔 장소 버튼(Button_Normal 9-slice + 글자)을 작은 [이전]/[다음] 버튼으로 바꿔 창에 붙인다
        static GameObject MakeButton(GameObject template, Transform parent, string name, string label, Vector2 position)
        {
            var go = Object.Instantiate(template, parent, false);
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 90f);
            var text = go.transform.Find("Label").GetComponent<TMP_Text>();
            text.text = label;
            text.fontSize = 42f;
            ((RectTransform)text.transform).sizeDelta = new Vector2(260f, 80f);
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); // 복사된 장소 이동 연결을 비운다
            return go;
        }

        static void Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
