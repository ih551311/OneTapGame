using LastBullet.Inventory;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 5-2: 미술관에 창고 재료 표시, 훈련장에 "물감 가득 참" 알림을 만든다.
    // (창고 InventoryManager는 처음 쓰일 때 자동으로 생겨서 씬에 놓을 필요가 없다)
    // 메뉴: LastBullet > Phase 5-2 창고 표시와 가득 참 알림 만들기
    public static class Phase5InventorySetup
    {
        [MenuItem("LastBullet/Phase 5-2 창고 표시와 가득 참 알림 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // 1. 미술관: 오른쪽 위 창고 표시
            var museum = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            if (canvas.transform.Find("InventoryPanel") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 InventoryPanel이 있어요.", "확인");
                return;
            }
            CreateInventoryPanel(canvas.transform);
            EditorSceneManager.MarkSceneDirty(museum);
            EditorSceneManager.SaveScene(museum);

            // 2. 훈련장: 가득 참 알림 (클리어 문구 아래)
            var training = EditorSceneManager.OpenScene("Assets/Scenes/2_Forest.unity");
            CreatePaintFullToast(ArtSlots.FindCanvas().transform);
            EditorSceneManager.MarkSceneDirty(training);
            EditorSceneManager.SaveScene(training);

            EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 5-2: 미술관에 창고 표시(InventoryPanel), 훈련장에 가득 참 알림(PaintFullToast)을 만들었어요.");
        }

        static void CreateInventoryPanel(Transform canvas)
        {
            var panel = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -30f);
            rect.sizeDelta = new Vector2(PaintColors.Count * 48f + 32f, 160f);
            var image = panel.GetComponent<Image>();
            image.sprite = ArtSlots.LoadSprite("Assets/UI/BagPanel.png", 24f);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;

            Phase5BagSetup.CreateIcon(panel.transform, "Icon_PaperScrap", "Assets/UI/Icon_PaperScrap.png", new Vector2(40f, -40f), 48f);
            var scraps = Phase5BagSetup.CreateCount(panel.transform, "PaperScrapCount", new Vector2(120f, -40f), new Vector2(100f, 48f), 34f, TextAlignmentOptions.Left);
            Phase5BagSetup.CreateIcon(panel.transform, "Icon_Paper", "Assets/UI/Icon_Paper.png", new Vector2(240f, -40f), 48f);
            var paper = Phase5BagSetup.CreateCount(panel.transform, "DrawingPaperCount", new Vector2(320f, -40f), new Vector2(100f, 48f), 34f, TextAlignmentOptions.Left);

            var paints = new TMP_Text[PaintColors.Count];
            for (int i = 0; i < PaintColors.Count; i++)
            {
                string colorName = ((PaintColor)i).ToString();
                float x = 40f + i * 48f;
                Phase5BagSetup.CreateIcon(panel.transform, $"Icon_Paint_{colorName}", $"Assets/UI/Icon_Paint_{colorName}.png", new Vector2(x, -100f), 40f);
                paints[i] = Phase5BagSetup.CreateCount(panel.transform, $"PaintCount_{colorName}", new Vector2(x, -138f), new Vector2(48f, 26f), 20f, TextAlignmentOptions.Center);
            }

            var ui = panel.AddComponent<InventoryUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("paperScrapCount").objectReferenceValue = scraps;
            so.FindProperty("drawingPaperCount").objectReferenceValue = paper;
            var array = so.FindProperty("paintCounts");
            array.arraySize = PaintColors.Count;
            for (int i = 0; i < PaintColors.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = paints[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreatePaintFullToast(Transform canvas)
        {
            // 줄들을 위에서 아래로 쌓는 상자
            var container = new GameObject("PaintFullToast", typeof(RectTransform), typeof(VerticalLayoutGroup));
            container.transform.SetParent(canvas, false);
            var rect = container.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -620f);
            rect.sizeDelta = new Vector2(360f, 300f);
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            // 한 줄의 틀: [물감 아이콘] 가득 참! -50
            var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(container.transform, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 64f);
            var rowImage = row.GetComponent<Image>();
            rowImage.sprite = ArtSlots.LoadSprite("Assets/UI/BagPanel.png", 24f);
            rowImage.type = Image.Type.Sliced;
            rowImage.color = Color.white;
            rowImage.raycastTarget = false;

            Phase5BagSetup.CreateIcon(row.transform, "Icon", "Assets/UI/Icon_Paint_Red.png", new Vector2(36f, -32f), 44f);
            Phase5BagSetup.CreateIcon(row.transform, "Label", "Assets/UI/Toast_PaintFull.png", new Vector2(170f, -32f), 0f);
            var label = row.transform.Find("Label").GetComponent<RectTransform>();
            label.sizeDelta = new Vector2(180f, 48f);
            Phase5BagSetup.CreateCount(row.transform, "Amount", new Vector2(310f, -32f), new Vector2(80f, 44f), 28f, TextAlignmentOptions.Center);
            row.SetActive(false);

            var toast = container.AddComponent<PaintFullToast>();
            var so = new SerializedObject(toast);
            so.FindProperty("rowTemplate").objectReferenceValue = row;
            var icons = so.FindProperty("paintIcons");
            icons.arraySize = PaintColors.Count;
            for (int i = 0; i < PaintColors.Count; i++)
                icons.GetArrayElementAtIndex(i).objectReferenceValue = ArtSlots.LoadSprite($"Assets/UI/Icon_Paint_{(PaintColor)i}.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
