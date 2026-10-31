using LastBullet.Drawing;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 미술관과 전투 맵(숲·성 마을·사막) 화면 왼쪽 위에 내 프로필 패널을 만든다 (2026-10-31 사용자 요청, 스케치대로).
    //   ┌──────────────────────────────┐
    //   │ [ 사진 ]  [   닉네임        ]│   사진은 크고 정사각형(150), 이름 칸은 크림색 판
    //   │ ▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░ │   전투 맵에서만: 체력바가 패널 맨 아래를 가로지른다
    //   └──────────────────────────────┘
    // 패널은 어두운 판 하나(Hud_Panel.png)이고 안에 사진 테두리(Hud_Photo.png), 이름 칸(Hud_Name.png)이 있다. 모두 Assets/UI PNG라서 바꿀 수 있다.
    // 미술관은 체력바가 없어서 패널이 짧다. 다시 실행하면 지우고 새로 만든다 (체력바는 안전하게 꺼내 둔 뒤 다시 넣는다).
    // 메뉴: LastBullet > Phase 33 화면에 프로필 넣기
    public static class Phase33HudProfileSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";

        const float Pad = 14f;          // 패널 안쪽 여백
        const float Photo = 150f;       // 사진 한 변
        const float PanelWidth = 440f; // 사진이 주인공이라 이름 칸은 좁게 (맵 이름 안내와도 겹치지 않는다)
        const float BarHeight = 28f;
        const float Gap = 12f;

        static readonly (string scene, bool battle)[] Scenes =
        {
            ("Assets/Scenes/1_Museum.unity", false),
            ("Assets/Scenes/2_Forest.unity", true),
            ("Assets/Scenes/3_CastleVillage.unity", true),
            ("Assets/Scenes/4_Desert.unity", true),
        };

        [MenuItem("LastBullet/Phase 33 화면에 프로필 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var (path, battle) in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                if (canvas == null)
                    continue;

                // 체력바는 패널 안에 있으면 패널을 지울 때 같이 지워지니, 먼저 캔버스로 꺼낸다
                var bar = FindHealthBar(canvas.transform);
                if (bar != null)
                    bar.SetParent(canvas.transform, false);
                var old = canvas.transform.Find("HudProfile");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var panel = Build(canvas.transform, battle);
                if (battle && bar != null)
                    PutBarInPanel(bar, panel);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 프로필 패널을 만들었어요{(battle ? " (체력바는 패널 맨 아래)" : "")}.");
            }
            AssetDatabase.SaveAssets();
        }

        static Transform FindHealthBar(Transform canvas)
        {
            var bar = canvas.Find("HealthBar");
            if (bar != null)
                return bar;
            var panel = canvas.Find("HudProfile");
            return panel != null ? panel.Find("HealthBar") : null;
        }

        static Transform Build(Transform canvas, bool battle)
        {
            float height = Pad + Photo + Pad + (battle ? Gap - Pad + BarHeight + Pad : 0f);

            var root = Phase6PaintBoardSetup.Sliced("HudProfile", canvas, "Assets/UI/Hud_Panel.png", 20f);
            root.GetComponent<Image>().raycastTarget = false;
            // 다른 화면 표시 뒤쪽에 그린다 (붉은 테두리 효과 바로 위)
            var hit = canvas.Find("HitFeedback");
            root.transform.SetSiblingIndex(hit != null ? hit.GetSiblingIndex() + 1 : 0);
            {
                // 전투 맵과 미술관 모두 왼쪽 아래 (2026-10-31 사용자 요청)
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(36f, 28f);
                rect.sizeDelta = new Vector2(PanelWidth, height);
            }

            // 사진: 얇은 금색 테두리 + 안쪽 사진 (사진이 없으면 빈 사람 모양)
            var frame = Phase6PaintBoardSetup.Sliced("PhotoFrame", root.transform, "Assets/UI/Hud_Photo.png", 10f);
            frame.GetComponent<Image>().raycastTarget = false;
            TopLeft(frame, new Vector2(Pad, Pad), new Vector2(Photo, Photo));
            var pictureObject = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
            Inset(pictureObject, frame.transform, 5f);
            var raw = pictureObject.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.enabled = false;
            var empty = Phase4StageSelectSetup.CreateImage("Empty", frame.transform, "Assets/UI/Profile_Empty.png");
            Inset(empty, frame.transform, 5f);
            empty.GetComponent<Image>().preserveAspect = false;

            // 이름 칸: 패널보다 한 톤 밝은 어두운 판, 부드러운 크림색 글씨 (사진과 같은 높이. 눈에 띄는 건 사진이어야 해서 조용하게)
            var plate = Phase6PaintBoardSetup.Sliced("NamePlate", root.transform, "Assets/UI/Hud_Name.png", 14f);
            plate.GetComponent<Image>().raycastTarget = false;
            float nameX = Pad + Photo + Gap;
            TopLeft(plate, new Vector2(nameX, Pad), new Vector2(PanelWidth - nameX - Pad, Photo));

            var textObject = new GameObject("Nickname", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(plate.transform, false);
            Phase4StageSelectSetup.Stretch(textObject.GetComponent<RectTransform>());
            var text = textObject.GetComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontStyle = FontStyles.Normal;
            text.color = new Color32(0xEE, 0xE6, 0xDA, 0xFF); // 어두운 판 위의 부드러운 크림색 (튀지 않게)
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true; // 긴 이름은 글자가 줄어들어 칸 안에 들어온다
            text.fontSizeMin = 18f;
            text.fontSizeMax = 30f;
            text.rectTransform.offsetMin = new Vector2(16f, 8f);
            text.rectTransform.offsetMax = new Vector2(-16f, -8f);
            text.text = ProfileStore.DefaultNickname;

            var script = root.AddComponent<HudProfile>();
            var so = new SerializedObject(script);
            so.FindProperty("picture").objectReferenceValue = raw;
            so.FindProperty("emptyShape").objectReferenceValue = empty;
            so.FindProperty("nickname").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root.transform;
        }

        // 체력바를 패널 맨 아래(사진·이름 칸 아래)에 가로로 놓는다
        static void PutBarInPanel(Transform bar, Transform panel)
        {
            bar.SetParent(panel, false);
            bar.gameObject.SetActive(true);
            bar.SetAsLastSibling();
            TopLeft(bar.gameObject, new Vector2(Pad, Pad + Photo + Gap), new Vector2(PanelWidth - 2 * Pad, BarHeight));
        }

        // 왼쪽 위 모서리 기준: x는 오른쪽으로, y는 아래로 얼마나 떨어졌는지
        static void TopLeft(GameObject go, Vector2 offset, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(offset.x, -offset.y);
            rect.sizeDelta = size;
        }

        static void Inset(GameObject go, Transform parent, float margin)
        {
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }
    }
}
