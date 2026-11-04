using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 미술관 입구 안내판과 안내 팝업을 만든다 (2026-11-05 사용자 요청: 처음 들어온 사람에게 조작을 알려 주는 튜토리얼).
    //   Canvas/GuidePopup: 전투 장소 선택 팝업과 같은 모양(어두운 배경 + Popup_Panel 창 + Button_Close)의 안내 창.
    //                      왼쪽 "조작", 오른쪽 "규칙"(메인 화면 도움말과 같은 내용. 나중에 이 안내판은 스토리 보기로 쓸 예정 — 2026-11-05 사용자)은 글자가 아니라 그림이다: Assets/UI/Guide_Controls.png, Guide_Flow.png (사용자가 같은 이름으로 덮어써서 바꾼다).
    //   Canvas/Prompt_Guide: 가까이 가면 뜨는 "[E] 안내판 읽기".
    //   GuideSign: 입구(시작 위치 왼쪽 앞)에 서 있는 나무 안내판. 판의 앞면 그림은 Assets/Textures/GuideSign_Board.png (머티리얼 GuideSign_Board의 Base Map). 처음 들어오면 팝업이 저절로 한 번 열린다.
    //   그림 파일(PNG)이 없으면 만들지 않는다 (있는 그림을 덮어쓰지 않는다).
    // 이미 만들었으면 지우고 다시 만든다. 메뉴: LastBullet > Phase 72 입구 안내판 만들기
    public static class Phase72GuideSignSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";

        [MenuItem("LastBullet/Phase 72 입구 안내판 만들기")]
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
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            foreach (var name in new[] { "GuidePopup", "Prompt_Guide" })
            {
                var old = canvas.transform.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }
            var oldSign = GameObject.Find("GuideSign");
            if (oldSign != null)
                Object.DestroyImmediate(oldSign);

            // ── 안내 팝업: 전투 장소 선택 팝업의 배경·창·닫기 버튼을 복사해서 쓴다 ──
            stagePopup.gameObject.SetActive(true);
            var popupGo = new GameObject("GuidePopup", typeof(RectTransform));
            popupGo.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)popupGo.transform);
            var dim = Object.Instantiate(stagePopup.Find("Dim").gameObject, popupGo.transform, false);
            dim.name = "Dim";
            var panel = Object.Instantiate(stagePopup.Find("Panel").gameObject, popupGo.transform, false);
            panel.name = "Panel";
            stagePopup.gameObject.SetActive(false);
            // 복사한 창에서 장소 버튼·색 글자는 지우고 제목과 닫기 버튼만 남긴다
            foreach (var child in new[] { "ForestButton", "CastleButton", "DesertButton", "ForestColors", "CastleColors", "DesertColors" })
            {
                var t = panel.transform.Find(child);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }
            ((RectTransform)panel.transform).sizeDelta = new Vector2(1500f, 900f);
            panel.transform.Find("Title").GetComponent<TMP_Text>().text = "도움말";
            var title = (RectTransform)panel.transform.Find("Title");
            title.anchoredPosition = new Vector2(0f, -70f);

            MakeImage(panel.transform, "ControlsImage", ArtSlots.LoadSprite("Assets/UI/Guide_Controls.png"), new Vector2(-370f, -60f), new Vector2(640f, 700f));
            MakeImage(panel.transform, "FlowImage", ArtSlots.LoadSprite("Assets/UI/Guide_Flow.png"), new Vector2(330f, -60f), new Vector2(700f, 700f));

            var popup = popupGo.AddComponent<GuidePopup>();
            var popupSo = new SerializedObject(popup);
            popupSo.FindProperty("closeButton").objectReferenceValue = panel.transform.Find("CloseButton").GetComponent<UnityEngine.UI.Button>();
            popupSo.ApplyModifiedPropertiesWithoutUndo();
            popupGo.SetActive(false);

            // ── 안내 문구 "[E] 안내판 읽기": 다른 안내 문구를 복사해서 글자만 바꾼다 ──
            var prompt = Object.Instantiate(stagePrompt.gameObject, canvas.transform, false);
            prompt.name = "Prompt_Guide";
            prompt.GetComponent<TMP_Text>().text = "[E] 도움말 보기";
            prompt.SetActive(false);

            // ── 안내판 ──
            var wood = ArtSlots.GetOrCreateMaterial("GuideSign_Wood", new Color(0.34f, 0.22f, 0.12f));
            var boardMat = ArtSlots.GetOrCreateMaterial("GuideSign_Board", Color.white);
            var boardTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/GuideSign_Board.png");
            if (boardTexture != null && boardMat.GetTexture("_BaseMap") == null)
            {
                // 그림은 뒤집지 않고 그대로 입힌다 (실제 게임 화면에서 바르게 보인다). 이미 그림을 넣어 둔 머티리얼은 그대로 둔다.
                boardMat.SetTexture("_BaseMap", boardTexture);
                boardMat.SetTextureScale("_BaseMap", Vector2.one);
                boardMat.SetTextureOffset("_BaseMap", Vector2.zero);
            }
            var sign = new GameObject("GuideSign");
            Vector3 position = new Vector3(-2.6f, 0f, 0.9f);
            sign.transform.position = position;
            sign.transform.rotation = Quaternion.LookRotation(new Vector3(0.8f, 0f, -0.45f)); // 시작 위치 쪽을 본다 (앞면 +Z)
            Part("PostLeft", sign.transform, new Vector3(-0.62f, 0.8f, 0f), new Vector3(0.1f, 1.6f, 0.1f), wood);
            Part("PostRight", sign.transform, new Vector3(0.62f, 0.8f, 0f), new Vector3(0.1f, 1.6f, 0.1f), wood);
            Part("Frame", sign.transform, new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 0.95f, 0.07f), wood);
            Part("Board", sign.transform, new Vector3(0f, 1.25f, 0.04f), new Vector3(1.36f, 0.81f, 0.03f), boardMat);

            var blocker = sign.AddComponent<BoxCollider>(); // 지나가다 통과하지 않게
            blocker.center = new Vector3(0f, 0.8f, 0f);
            blocker.size = new Vector3(1.5f, 1.6f, 0.3f);

            var device = sign.AddComponent<GuideSign>();
            var deviceSo = new SerializedObject(device);
            deviceSo.FindProperty("popup").objectReferenceValue = popup;
            deviceSo.FindProperty("prompt").objectReferenceValue = prompt;
            deviceSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 1_Museum: 입구 안내판과 안내 팝업을 만들었어요.");
        }

        // 그림 한 장 (흰색 = 그림 색 그대로, 비율 유지)
        static void MakeImage(Transform panel, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(panel, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static void Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
