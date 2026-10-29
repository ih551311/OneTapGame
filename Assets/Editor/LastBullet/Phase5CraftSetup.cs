using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 5-3: 미술관에 종이 합성대를 놓고, E로 여는 "종이 합성" 팝업을 만든다.
    // 팝업 구성: 제목 / [종이조각] x5 → [종이] x1 / 가진 것 [종이조각] N [종이] M / 합성 버튼 / 닫기
    // 메뉴: LastBullet > Phase 5-3 종이 합성대 만들기
    public static class Phase5CraftSetup
    {
        [MenuItem("LastBullet/Phase 5-3 종이 합성대 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            if (GameObject.Find("PaperCraftTable") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 PaperCraftTable이 있어요.", "확인");
                return;
            }

            // 1. 종이 합성대 (임시 상자, 전용 머티리얼) - 지도 탁자 왼쪽
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "PaperCraftTable";
            table.transform.position = new Vector3(-4f, 0.5f, 3f);
            table.transform.localScale = new Vector3(1.2f, 1f, 0.8f);
            table.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("PaperCraftTable", new Color32(0xC8, 0xA2, 0x7A, 0xFF));
            var device = table.AddComponent<InteractDevice>();

            // 2. 가까이 가면 뜨는 안내
            var prompt = Phase4StageSelectSetup.CreateImage("Prompt_PaperCraft", canvas.transform, "Assets/UI/Prompt_PaperCraft.png");
            Phase4StageSelectSetup.SetRect(prompt, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(480f, 60f));
            prompt.SetActive(false);

            // 3. 팝업
            var popup = new GameObject("PaperCraftPopup", typeof(RectTransform));
            popup.transform.SetParent(canvas.transform, false);
            Phase4StageSelectSetup.Stretch(popup.GetComponent<RectTransform>());

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(popup.transform, false);
            Phase4StageSelectSetup.Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var panel = Phase4StageSelectSetup.CreateImage("Panel", popup.transform, "Assets/UI/Popup_Panel.png", Phase4StageSelectSetup.PanelBorder);
            panel.GetComponent<Image>().type = Image.Type.Sliced;
            panel.GetComponent<Image>().preserveAspect = false;
            Phase4StageSelectSetup.SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 560f));
            var center = new Vector2(0.5f, 0.5f);

            var title = Phase4StageSelectSetup.CreateImage("Title", panel.transform, "Assets/UI/PaperCraft_Title.png");
            Phase4StageSelectSetup.SetRect(title, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 100f));

            // 만드는 법: [종이조각] x5 → [종이] x1
            Icon(panel.transform, "Recipe_Scrap", "Assets/UI/Icon_PaperScrap.png", new Vector2(-230f, 70f), 80f);
            Text(panel.transform, "Recipe_ScrapAmount", "x5", new Vector2(-150f, 70f), 40f);
            var arrow = Phase4StageSelectSetup.CreateImage("Recipe_Arrow", panel.transform, "Assets/UI/Arrow_Right.png");
            Phase4StageSelectSetup.SetRect(arrow, center, new Vector2(-30f, 70f), new Vector2(96f, 64f));
            Icon(panel.transform, "Recipe_Paper", "Assets/UI/Icon_Paper.png", new Vector2(90f, 70f), 80f);
            Text(panel.transform, "Recipe_PaperAmount", "x1", new Vector2(170f, 70f), 40f);

            // 가진 것: [종이조각] N [종이] M
            var owned = Phase4StageSelectSetup.CreateImage("Owned_Label", panel.transform, "Assets/UI/PaperCraft_Owned.png");
            Phase4StageSelectSetup.SetRect(owned, center, new Vector2(-250f, -40f), new Vector2(180f, 48f));
            Icon(panel.transform, "Owned_Scrap", "Assets/UI/Icon_PaperScrap.png", new Vector2(-110f, -40f), 48f);
            var scrapCount = Text(panel.transform, "Owned_ScrapCount", "0", new Vector2(-40f, -40f), 32f);
            Icon(panel.transform, "Owned_Paper", "Assets/UI/Icon_Paper.png", new Vector2(70f, -40f), 48f);
            var paperCount = Text(panel.transform, "Owned_PaperCount", "0", new Vector2(140f, -40f), 32f);

            var craftButton = Phase4StageSelectSetup.CreateStageButton("CraftButton", panel.transform, "Assets/UI/ButtonLabel_Craft.png", new Vector2(0f, -170f));

            var close = Phase4StageSelectSetup.CreateImage("CloseButton", panel.transform, "Assets/UI/Button_Close.png");
            Phase4StageSelectSetup.SetRect(close, new Vector2(1f, 1f), new Vector2(-48f, -48f), new Vector2(64f, 64f));
            close.GetComponent<Image>().raycastTarget = true;
            var closeButton = close.AddComponent<Button>();

            var popupScript = popup.AddComponent<PaperCraftPopup>();
            var popupSo = new SerializedObject(popupScript);
            popupSo.FindProperty("craftButton").objectReferenceValue = craftButton;
            popupSo.FindProperty("paperScrapCount").objectReferenceValue = scrapCount;
            popupSo.FindProperty("drawingPaperCount").objectReferenceValue = paperCount;
            popupSo.FindProperty("closeButton").objectReferenceValue = closeButton;
            popupSo.ApplyModifiedPropertiesWithoutUndo();
            popup.SetActive(false);

            var deviceSo = new SerializedObject(device);
            deviceSo.FindProperty("popup").objectReferenceValue = popupScript;
            deviceSo.FindProperty("prompt").objectReferenceValue = prompt;
            deviceSo.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = table;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 5-3: 미술관에 종이 합성대(PaperCraftTable)와 종이 합성 팝업을 만들고 1_Museum 씬을 저장했어요.");
        }

        static void Icon(Transform parent, string name, string spritePath, Vector2 position, float size)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, parent, spritePath);
            Phase4StageSelectSetup.SetRect(go, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        }

        static TMP_Text Text(Transform parent, string name, string value, Vector2 position, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Phase4StageSelectSetup.SetRect(go, new Vector2(0.5f, 0.5f), position, new Vector2(100f, 56f));
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
