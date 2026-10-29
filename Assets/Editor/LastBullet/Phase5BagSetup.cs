using LastBullet.Enemy;
using LastBullet.Inventory;
using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 5-1: 적 처치 → 재료 드롭 → 전투 가방, 그리고 화면 오른쪽 위 전투 가방 표시.
    // 처음 실행하면 숫자 표시에 필요한 TextMeshPro 기본 파일을 먼저 가져온다 (그 뒤 메뉴를 한 번 더 누른다).
    // 메뉴: LastBullet > Phase 5-1 재료 드롭과 전투 가방 만들기
    public static class Phase5BagSetup
    {
        const string TrainingPath = "Assets/Scenes/2_Training.unity";
        const string EnemyPrefabPath = "Assets/Prefabs/Enemy_Charger.prefab";
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [MenuItem("LastBullet/Phase 5-1 재료 드롭과 전투 가방 만들기")]
        static void Create()
        {
            // 0. TextMeshPro 기본 파일 (글꼴 등)
            if (AssetDatabase.LoadMainAssetAtPath(TmpSettingsPath) == null)
            {
                AssetDatabase.ImportPackage(TmpEssentialsPackage, false);
                EditorUtility.DisplayDialog("LastBullet", "글자 표시에 필요한 TextMeshPro 기본 파일을 가져왔어요.\n가져오기가 끝나면 이 메뉴를 한 번 더 눌러 주세요.", "확인");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(TrainingPath);
            var stageManager = Object.FindAnyObjectByType<StageManager>();
            var canvas = ArtSlots.FindCanvas();
            if (stageManager == null || canvas == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 4-4 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (stageManager.GetComponent<RunBag>() != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 RunBag이 있어요.", "확인");
                return;
            }

            // 1. 적 프리팹에 드롭
            var root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            if (root.GetComponent<EnemyDrop>() == null)
                root.AddComponent<EnemyDrop>();
            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            // 2. 전투 가방
            stageManager.gameObject.AddComponent<RunBag>();

            // 3. 전투 가방 표시 (오른쪽 위)
            var panel = new GameObject("RunBagPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-30f, -30f);
            panelRect.sizeDelta = new Vector2(PaintColors.Count * 48f + 32f, 150f);
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = ArtSlots.LoadSprite("Assets/UI/BagPanel.png", 24f);
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
            panelImage.raycastTarget = false;

            CreateIcon(panel.transform, "Icon_PaperScrap", "Assets/UI/Icon_PaperScrap.png", new Vector2(40f, -40f), 48f);
            var scrapCount = CreateCount(panel.transform, "PaperScrapCount", new Vector2(130f, -40f), new Vector2(120f, 48f), 34f, TextAlignmentOptions.Left);

            var paintCounts = new TMP_Text[PaintColors.Count];
            for (int i = 0; i < PaintColors.Count; i++)
            {
                string colorName = ((PaintColor)i).ToString();
                float x = 40f + i * 48f;
                CreateIcon(panel.transform, $"Icon_Paint_{colorName}", $"Assets/UI/Icon_Paint_{colorName}.png", new Vector2(x, -92f), 40f);
                paintCounts[i] = CreateCount(panel.transform, $"PaintCount_{colorName}", new Vector2(x, -128f), new Vector2(48f, 26f), 20f, TextAlignmentOptions.Center);
            }

            var ui = panel.AddComponent<RunBagUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("paperScrapCount").objectReferenceValue = scrapCount;
            var array = so.FindProperty("paintCounts");
            array.arraySize = PaintColors.Count;
            for (int i = 0; i < PaintColors.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = paintCounts[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = panel;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 5-1: 적 드롭(EnemyDrop), 전투 가방(RunBag), 전투 가방 표시(RunBagPanel)를 만들고 2_Training 씬을 저장했어요.");
        }

        // 패널 왼쪽 위 기준 위치(center)에 아이콘 그림
        internal static void CreateIcon(Transform parent, string name, string spritePath, Vector2 center, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = center;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = ArtSlots.LoadSprite(spritePath);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        internal static TMP_Text CreateCount(Transform parent, string name, Vector2 center, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "0";
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
