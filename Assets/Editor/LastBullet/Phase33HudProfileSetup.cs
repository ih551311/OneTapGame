using LastBullet.Drawing;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 미술관과 전투 맵(숲·성 마을·사막) 화면 왼쪽 위에 내 프로필(사진 + 닉네임)을 넣는다 (2026-10-31 사용자 요청).
    // 전투 맵에서는 체력바를 프로필 바로 아래로 옮긴다. 미술관의 체력바는 계속 꺼 둔다.
    // 위치(왼쪽 위 기준, 1920×1080): 프로필 (40, 30) 크기 440×120 / 체력바 (40, 166) 크기 440×40
    // 이미 있으면 위치만 다시 맞춘다. 메뉴: LastBullet > Phase 33 화면에 프로필 넣기
    public static class Phase33HudProfileSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";

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

                if (canvas.transform.Find("HudProfile") == null)
                    Build(canvas.transform);

                if (battle)
                    PlaceHealthBar(canvas.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 화면 왼쪽 위에 프로필을 넣었어요{(battle ? " (체력바는 그 아래)" : "")}.");
            }
            AssetDatabase.SaveAssets();
        }

        static void Build(Transform canvas)
        {
            var root = new GameObject("HudProfile", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            // 다른 화면 표시 뒤쪽에 그린다 (붉은 테두리 효과 바로 위)
            var hit = canvas.Find("HitFeedback");
            root.transform.SetSiblingIndex(hit != null ? hit.GetSiblingIndex() + 1 : 0);
            TopLeft(root, new Vector2(40f, 30f), new Vector2(440f, 120f));

            // 사진 액자 (정사각형)
            var frame = Phase6PaintBoardSetup.Sliced("Frame", root.transform, "Assets/UI/Gallery_Frame.png", 24f);
            frame.GetComponent<Image>().raycastTarget = false;
            TopLeft(frame, Vector2.zero, new Vector2(120f, 120f));

            var pictureObject = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
            Inset(pictureObject, frame.transform, 18f);
            var raw = pictureObject.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.enabled = false;

            var empty = Phase4StageSelectSetup.CreateImage("Empty", frame.transform, "Assets/UI/Profile_Empty.png");
            Inset(empty, frame.transform, 18f);
            empty.GetComponent<Image>().preserveAspect = false;

            // 닉네임 이름표 (사진 오른쪽, 세로 가운데)
            var plate = Phase6PaintBoardSetup.Sliced("NamePlate", root.transform, "Assets/UI/Profile_NamePlate.png", 12f);
            plate.GetComponent<Image>().raycastTarget = false;
            TopLeft(plate, new Vector2(132f, 30f), new Vector2(308f, 60f));

            var textObject = new GameObject("Nickname", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(plate.transform, false);
            Phase4StageSelectSetup.Stretch(textObject.GetComponent<RectTransform>());
            var text = textObject.GetComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true; // 긴 이름은 글자가 줄어들어 판 안에 들어온다
            text.fontSizeMin = 20f;
            text.fontSizeMax = 36f;
            text.rectTransform.offsetMin = new Vector2(12f, 4f);
            text.rectTransform.offsetMax = new Vector2(-12f, -4f);
            text.text = ProfileStore.DefaultNickname;

            var script = root.AddComponent<HudProfile>();
            var so = new SerializedObject(script);
            so.FindProperty("picture").objectReferenceValue = raw;
            so.FindProperty("emptyShape").objectReferenceValue = empty;
            so.FindProperty("nickname").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 체력바를 프로필 바로 아래(왼쪽 위 기준)로. 원래 왼쪽 아래 기준으로 놓여 있었는데 왼쪽 위 기준으로 바꾼다.
        static void PlaceHealthBar(Transform canvas)
        {
            var bar = canvas.Find("HealthBar");
            if (bar == null)
                return;
            bar.gameObject.SetActive(true);
            TopLeft(bar.gameObject, new Vector2(40f, 166f), new Vector2(440f, 40f));
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
