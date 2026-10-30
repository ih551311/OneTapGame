using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 미술관의 전투 장소 선택 팝업에 "성 마을" 버튼을 더한다 (숲 버튼은 위로 올린다).
    // 메뉴: LastBullet > Phase 11 장소 선택에 성 마을 추가
    public static class Phase11StageSelectSetup
    {
        [MenuItem("LastBullet/Phase 11 장소 선택에 성 마을 추가")]
        static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/3_CastleVillage.unity") == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 11 성 마을 씬 만들기를 먼저 실행하세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var popup = Object.FindFirstObjectByType<StageSelectPopup>(FindObjectsInactive.Include);
            var panel = popup.transform.Find("Panel");
            if (panel.Find("CastleButton") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 CastleButton이 있어요.", "확인");
                return;
            }

            // 숲은 위로, 성 마을은 아래에
            panel.Find("ForestButton").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
            var castle = Phase4StageSelectSetup.CreateStageButton("CastleButton", panel, "Assets/UI/StageLabel_Castle.png", new Vector2(0f, -110f));

            var so = new SerializedObject(popup);
            so.FindProperty("castleButton").objectReferenceValue = castle;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 장소 선택 팝업에 성 마을 버튼을 더하고 1_Museum 씬을 저장했어요.");
        }
    }
}
