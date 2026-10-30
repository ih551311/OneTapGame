using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관의 전투 장소 선택 팝업을 3개 버튼(숲·성 마을·사막)으로 만들고, 버튼마다 "얻는 물감" 안내 그림을 붙인다.
    // 또 성 마을 씬에 남아 있던 물감 지역(PaintZones)을 지운다 (맵마다 3색이라 필요 없다).
    // 안내 그림은 Assets/UI/StageColors_Forest·Castle·Desert.png (글자도 그림 안에 있다. 직접 그린 그림으로 바꿀 수 있다)
    // 메뉴: LastBullet > Phase 13 장소 선택에 사막과 물감 안내 넣기
    public static class Phase13StageSelectSetup
    {
        [MenuItem("LastBullet/Phase 13 장소 선택에 사막과 물감 안내 넣기")]
        static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/4_Desert.unity") == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 13 사막 씬 만들기를 먼저 실행하세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // 1. 성 마을: 물감 지역 지우기
            var castle = EditorSceneManager.OpenScene("Assets/Scenes/3_CastleVillage.unity");
            var zones = GameObject.Find("PaintZones");
            if (zones != null)
            {
                Object.DestroyImmediate(zones);
                EditorSceneManager.MarkSceneDirty(castle);
                EditorSceneManager.SaveScene(castle);
            }

            // 2. 미술관 팝업
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var popup = Object.FindFirstObjectByType<StageSelectPopup>(FindObjectsInactive.Include);
            var panel = popup.transform.Find("Panel");

            // 버튼 3개와 아래 안내 그림 3줄이 들어가도록 창을 키운다 (창 높이 560 → 680)
            Phase4StageSelectSetup.SetRect(panel.gameObject, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 680f));
            Place(panel.Find("ForestButton"), 110f);
            Place(panel.Find("CastleButton"), -60f);
            var desertTransform = panel.Find("DesertButton");
            var desert = desertTransform != null
                ? desertTransform.GetComponent<UnityEngine.UI.Button>()
                : Phase4StageSelectSetup.CreateStageButton("DesertButton", panel, "Assets/UI/StageLabel_Desert.png", new Vector2(0f, -230f));
            Place(desert.transform, -230f);

            // 예전에 글자(TextMeshPro)로 만들었던 안내가 있으면 지우고 그림으로 다시 만든다
            foreach (var old in new[] { "ForestColors", "CastleColors", "DesertColors" })
            {
                var t = panel.Find(old);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }
            CreateColorImage(panel, "ForestColors", "Assets/UI/StageColors_Forest.png", 30f);
            CreateColorImage(panel, "CastleColors", "Assets/UI/StageColors_Castle.png", -140f);
            CreateColorImage(panel, "DesertColors", "Assets/UI/StageColors_Desert.png", -310f);

            var so = new SerializedObject(popup);
            so.FindProperty("desertButton").objectReferenceValue = desert;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 13: 장소 선택 팝업에 사막 버튼과 '얻는 물감' 안내를 넣고 1_Museum 씬을 저장했어요.");
        }

        static void Place(Transform button, float y)
        {
            button.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y);
        }

        static void CreateColorImage(Transform panel, string name, string spritePath, float y)
        {
            var go = Phase4StageSelectSetup.CreateImage(name, panel, spritePath);
            Phase4StageSelectSetup.SetRect(go, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(720f, 64f));
        }
    }
}
