using LastBullet.Inventory;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 숲·성 마을·사막 씬에 "이번 판 결과" 창(RunResultPopup)을 만든다 (2026-10-31 사용자 요청).
    // 귀환하거나 죽으면 뜨는 창: 제목 / 잡은 적 · 종이조각 / 얻은(잃은) 물감 9색 / [미술관으로].
    // 글자는 Assets/UI의 PNG(ResultTitle_*, ResultCaption_*, ResultLabel_Kills, ButtonLabel_ToMuseum), 숫자만 TextMeshPro.
    // 이미 있으면 건너뛴다. 메뉴: LastBullet > Phase 20 이번 판 결과 창 넣기
    public static class Phase20RunResultSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 20 이번 판 결과 창 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var scenePath in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var canvas = ArtSlots.FindCanvas();
                if (canvas == null || canvas.transform.Find("RunResultPopup") != null)
                    continue;
                Build(canvas.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 이번 판 결과 창(RunResultPopup)을 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static void Build(Transform canvas)
        {
            // 스크립트가 붙은 오브젝트는 항상 켜 둔다 (꺼져 있으면 Awake가 불리지 않는다). 창(Window)만 껐다 켠다.
            var root = new GameObject("RunResultPopup", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            root.transform.SetAsLastSibling();
            Phase4StageSelectSetup.Stretch(root.GetComponent<RectTransform>());

            var window = new GameObject("Window", typeof(RectTransform));
            window.transform.SetParent(root.transform, false);
            Phase4StageSelectSetup.Stretch(window.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(window.transform, false);
            Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
            var dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.7f);
            dimImage.raycastTarget = true; // 뒤쪽 클릭을 막는다

            var panel = Phase4StageSelectSetup.CreateImage("Panel", window.transform, "Assets/UI/Popup_Panel.png", Phase4StageSelectSetup.PanelBorder);
            panel.GetComponent<Image>().type = Image.Type.Sliced;
            panel.GetComponent<Image>().preserveAspect = false;
            Phase4StageSelectSetup.SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 720f));

            // 제목 (귀환 / 사망 중 하나만 보인다)
            var titleClear = Picture("TitleClear", panel.transform, "Assets/UI/ResultTitle_Clear.png", new Vector2(0f, 280f), new Vector2(640f, 96f));
            var titleDead = Picture("TitleDead", panel.transform, "Assets/UI/ResultTitle_Dead.png", new Vector2(0f, 280f), new Vector2(640f, 96f));
            titleDead.SetActive(false);

            // 잡은 적 / 종이조각
            Picture("KillsLabel", panel.transform, "Assets/UI/ResultLabel_Kills.png", new Vector2(-300f, 170f), new Vector2(240f, 64f));
            var kills = Number("KillsCount", panel.transform, new Vector2(-130f, 170f), new Vector2(160f, 64f), 56f);
            Picture("PaperIcon", panel.transform, "Assets/UI/Icon_PaperScrap.png", new Vector2(130f, 170f), new Vector2(64f, 64f));
            var paper = Number("PaperScrapCount", panel.transform, new Vector2(260f, 170f), new Vector2(160f, 64f), 56f);

            // 얻은(잃은) 재료
            var gained = Picture("CaptionGained", panel.transform, "Assets/UI/ResultCaption_Gained.png", new Vector2(0f, 70f), new Vector2(400f, 64f));
            var lost = Picture("CaptionLost", panel.transform, "Assets/UI/ResultCaption_Lost.png", new Vector2(0f, 70f), new Vector2(400f, 64f));
            lost.SetActive(false);

            string[] colors = System.Enum.GetNames(typeof(PaintColor));
            var paintCounts = new TMP_Text[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                float x = (i - (colors.Length - 1) / 2f) * 92f;
                Picture($"Icon_Paint_{colors[i]}", panel.transform, $"Assets/UI/Icon_Paint_{colors[i]}.png", new Vector2(x, -35f), new Vector2(68f, 68f));
                paintCounts[i] = Number($"PaintCount_{colors[i]}", panel.transform, new Vector2(x, -105f), new Vector2(88f, 44f), 34f);
            }

            // [미술관으로]
            var button = Phase4StageSelectSetup.CreateStageButton("ToMuseumButton", panel.transform, "Assets/UI/ButtonLabel_ToMuseum.png", new Vector2(0f, -265f));
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(440f, 110f);
            button.transform.Find("Label").GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 90f);

            var script = root.AddComponent<RunResultPopup>();
            var so = new SerializedObject(script);
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("titleClear").objectReferenceValue = titleClear;
            so.FindProperty("titleDead").objectReferenceValue = titleDead;
            so.FindProperty("captionGained").objectReferenceValue = gained;
            so.FindProperty("captionLost").objectReferenceValue = lost;
            so.FindProperty("killsCount").objectReferenceValue = kills;
            so.FindProperty("paperScrapCount").objectReferenceValue = paper;
            var counts = so.FindProperty("paintCounts");
            counts.arraySize = paintCounts.Length;
            for (int i = 0; i < paintCounts.Length; i++)
                counts.GetArrayElementAtIndex(i).objectReferenceValue = paintCounts[i];
            so.FindProperty("toMuseumButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            window.SetActive(false);
        }

        static GameObject Picture(string name, Transform parent, string spritePath, Vector2 position, Vector2 size)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, parent, spritePath);
            Phase4StageSelectSetup.SetRect(go, new Vector2(0.5f, 0.5f), position, size);
            return go;
        }

        // 가운데 맞춘 숫자 글자 (TextMeshPro, 흰색 굵게)
        static TMP_Text Number(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Phase4StageSelectSetup.SetRect(go, new Vector2(0.5f, 0.5f), position, size);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = "0";
            text.raycastTarget = false;
            return text;
        }
    }
}
